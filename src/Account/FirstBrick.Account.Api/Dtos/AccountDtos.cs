namespace FirstBrick.Account.Api.Dtos;

public record RegisterRequest(string Username, string Password, string FullName, string? Role);
public record LoginRequest(string Username, string Password);
public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);
public record UserResponse(Guid Id, string Username, string FullName, string Role);
public record UpdateUserRequest(string FullName);
