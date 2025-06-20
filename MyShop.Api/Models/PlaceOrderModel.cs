using MyShop.Domain.Models;

namespace MyShop.Api.Models;

public class PlaceOrderModel
{
    public required IEnumerable<CartItemModel> Items { get; init; }
    public required CustomerModel Customer { get; init; }
    public required PaymentType PaymentType { get; init; }
}
