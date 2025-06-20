namespace MyShop.Domain.Models;

public class Payment
{
    public Guid PaymentId { get; init; } = Guid.NewGuid();

    public virtual required Order Order { get; set; }
    public Guid OrderId { get; init; }

    public decimal Amount { get; init; }

    public PaymentType Type { get; init; }
    public PaymentStatus Status { get; set; }
}

public enum PaymentType
{
    Cash,
    Invoice,
    CreditCard
}

public enum PaymentStatus
{
    Initialized,
    Timeout,
    Finalized
}