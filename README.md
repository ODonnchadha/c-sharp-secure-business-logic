## Secure Business Logic for C# by Filip Ekberg

- SECURE BUSINESS LOGIC:
    - What is secure business logic?
        - Considerations:
            - Avoid race conditions:
                - Multiple threads fighting for the same resource.
            - Ensure the execution flow:
                - Execute code in order both in sychronous and asynchronous operations.
            - Handle errors:
                - ROlling back data when needed.
            - Thread safety:
                - Don't leak data.
    - Ensuring execution order:
        - Introduction patterns and abstraction needs to be thought through.
        - Race conditions. Who wins?
        - Multiple requests can occur simultaneously. Does this change code behavior?
            - Ensure sequention flow may *not* be enough.
    - Execution order in asynchronous processing:
        - Async/await: The control is returned back to the caller to execute the next line.
            - Internally. A state machine.
            - Asynchronous methods return tasks, or value tasks, which:
                - Allow to pause execution of the rest of the method.
                - Exceptions are "swallowed" inside the task.
                - Append a contonuation.
                - Check if it's completed.
        - Securing logic flow in asynchronous methods:
            - Use `await` to validate asynchronous operations and to pause further execution of that method.
            - Handle errors gracefully. Consider how to roll back data and clean up resources.
            - Ensures that the order processing occurs in an expected and regimented manner.
            - Each await validates the success/failure of its operation. It also returns controll back to the caller, introduction a continuation.
            - The action continues executing after completion. This ensures more requests can be handled. More scallable. But we need to consider race condition and perhaps rollback.
    - Handling failure: Rollback, transactions, and ensuring state:
        - Database transactions are a great way to handle potential rollbacks.
        - Ensuring state can be more complex.
        - Database: Source of truth.
        - Securing business logic flow:
            - Sequential & synchronous code:
                - Requires handling errors, rolling back, and ensuring state.
            - Asynchronous processing:
                - Use async/await to ensure execution order and success.
            - Task considerations.
                - Exception are wrapped inside the task. Always use await to validate.
            - Rollback, transactions, and ensuring state
                - Plan for failure and for attacks.
        - Ensuring the logic flow is crucial.
    - Race conditions:
        - May only reveal themselves occasionally or under heavy load.
        - NOTE: Static data is per process or thread.
        - Two calls at *exact* same time:
            ```cs
                var validCustomer = ValidateCustomer(customer);
                if (!validCustomer) return false;
                // Multiple incoming requests check the stock at approximately the same time.
                Thread.Sleep(20);
                if (Inventory.Stock < 0) return false;
                
            ```
        - NBomber & NBomber.Http: Load test any system with a distributed cluster:
            - Distributed load testing framework for C# .NET.
                - NOTE: 100 operations run simultaneously for 10,000 iterations.
                ```cs
                    [TestClass]
                    public sealed class OrderControllerTests
                    {
                        [TestMethod]
                        public void CanReserveStaticProduct()
                        {
                            Inventory.Stock = 139;
                            WebApplicationFactory<Program> factory = new();
                            var client = factory.CreateClient();
                            var scenario = Scenario.Create("Order_Reserve_Static_Scenario",
                            async context => {
                                var request = Http.CreateRequest("POST", $"/order/reserve/ff76975d-a558-4c7e-8e4d-062ee01b9499");
                                var response = await Http.Send(client, request);
                                return response;
                            })
                            .WithoutWarmuUp()
                            .WithLoadSimulations(Simulation.IterationsForConstant(100, 10000))

                            var stats = NBomberRunner.RegisterScenarios(scenario).WithWorkerPlugins(
                                new HttpMetricsPlugin([HttpVersion.Version1])).Run();
                            var scenarioResult = stats.ScenarioStats.First();
                            var okResponses = scenarioResult.Ok.StatusCodes.Single(s => s.StatusCode == "OK").Count;

                            Assert.AreEqual(139, okResponses);
                        }
                    }
                ```
                - e.g.: Expected: 139. Actual: 168. Race condition.
                - NOTE: We need to load the value before performing the arithmetic. So, two operations.
                ```cs
                    Inventory.Stock =-1; Inventory.Stock = Inventory.Stock - 1;
                ```
                ```cs
                    private static int stock = 100;
                    [TestMethod]
                    public void AtomicTest()
                    {
                        stock = 100;
                        Parallel.For(0, 100, _ => {
                            stock -= 1;
                        });
                        Assert.AreEqual(0, stock);
                    }
                ```
                - e.g.: Expected: 0. Actual: 6. Race condition.
                - NOTE: Not atomic. Not thread safe. Operation is not read and subtracted within the same operation.
                - Only one thread should modify the value at a gievn time. `A shared resource.`
                ```cs
                    private static int stock = 100;
                    [TestMethod]
                    public void AtomicTest()
                    {
                        stock = 100;
                        Parallel.For(0, 100, _ => {
                            Interlocked.Decrement(ref stock);
                        });
                        Assert.AreEqual(0, stock);
                    }
                ```
                - NOTE: Thread-safe approach to working with a shared variable.
            - The concept of a lock. Only one thread executes at a time.
                - This new Lock object was introduced in .NET 9.
                - NOTE: Don't share Locks with others. Per each use case.
                - NOTE: Perform as little work as possible inside a lock!
                - NOTE: Locking is not supported in asynchronous code.
                    ```cs
                        private static Lock reserveStockLock = new();
                        public bool TryReserveProductInStock(Guid id, Customer c)
                        {
                            var validCustomer = ValidateCustomer(customer);
                            if (!validCustomer) return false;
                            lock (reserveStockLock) 
                            {
                                if (Inventory.Stock =< 0) return false;
                                Inventory.Stock = Inventory.Stock - 1;
                            }
                            var order = new Order(id, c);
                        }
                    ```
            - The concept of semaphores with race conditions:
                - Limits the number of threads that can access a resource or pool of resources concurrently.
                    ```cs
                        // One thread at a time.
                        static SemaphoreSlim semaphore = new(1);
                        try
                        {
                            // Only one thread at a time.
                            await semaphore.WaitAsync();
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    ```
                    ```cs
                        private static SemaphoreSlim reserveStockSemaphore = new(1);
                        public async Task<Result> Reserve(Guid id, Customer c)
                        {
                            var customerValidationTask = ValidateCustomerAsync(customer);
                            var customerIsValid = await customerValidationTask;
                            if (!customerIsValid) return new Failure<Customer>(c, Error.Value);

                            await reserveStockSemaphore.WaitAsync();

                            // Only one thread at a time. This may hurt scalability.
                            try
                            {
                                var product = await productRepository.GetAsync(id);
                                if (product is null) return new Failure<Product>(Error.Value);
                                if (product.Stock <= 0) return new Failure<Product>(Error.Value);
                                product.Stock = product.Stock - 1;
                                var order = new Order(id, c);
                                await orderRepository.UpdateAsync(order);
                                await UpdateInventoryForAsync(product);
                                await orderRepository.SaveChangesAsync();
                                return new Success<Order>(order);
                            }
                            finally
                            {
                                reserveStockSemaphore.Release();
                            }
                        }
                    ```
            - e.g.: 
                - System.Collections.Concurrent.ConcurrentDictionary<>();
                - System.Collections.Concurrent.ConcurrentQueue<>();
                - System.Collections.Concurrent.ConcurrentStack<>();
        - Using a Queue:
            - API or front-end: Enqueue
            - Background processor: Dequeue
                - Validate stock. Validate payment. Validate customer.
                - Save order.
                - Notify customer.
        - Example of a thread-safe collection: Queue
            ```cs
                using System.Collections.Concurrent;
                ConcurrentQueue<Order> queue = new();
                queue.Enqueue(order);
                // Inside the worker that process the queue:
                queue.TryDequeue(out var nextOrderToProcess);
            ```
        - Combine a thread-safe queue with an external message queue/bus to mitigate race conditions:
            - The concurrent collections are thread-safe and block others from adding or removing data at the same time.
            - Order processing is the perfect example of how a concurrent queue can be introduced to wrote thread-safe code.
    - Data Leaks & threads:
        - Tasks in .NET need to be as effective as possible.
            - Tasks reuse threads if possible. Not a guarenteee that it runs on a new thread.
            - Cleaning all states or always creating a completely new thread is an expensive operation.
            - NOTE: Beware static data.
        ```cs
            private static ThreadLocal<Customer> currentCustomer = new();
            if (currentCustomer.Value is not null) throw new Exception();
        ```
        - DO not rely on thread-local data. We may not receive a new thread.