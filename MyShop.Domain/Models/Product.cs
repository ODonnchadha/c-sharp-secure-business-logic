﻿namespace MyShop.Domain.Models;

public class Product
{
    public Guid ProductId { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }
}