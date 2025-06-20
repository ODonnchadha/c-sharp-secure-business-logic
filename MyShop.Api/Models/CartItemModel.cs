namespace MyShop.Api.Models;

public class CartItemModel
{
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
}
