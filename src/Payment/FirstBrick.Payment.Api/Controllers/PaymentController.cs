using FirstBrick.Payment.Api.Data;
using FirstBrick.Payment.Api.Domain;
using FirstBrick.Payment.Api.Dtos;
using FirstBrick.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Payment.Api.Controllers;

[ApiController]
[Route("v1")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PaymentDbContext db, ILogger<PaymentController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("ApplepayTopup")]
    public async Task<IActionResult> TopUp([FromBody] TopUpRequest request, CancellationToken ct)
    {
        if (request.Amount <= 0)
            return BadRequest(new { error = "Amount must be positive" });
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return BadRequest(new { error = "IdempotencyKey required" });

        var userId = User.GetUserId();

        // Idempotency: return original result if already processed.
        var existing = await _db.Transactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null)
            return Ok(existing.ToDto());

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
            if (wallet is null)
            {
                wallet = new Wallet { UserId = userId, Balance = 0 };
                _db.Wallets.Add(wallet);
            }

            wallet.Balance += request.Amount;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = request.Amount,
                Type = TransactionType.TopUp,
                Status = TransactionStatus.Success,
                IdempotencyKey = request.IdempotencyKey,
                InvestmentRequestId = null,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.Transactions.Add(transaction);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(transaction.ToDto());
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Transactions_IdempotencyKey") == true)
        {
            await tx.RollbackAsync(ct);
            var original = await _db.Transactions
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey, ct);
            return original is not null ? Ok(original.ToDto()) : Conflict();
        }
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var wallet = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct);
        return Ok(new BalanceResponse(userId, wallet?.Balance ?? 0m));
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var userId = User.GetUserId();
        var query = _db.Transactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => t.ToDto())
            .ToListAsync(ct);

        return Ok(new PagedResult<TransactionDto>(items, page, pageSize, total));
    }
}
