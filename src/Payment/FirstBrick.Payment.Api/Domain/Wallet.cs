namespace FirstBrick.Payment.Api.Domain;

public class Wallet
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
}
