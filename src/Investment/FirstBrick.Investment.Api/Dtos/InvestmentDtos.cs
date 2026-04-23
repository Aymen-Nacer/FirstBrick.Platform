namespace FirstBrick.Investment.Api.Dtos;

public record CreateProjectRequest(string Title, decimal TargetAmount);
public record ProjectResponse(Guid Id, Guid OwnerId, string Title, decimal TargetAmount, decimal CurrentAmount);
public record InvestRequest(Guid ProjectId, decimal Amount);
public record InvestResponse(Guid RequestId, Guid ProjectId, decimal Amount, string Status);
