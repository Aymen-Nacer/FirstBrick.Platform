namespace FirstBrick.Shared.Events;

public record InvestmentRequested(Guid UserId, Guid ProjectId, decimal Amount, Guid RequestId, string ProjectTitle);
