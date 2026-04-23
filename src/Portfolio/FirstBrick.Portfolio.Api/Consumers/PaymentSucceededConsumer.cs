using FirstBrick.Portfolio.Api.Data;
using FirstBrick.Portfolio.Api.Domain;
using FirstBrick.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Portfolio.Api.Consumers;

public class PaymentSucceededConsumer : IConsumer<PaymentSucceeded>
{
    private readonly PortfolioDbContext _db;
    private readonly ILogger<PaymentSucceededConsumer> _logger;

    public PaymentSucceededConsumer(
        PortfolioDbContext db,
        ILogger<PaymentSucceededConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentSucceeded> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Dedupe on RequestId: INSERT ... ON CONFLICT DO NOTHING. 0 rows => already applied, skip.
        var inserted = await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO ""ProcessedInvestments"" (""RequestId"", ""UserId"", ""ProjectId"", ""AppliedAtUtc"")
               VALUES ({msg.RequestId}, {msg.UserId}, {msg.ProjectId}, {DateTime.UtcNow})
               ON CONFLICT (""RequestId"") DO NOTHING",
            ct);

        if (inserted == 0)
        {
            _logger.LogInformation("PaymentSucceeded {RequestId} already applied; skipping", msg.RequestId);
            await tx.RollbackAsync(ct);
            return;
        }

        var entry = await _db.PortfolioEntries
            .FirstOrDefaultAsync(e => e.UserId == msg.UserId && e.ProjectId == msg.ProjectId, ct);

        // ProjectTitle comes from the event itself — no sync call to Investment.
        if (entry is null)
        {
            entry = new PortfolioEntry
            {
                UserId = msg.UserId,
                ProjectId = msg.ProjectId,
                TotalInvested = msg.Amount,
                ProjectTitle = msg.ProjectTitle,
                LastUpdatedUtc = DateTime.UtcNow
            };
            _db.PortfolioEntries.Add(entry);
        }
        else
        {
            entry.TotalInvested += msg.Amount;
            entry.ProjectTitle = msg.ProjectTitle;
            entry.LastUpdatedUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
