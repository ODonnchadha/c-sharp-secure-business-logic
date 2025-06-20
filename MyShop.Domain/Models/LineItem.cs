namespace MyShop.Domain.Models;
public class LineItem
{
    public Guid LineItemId { get; init; } = Guid.NewGuid();

    public int Quantity { get; set; }

    public virtual required Product Product { get; set; }
    public Guid ProductId { get; set; }

    public virtual required Order Order { get; set; }
    public Guid OrderId { get; set; }
}