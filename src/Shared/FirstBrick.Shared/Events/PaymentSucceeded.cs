namespace FirstBrick.Shared.Events;

public record PaymentSucceeded(Guid UserId, Guid ProjectId, decimal Amount, Guid RequestId, string ProjectTitle);
