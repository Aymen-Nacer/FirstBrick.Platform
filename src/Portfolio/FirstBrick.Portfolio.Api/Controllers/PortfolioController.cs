using FirstBrick.Portfolio.Api.Data;
using FirstBrick.Portfolio.Api.Dtos;
using FirstBrick.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Portfolio.Api.Controllers;

[ApiController]
[Route("v1")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly PortfolioDbContext _db;

    public PortfolioController(PortfolioDbContext db)
    {
        _db = db;
    }

    [HttpGet("portfolio")]
    public async Task<IActionResult> GetPortfolio(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var entries = await _db.PortfolioEntries.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.LastUpdatedUtc)
            .Select(e => new PortfolioEntryDto(e.ProjectId, e.ProjectTitle, e.TotalInvested, e.LastUpdatedUtc))
            .ToListAsync(ct);

        return Ok(entries);
    }

    [HttpGet("portfolio/{project_id:guid}")]
    public async Task<IActionResult> GetById([FromRoute(Name = "project_id")] Guid project_id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var entry = await _db.PortfolioEntries.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.ProjectId == project_id, ct);
        if (entry is null) return NotFound();

        return Ok(new PortfolioEntryDto(entry.ProjectId, entry.ProjectTitle, entry.TotalInvested, entry.LastUpdatedUtc));
    }
}
