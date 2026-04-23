namespace FirstBrick.Payment.Api.Domain;

public enum TransactionType { TopUp, Investment }
public enum TransactionStatus { Pending, Success, Failed }

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid? InvestmentRequestId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
