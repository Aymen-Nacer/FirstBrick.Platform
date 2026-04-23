namespace FirstBrick.Portfolio.Api.Domain;

public class ProcessedInvestment
{
    public Guid RequestId { get; set; }
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
}
