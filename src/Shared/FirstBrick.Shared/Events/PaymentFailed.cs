namespace FirstBrick.Shared.Events;

public record PaymentFailed(Guid UserId, Guid ProjectId, decimal Amount, Guid RequestId, string ProjectTitle, string Reason);
