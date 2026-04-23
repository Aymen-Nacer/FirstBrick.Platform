namespace FirstBrick.Investment.Api.Domain;

public enum InvestmentStatus { Pending, Success, Failed }

public class InvestmentRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public decimal Amount { get; set; }
    public InvestmentStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
