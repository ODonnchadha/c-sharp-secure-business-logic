using System.ComponentModel.DataAnnotations;

namespace MyShop.Domain.Models;

public class Customer
{
    public Guid CustomerId { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    [MaxLength(200)]
    public required string Email { get; set; }

    public required string ShippingAddress { get; set; }
    public required string City { get; set; }
    public required string PostalCode { get; set; }
    public required string Country { get; set; }
}