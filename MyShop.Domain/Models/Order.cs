namespace MyShop.Domain.Models;

public class Order
{
    public Guid OrderId { get; init; } = Guid.NewGuid();

    public virtual ICollection<LineItem> LineItems { get; set; } = new List<LineItem>();

    public virtual Customer? Customer { get; set; }
    public Guid CustomerId { get; set; }

    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;

    public decimal OrderTotal => LineItems.Sum(item => item.Product.Price * item.Quantity);

    public OrderStatus Status { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum OrderStatus
{
    Open,
    Cancelled,
    WaitingForPayment,
    FullyPaid,
    Shipped
}