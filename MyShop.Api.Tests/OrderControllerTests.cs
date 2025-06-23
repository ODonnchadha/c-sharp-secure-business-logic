using Microsoft.AspNetCore.Mvc.Testing;
using MyShop.Core;
using MyShop.Domain.Models;
using NBomber.CSharp;
using NBomber.Http;
using NBomber.Http.CSharp;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace MyShop.Api.Tests;

[TestClass]
public sealed class OrderControllerTests
{
    private static int stock = 100;
    private static AsyncLocal<int?> value = new();
    [TestMethod]
    public void LeakyTest()
    {
        Parallel.For(0, 100, async i => {
            // This is a new Task each time, but is it a new thread?

            if (value.Value is not null) throw new Exception();

            value.Value = i;
        });
    }

    [TestMethod]
    public void AtomicTest()
    {
        stock = 100;

        Parallel.For(0, 100, _ => {
            // stock -= 1; // Not Thread Safe!

            Interlocked.Decrement(ref stock); // Thread safe!
        });

        Assert.AreEqual(0, stock);
    }

    [TestMethod]
    public void CanReserveStaticProuct()
    {
        // Reset the stock
        Inventory.Stock = 139;

        WebApplicationFactory<Program> factory = new();

        var client = factory.CreateClient();

        var scenario = Scenario.Create("Order_reserve_static_scenario",
            async context => {
                var request = Http.CreateRequest("POST",
                    "/order/reserve-static/d2dae150-9a29-4c78-b912-68ed66b056f0");
                var response = await Http.Send(client, request);

                return response;
        })
            .WithoutWarmUp()
            .WithLoadSimulations(Simulation.IterationsForConstant(100, 10000));

        var stats = NBomberRunner.RegisterScenarios(scenario)
            .WithWorkerPlugins(new HttpMetricsPlugin([HttpVersion.Version1]))
            .Run();

        var scenarioResult = stats.ScenarioStats.First();
        var okResponses = scenarioResult.Ok
            .StatusCodes
            .Single(s => s.StatusCode == "OK").Count;

        Assert.AreEqual(139, okResponses);
    }

    [TestMethod]
    public async Task CanReserveProuct()
    {
        WebApplicationFactory<Program> factory = new();

        var client = factory.CreateClient();

        var products = await client.GetFromJsonAsync<IEnumerable<Product>>("/product");
        var productToPurchase = products!.First();

        var scenario = Scenario.Create("Order_reserve_static_scenario",
            async context => {
                var request = Http.CreateRequest("POST",
                    $"/order/reserve/{productToPurchase.ProductId}");
                var response = await Http.Send(client, request);

                return response;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(Simulation.IterationsForConstant(100, 10000));

        var stats = NBomberRunner.RegisterScenarios(scenario)
            .WithWorkerPlugins(new HttpMetricsPlugin([HttpVersion.Version1]))
            .Run();

        var scenarioResult = stats.ScenarioStats.First();
        var okResponses = scenarioResult.Ok
            .StatusCodes
            .Single(s => s.StatusCode == "OK").Count;

        Assert.AreEqual(productToPurchase.Stock, okResponses);
    }

    [TestMethod]
    public void RateLimiterTest()
    {
        WebApplicationFactory<Program> factory = new();

        var client = factory.CreateClient();

        var scenario = Scenario.Create("Product_rate_limiter_scenario",
            async context =>
            {
                var request = Http.CreateRequest("GET", "/product");
                var response = await Http.Send(client, request);
                return response;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(10, 
                    interval: TimeSpan.FromSeconds(1), 
                    during: TimeSpan.FromSeconds(5)));

        var stats = NBomberRunner.RegisterScenarios(scenario)
            .WithWorkerPlugins(new HttpMetricsPlugin([HttpVersion.Version1]))
            .Run();

        var scenarioResult = stats.ScenarioStats.First();

        var okResponses = scenarioResult.Ok
            .StatusCodes
            .Single(s => s.StatusCode == "OK").Count;

        Assert.AreEqual(20, okResponses);
    }

    [TestMethod]
    public async Task GenerateYearlyReportTestAsync()
    {
        // Generate fake orders
        var orders = Enumerable.Range(0, 100)
            .Select(x => new Order
            {
                LineItems = [
                    new() { ProductId = Guid.NewGuid(), Quantity = 99 }
                ],
                OrderDate = DateTimeOffset.Now,
            })
            .ToArray();

        await Parallel.ForAsync(0, 1000, async (_, _) => {
            var report = 
                await OrderProcessor.GenerateYearlyReportForAsync(orders);
        });
    }
}
