using FirstBrick.Investment.Api.Data;
using FirstBrick.Investment.Api.Domain;
using FirstBrick.Investment.Api.Dtos;
using FirstBrick.Shared.Auth;
using FirstBrick.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Investment.Api.Controllers;

[ApiController]
[Route("v1")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly InvestmentDbContext _db;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(InvestmentDbContext db, IPublishEndpoint publisher, ILogger<ProjectsController> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost("project")]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        if (User.GetRole() != Roles.ProjectOwner)
            return Forbid();
        if (string.IsNullOrWhiteSpace(request.Title) || request.TargetAmount <= 0)
            return BadRequest(new { error = "Title and positive TargetAmount required" });

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OwnerId = User.GetUserId(),
            Title = request.Title.Trim(),
            TargetAmount = request.TargetAmount,
            CurrentAmount = 0m
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(List), new { page = 1, pageSize = 20 }, ToDto(project));
    }

    [HttpGet("projects")]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var projects = await _db.Projects.AsNoTracking()
            .OrderBy(p => p.Title)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync(ct);

        return Ok(projects);
    }

    [HttpPost("invest")]
    public async Task<IActionResult> Invest([FromBody] InvestRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be positive" });

        var userId = User.GetUserId();

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Atomic capacity reservation: UPDATE ... WHERE CurrentAmount + @Amount <= TargetAmount
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE ""Projects""
               SET ""CurrentAmount"" = ""CurrentAmount"" + {request.Amount}
               WHERE ""Id"" = {request.ProjectId}
                 AND (""CurrentAmount"" + {request.Amount}) <= ""TargetAmount""",
            ct);

        if (affected == 0)
        {
            // 0 rows: either the project doesn't exist (404) or capacity is exhausted (409)
            var exists = await _db.Projects.AsNoTracking()
                .AnyAsync(p => p.Id == request.ProjectId, ct);
            await tx.RollbackAsync(ct);
            return exists
                ? Conflict(new { error = "Project capacity exhausted" })
                : NotFound(new { error = "Project not found" });
        }

        // Load title in the same transaction so it can be embedded in the event.
        // This removes the need for Portfolio to call Investment synchronously.
        var projectTitle = await _db.Projects.AsNoTracking()
            .Where(p => p.Id == request.ProjectId)
            .Select(p => p.Title)
            .FirstAsync(ct);

        var investmentRequest = new InvestmentRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = request.ProjectId,
            Amount = request.Amount,
            Status = InvestmentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.InvestmentRequests.Add(investmentRequest);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await _publisher.Publish(
            new InvestmentRequested(userId, request.ProjectId, request.Amount, investmentRequest.Id, projectTitle),
            ct);

        return Accepted(new InvestResponse(investmentRequest.Id, request.ProjectId, request.Amount, investmentRequest.Status.ToString()));
    }

    private static ProjectResponse ToDto(Project p) =>
        new(p.Id, p.OwnerId, p.Title, p.TargetAmount, p.CurrentAmount);
}
