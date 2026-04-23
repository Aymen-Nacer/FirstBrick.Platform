using FirstBrick.Payment.Api.Data;
using FirstBrick.Payment.Api.Domain;
using FirstBrick.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Payment.Api.Consumers;

public class InvestmentRequestedConsumer : IConsumer<InvestmentRequested>
{
    private readonly PaymentDbContext _db;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<InvestmentRequestedConsumer> _logger;

    public InvestmentRequestedConsumer(
        PaymentDbContext db,
        IPublishEndpoint publisher,
        ILogger<InvestmentRequestedConsumer> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InvestmentRequested> context)
    {
        var msg = context.Message;
        var idempotencyKey = msg.RequestId.ToString();

        // Idempotent retry guard
        var existing = await _db.Transactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, context.CancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Duplicate InvestmentRequested {RequestId} skipped, status={Status}",
                msg.RequestId, existing.Status);
            return;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(context.CancellationToken);

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == msg.UserId, context.CancellationToken);
        if (wallet is null)
        {
            wallet = new Wallet { UserId = msg.UserId, Balance = 0 };
            _db.Wallets.Add(wallet);
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = msg.UserId,
            Amount = msg.Amount,
            Type = TransactionType.Investment,
            Status = TransactionStatus.Pending,
            IdempotencyKey = idempotencyKey,
            InvestmentRequestId = msg.RequestId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Transactions.Add(transaction);

        if (wallet.Balance >= msg.Amount)
        {
            wallet.Balance -= msg.Amount;
            transaction.Status = TransactionStatus.Success;
            await _db.SaveChangesAsync(context.CancellationToken);
            await tx.CommitAsync(context.CancellationToken);

            await _publisher.Publish(
                new PaymentSucceeded(msg.UserId, msg.ProjectId, msg.Amount, msg.RequestId, msg.ProjectTitle),
                context.CancellationToken);
        }
        else
        {
            transaction.Status = TransactionStatus.Failed;
            await _db.SaveChangesAsync(context.CancellationToken);
            await tx.CommitAsync(context.CancellationToken);

            await _publisher.Publish(
                new PaymentFailed(msg.UserId, msg.ProjectId, msg.Amount, msg.RequestId, msg.ProjectTitle, "InsufficientFunds"),
                context.CancellationToken);
        }
    }
}
