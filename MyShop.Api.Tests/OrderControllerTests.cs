namespace MyShop.Api.Tests;

[TestClass]
public sealed class OrderControllerTests
{
    private static int stock = 100;

    [TestMethod]
    public void AtomicOperationTest()
    {
        stock = 100;

        Parallel.For(0, 100, _ => {
            // stock -= 1;
            System.Threading.Interlocked.Add(ref stock, -1);
        });

        Assert.AreEqual(0, stock);
    }
}
