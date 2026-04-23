using FirstBrick.Account.Api.Data;
using FirstBrick.Account.Api.Domain;
using FirstBrick.Account.Api.Dtos;
using FirstBrick.Account.Api.Services;
using FirstBrick.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Account.Api.Controllers;

[ApiController]
[Route("v1")]
public class UsersController : ControllerBase
{
    private readonly AccountDbContext _db;
    private readonly TokenService _tokens;

    public UsersController(AccountDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("user")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { error = "Username, password and full name are required" });

        var role = request.Role == Roles.ProjectOwner ? Roles.ProjectOwner : Roles.User;

        if (await _db.Users.AnyAsync(u => u.Username == request.Username, ct))
            return Conflict(new { error = "Username already taken" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Role = role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { user_id = user.Id },
            new UserResponse(user.Id, user.Username, user.FullName, user.Role));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials" });

        var (token, expiresAt) = _tokens.CreateToken(user);
        return Ok(new LoginResponse(token, expiresAt));
    }

    [HttpGet("user/{user_id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById([FromRoute(Name = "user_id")] Guid user_id, CancellationToken ct)
    {
        if (User.GetUserId() != user_id) return Forbid();

        var user = await _db.Users.FindAsync(new object?[] { user_id }, ct);
        if (user is null) return NotFound();

        return Ok(new UserResponse(user.Id, user.Username, user.FullName, user.Role));
    }

    [HttpPut("user/{user_id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute(Name = "user_id")] Guid user_id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        if (User.GetUserId() != user_id) return Forbid();

        var user = await _db.Users.FindAsync(new object?[] { user_id }, ct);
        if (user is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { error = "FullName required" });

        user.FullName = request.FullName.Trim();
        await _db.SaveChangesAsync(ct);

        return Ok(new UserResponse(user.Id, user.Username, user.FullName, user.Role));
    }
}
