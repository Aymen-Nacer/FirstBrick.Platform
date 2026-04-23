using FirstBrick.Investment.Api.Data;
using FirstBrick.Investment.Api.Domain;
using FirstBrick.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Investment.Api.Consumers;

public class PaymentFailedConsumer : IConsumer<PaymentFailed>
{
    private readonly InvestmentDbContext _db;
    private readonly ILogger<PaymentFailedConsumer> _logger;

    public PaymentFailedConsumer(InvestmentDbContext db, ILogger<PaymentFailedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailed> context)
    {
        var msg = context.Message;

        await using var tx = await _db.Database.BeginTransactionAsync(context.CancellationToken);

        var req = await _db.InvestmentRequests.FirstOrDefaultAsync(r => r.Id == msg.RequestId, context.CancellationToken);
        if (req is null)
        {
            _logger.LogWarning("PaymentFailed for unknown InvestmentRequest {RequestId}", msg.RequestId);
            return;
        }

        if (req.Status == InvestmentStatus.Failed) return; // idempotent

        req.Status = InvestmentStatus.Failed;

        // Release the reservation
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE ""Projects"" SET ""CurrentAmount"" = ""CurrentAmount"" - {msg.Amount} WHERE ""Id"" = {msg.ProjectId}",
            context.CancellationToken);

        await _db.SaveChangesAsync(context.CancellationToken);
        await tx.CommitAsync(context.CancellationToken);

        _logger.LogInformation("Released reservation for failed investment {RequestId}: {Reason}", msg.RequestId, msg.Reason);
    }
}
