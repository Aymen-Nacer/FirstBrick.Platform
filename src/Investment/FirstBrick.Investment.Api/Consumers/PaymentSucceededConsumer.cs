using FirstBrick.Investment.Api.Data;
using FirstBrick.Investment.Api.Domain;
using FirstBrick.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Investment.Api.Consumers;

public class PaymentSucceededConsumer : IConsumer<PaymentSucceeded>
{
    private readonly InvestmentDbContext _db;
    private readonly ILogger<PaymentSucceededConsumer> _logger;

    public PaymentSucceededConsumer(InvestmentDbContext db, ILogger<PaymentSucceededConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentSucceeded> context)
    {
        var msg = context.Message;
        var req = await _db.InvestmentRequests.FirstOrDefaultAsync(r => r.Id == msg.RequestId, context.CancellationToken);
        if (req is null)
        {
            _logger.LogWarning("PaymentSucceeded for unknown InvestmentRequest {RequestId}", msg.RequestId);
            return;
        }

        if (req.Status == InvestmentStatus.Success) return; // idempotent

        req.Status = InvestmentStatus.Success;
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
