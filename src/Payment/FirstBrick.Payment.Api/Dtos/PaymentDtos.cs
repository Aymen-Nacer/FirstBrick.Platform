using FirstBrick.Payment.Api.Domain;

namespace FirstBrick.Payment.Api.Dtos;

public record TopUpRequest(decimal Amount, string IdempotencyKey);
public record BalanceResponse(Guid UserId, decimal Balance);
public record TransactionDto(Guid Id, decimal Amount, string Type, string Status, Guid? InvestmentRequestId, DateTime CreatedAtUtc);
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public static class TransactionMapping
{
    public static TransactionDto ToDto(this Transaction t) =>
        new(t.Id, t.Amount, t.Type.ToString(), t.Status.ToString(), t.InvestmentRequestId, t.CreatedAtUtc);
}
