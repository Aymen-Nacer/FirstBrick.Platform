namespace FirstBrick.Portfolio.Api.Domain;

public class PortfolioEntry
{
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public decimal TotalInvested { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
