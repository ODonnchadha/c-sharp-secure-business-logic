namespace MyShop.Infrastructure.Services;

public class PaymentService
{
    public ValueTask<bool> ValidatePayment(string paymentReference)
    {
        return ValueTask.FromResult(true);
    }
}