# .Net Unit Testing 

## What is Unit Testing? 

For the purposes of this discussion we're going to define **Unit Testing** as writing automated functional tests in code that involve no external dependencies.  If these tests were to include things like accessing a database, calling another service, etc then it would be more along the lines of an **Integration Testing**. 
> The terms **Unit Test** & **Integration Test** are sometimes defined differently, but for the purposes of this exploration we will be adopting the above definitions. 

## What are the motivations behind Unit Testing? 

In the simplest terms - *to help the humans*

* Confidence in knowing that what has been developed meets the acceptance criteria. 

* Confidence that bugs won't regress and be found again

* Readability of the code. Test can help *document* the code base allowing new developers and even your future to better understand the code.

* Maintainability of the code. Unit tests can reduce the fear of change. Enhancements, refactorings and bug fixes are all things that happen in the course of a piece of software's lifetime. We shouldn't have to live in fear of making changes. 

* Reasonability. Unit tests can help us reason about the code to better understand the code provide better estimates for future changes and be more targeting in reproducing/fixing bugs.

* Communication. We can name tests and assertions in plain language that can match the requirements and allow us to have better collaboration with a variety of audiences. 

* Write better code. Not all but a lot of poorly structured code is difficult to unit test properly. 


## How do we write Unit Tests in .Net? 

### xUnit 

We will use **xUnit** as our unit testing framework of choice. It has a long, well maintained history and integrates nicely in the .net ecosystem. 

The major feature of **xUnit** that we're intrested is leveraging its *[attributes]* that allows our build tooling to recognize and execute code as unit tests.  

**xUnit** provides us with a `[Fact]` attribute that is used to indicate test that is always true. It also provides `[Theory]` which indicates that a test is only true for a particular set of data. 

The following is an example of a `[Fact]`

``` csharp 
    [Fact]
    public void Fact_Test()
    {
        var calculator = new Calculator("la calculadora"); 

        var result = calculator.Sum(1, 1);

        Assert.Equal(2, result);
    }
``` 

In the above test we've created an instance of the `Calculator` class. It is our SUT (Subject Under Test).  This test expects that the assertion will always hold true for the give call to the calulator's `Sum` method. 

Testing the `Sum` method as a `[Theory]` providing different values to the same tests. Each set of inputs will be evaluated as a separate test run. 
``` csharp
    [Theory]
    [InlineData(2147483646, 1, 2147483647)] 
    [InlineData(2147483647, 1, 2147483648)] 
    public void Theory_Test(int a, int b, int answer)
    {
        var calculator = new Calculator("la calculadora"); 

        var result = calculator.Sum(a, b);

        Assert.Equal(answer, result);
    }
```

So far we have been making use of XUnit's `[InlineData]` attribute in building `[Theory]` unit test, but we can also get our test data sets from sources other that attributes. Using XUnit's `[MemberData]` attribute we can specify a data source. In this setup we will be providing a collection of arrays of objects.

```csharp
    public static IEnumerable<object[]> Data =>
    new List<object[]>
    {
        new object[] { 2147483646, 1, 2147483647 },
        new object[] { 2147483647, 1, 2147483648 },
    };
```

Now that we have our `Data` we can tell XUnit that we would like to use it with our `[Theory]`. Since we are using the same data as our previous `[Theory]`'s our test method will still required a single input parameter to pass the data in on each test iteration. 

``` csharp
    [Theory]
    [MemberData(nameof(Data))]
    public void Theory_Test(int a, int b, int answer)
    {
        var calculator = new Calculator("la calculadora"); 

        var result = calculator.Sum(a, b);

        Assert.Equal(answer, result);
    }
```
A `[ClassData]` attribute is also provided by **xUnit**. The usage details for these are left to your exploration should the need arise. 



*Suddenly a common pattern emerges*

``` csharp
    [Fact]
    public void Fact_Test()
    {
        // Arrange
        var calculator = new Calculator("la calculadora"); 

        // Act
        var result = calculator.Sum(1, 1);

        // Assert 
        Assert.Equal(2, result);
    }
```

We *Arrange* the test by creating an instance of our SUT(Subject Under Test) the `Calculator`.  If we had any other dependencies to satisfy this is where we would handle that. 

Now we can *Act* on the **SUT** by calling its `Sum()` method.  

We gain value from our tests by *Assert*ing that something in particular has happened and we have received the expected result.  Being specific and intentional with our Asserts is important.  Code coverage numbers can be achieved through *Arrange* & *Act* but the value of a test is realized in the quality of its *Assert*(ions). 


### Moq

Imagine for a moment that we have the world's greatest ordering system

``` csharp 
public class OrdersController
{
    private readonly IPaymentService _paymentService;
    private readonly IShippingService _shippingService;
    private readonly IAuditLogger _auditLogger;

    public OrdersController(IPaymentService paymentService, 
                            IShippingService shippingService, 
                            IAuditLogger auditLogger)
    {
        _paymentService = paymentService;
        _shippingService = shippingService;
        _auditLogger = auditLogger;
    }

    public OrderResponse SubmitOrder(Order order)
    {
        OrderResponse response;

        try
        {
            var paymentResult = _paymentService.Pay(order);
            ShippingResult shippingResult = null;

            if (paymentResult.Success)
            {
                shippingResult = _shippingService.Ship(order);
            }

            response = new OrderResponse
            {
                Success = paymentResult.Success && shippingResult.Success,
                PaymentResult = paymentResult,
                ShippingResult = shippingResult
            };

            _auditLogger.LogOrder(order, response);
        }
        catch (Exception)
        {
            response = new OrderResponse { Success = false };
        }

        return response;
    }
}
```
We would like to test the behavior that the order is sent over to the shipping service once the customer has successfully paid for the order. In order to accomplish this we will want an actual concrete instance of the OrdersController, but everything it depends on can be mocks.

Creating Mocks with Moq is really simple.
```csharp
var mock = new Mock<string>();
```
That's it! Now we have a mock in hand, but what does that give us?
```csharp
mock.Object; // Provides us the instance of the mocked type to pass around and interact with
mock.Setup(...); // Setup allows us to implement behaviors on methods and properties of the mock
mock.Verify(...); // Verify allows us to assert that certain interactions with the mock instance occurred. 
```

A so-so test
```csharp 
[Fact]
public void Ship_Stuff()
{
    var mockShippingService = new Mock<IShippingService>();
    var mockAuditLogger = new Mock<IAuditLogger>();
    var mockPaymentService = new Mock<IPaymentService>();

    var ordersController = new OrdersController(
        mockPaymentService.Object,
        mockShippingService.Object,
        mockAuditLogger.Object);

    var order = new Order();
    var response = ordersController.SubmitOrder(order);

    Assert.NotNull(response);
}
```

We can improve our test a bit but making a stronger assertion. 
```csharp 
[Fact]
public void Should_Ship_Order_When_Payment_Successful()
{
    var mockShippingService = new Mock<IShippingService>();
    var mockAuditLogger = new Mock<IAuditLogger>();
    var mockPaymentService = new Mock<IPaymentService>();

    var ordersController = new OrdersController(
        mockPaymentService.Object,
        mockShippingService.Object,
        mockAuditLogger.Object);

    mockPaymentService.Setup(paymentService => paymentService.Pay(It.IsAny<Order>()))
        .Returns(new PaymentResult
        {
            Success = true
        });

    mockShippingService.Setup(shippingService => shippingService.Ship(It.IsAny<Order>()))
        .Returns(new ShippingResult
        {
            Success = true
        });

    var order = new Order();
    var response = ordersController.SubmitOrder(order);

    Assert.NotNull(response);
    Assert.True(response.ShippingResult.Success);
}
```


We can leverage Moq.Verify() to write a much better test. 
```csharp 
[Fact]
public void Verify_Order_Shipped_When_Payment_Successful()
{
    var mockShippingService = new Mock<IShippingService>();
    var mockAuditLogger = new Mock<IAuditLogger>();
    var mockPaymentService = new Mock<IPaymentService>();

    var ordersController = new OrdersController(
        mockPaymentService.Object,
        mockShippingService.Object,
        mockAuditLogger.Object);

    mockPaymentService.Setup(paymentService => paymentService.Pay(It.IsAny<Order>()))
        .Returns(new PaymentResult
        {
            Success = true
        });

    mockShippingService.Setup(shippingService => shippingService.Ship(It.IsAny<Order>()))
        .Returns(new ShippingResult
        {
            Success = true
        });

    var order = new Order();
    var response = ordersController.SubmitOrder(order);

    Assert.NotNull(response);
    Assert.True(response.ShippingResult.Success);

    mockShippingService.Verify(shippingService => shippingService.Ship(It.IsAny<Order>()), Times.Once);
}
```

Now that we have tested that the `OrdersController` properly calls the `ShippingService`, when the `PaymentService` call is successful, we should test that the opposite is enforced. The failure scenario is effectively the same test but with different **Expected** and **Actual** values.  The previous *XUnit* `[Fact]` tests a single specific scenario, but by attributing our test method as a `[Theory]` we can provide a data set to test a range of values.

```csharp
[Theory]
[InlineData(true)]
[InlineData(false)]
public void Should_Only_Call_Ship_Order_On_Successful_Payment(bool isSuccessOrder)
{
    var mockShippingService = new Mock<IShippingService>();
    var mockAuditLogger = new Mock<IAuditLogger>();
    var mockPaymentService = new Mock<IPaymentService>();

    var ordersController = new OrdersController(
        mockPaymentService.Object,
        mockShippingService.Object,
        mockAuditLogger.Object);

    mockPaymentService.Setup(paymentService => paymentService.Pay(It.IsAny<Order>()))
        .Returns(new PaymentResult
        {
            Success = isSuccessOrder
        });

    mockShippingService.Setup(shippingService => shippingService.Ship(It.IsAny<Order>()))
        .Returns(new ShippingResult
        {
            Success = isSuccessOrder
        });

    var order = new Order();
    ordersController.SubmitOrder(order);

    mockShippingService.Verify(
        shippingService => shippingService.Ship(It.IsAny<Order>()),
        isSuccessOrder ? Times.Once() : Times.Never());
}
```

**Moq** allows us to `Verify` that a mocked method was called with particular values by using `It.Is<T>()`. In this test we want to make sure that the `AuditLogger` was called in both the Success and Failure cases. We would like to make our test a bit stronger than that and ensure that if the failure is due to a payment failure that the failure is reflected in the `OrderResponse` that returns from the `OrderController`. 

```csharp
[Theory]
[InlineData(true)]
[InlineData(false)]
public void Should_Call_Audit_Logger_When_Order_Attempted(bool isSuccessOrder)
{
    var mockShippingService = new Mock<IShippingService>();
    var mockAuditLogger = new Mock<IAuditLogger>();
    var mockPaymentService = new Mock<IPaymentService>();

    var ordersController = new OrdersController(
        mockPaymentService.Object,
        mockShippingService.Object,
        mockAuditLogger.Object);

    mockPaymentService.Setup(paymentService => paymentService.Pay(It.IsAny<Order>()))
        .Returns(new PaymentResult
        {
            Success = isSuccessOrder
        });

    mockShippingService.Setup(shippingService => shippingService.Ship(It.IsAny<Order>()))
        .Returns(new ShippingResult
        {
            Success = isSuccessOrder
        });

    var order = new Order { PaymentMethod = "SuperCard" };
    ordersController.SubmitOrder(order);

    mockAuditLogger.Verify(al => al.LogOrder( 
        It.IsAny<Order>(),
        It.Is<OrderResponse>(or => or.Success == isSuccessOrder)));
}
```

The previous test is sufficient in its functionality, but it lacks clarity in the assertion if the test should ever fail. If the `mockAuditLogger.Verify` fails then we are presented with this error messge.
```csharp
Message: 
    Moq.MockException : 
    Expected invocation on the mock at least once, but was never performed: 
        al => al.LogOrder(It.IsAny<Order>(), It.Is<OrderResponse>(or => or.Success == False))
    
    Performed invocations:
       Mock<IAuditLogger:1> (al):
          IAuditLogger.LogOrder(Order, OrderResponse)
```
The test tells us what failed, but it doesn't tell us why. In other words we know what was expected, but we already knew this because we wrote the test. What a test written this way fails to tell us is what was the actual value of the `Success` of the `OrderResponse` that was given to our `mockAuditLogger` as a parameter. 

We can use a **Moq Callback** to help us capture the actual value that is being passed between the mock objects. In this case we add a `.Callback()` to our `AuditLogger` mock setup.

```csharp
 mockAuditLogger.Setup(al => al.LogOrder(It.IsAny<Order>(), It.IsAny<OrderResponse>()))
                           .Callback<Order, OrderResponse>((o, or) => actualResult = or.PaymentResult.Success);
```
The `Callback()` gives you a chance to inspect the parameters being passed to the mocked method call. We are going to use that opportunity to capture the value we are interested in as the `actualResult` so that a failure message can include both *expected* and *actual* values.
```csharp
[Theory]
[InlineData(true)]
[InlineData(false)]
public void Should_Call_Audit_Logger_When_Order_Attempted_WithFailMessage(bool isSuccessOrder)
{
    var mockShippingService = new Mock<IShippingService>();
    var mockAuditLogger = new Mock<IAuditLogger>();
    var mockPaymentService = new Mock<IPaymentService>();

    var ordersController = new OrdersController(
        mockPaymentService.Object,
        mockShippingService.Object,
        mockAuditLogger.Object);

    mockPaymentService.Setup(paymentService => paymentService.Pay(It.IsAny<Order>()))
        .Returns(new PaymentResult
        {
            Success = !isSuccessOrder // intentionally incorrect to demonstration failure message
        });

    mockShippingService.Setup(shippingService => shippingService.Ship(It.IsAny<Order>()))
        .Returns(new ShippingResult
        {
            Success = !isSuccessOrder // intentionally incorrect to demonstration failure message
        });

    var actualResult = false;
    mockAuditLogger.Setup(al => al.LogOrder(It.IsAny<Order>(), It.IsAny<OrderResponse>()))
                    .Callback<Order, OrderResponse>((o, or) => actualResult = or.Success);

    var order = new Order();
    ordersController.SubmitOrder(order);

    mockAuditLogger.Verify(al => al.LogOrder(
        It.IsAny<Order>(),
        It.Is<OrderResponse>(or => or.Success == isSuccessOrder)), 
        $"Expected AuditLog with OrderResponse.Success == {isSuccessOrder} but was {actualResult}");
}
```

Now the previous test provides us more visibility into what went wrong with our test case. The failure message makes our expected and actual values clear.
```csharp
Message: 
    Moq.MockException : Expected AuditLog with OrderResponse.Success == False but was True
    Expected invocation on the mock at least once, but was never performed: 
        al => al.LogOrder(It.IsAny<Order>(), It.Is<OrderResponse>(or => or.Success == False))
    
    Performed invocations:
       Mock<IAuditLogger:4> (al):
          IAuditLogger.LogOrder(Order, OrderResponse)
```

Sometimes we want to know how our code will behave should a component that it depends on raises an exception. Moq gives us an easy way of setting up mocks such that when invoked they will throw a particular exception.

```csharp
[Fact]
public void Should_Verify_Shipping_Exception_Is_Handled()
{
    var mockShippingService = new Mock<IShippingService>();
    var mockAuditLogger = new Mock<IAuditLogger>();
    var mockPaymentService = new Mock<IPaymentService>();

    var ordersController = new OrdersController(
        mockPaymentService.Object,
        mockShippingService.Object,
        mockAuditLogger.Object);

    mockPaymentService.Setup(paymentService => paymentService.Pay(It.IsAny<Order>()))
        .Returns(new PaymentResult
        {
            Success = true
        });

    mockShippingService.Setup(shippingService => shippingService.Ship(It.IsAny<Order>()))
        .Throws(It.IsAny<Exception>());

    var order = new Order { PaymentMethod = "SuperCard" };
    var response = ordersController.SubmitOrder(order);

    response.Success.Should().BeFalse();
}
```

The previous test makes use of [Fluent Assertions](https://fluentassertions.com) to provide a more readable assertion. 


### AutoFixture

When we create an object using **Moq** we get a mock that only returns specific values for things we have Setup. If we take a look at the instance the mock gives us we will see that Members of the given class return a default value for the given type. 
```csharp
[Fact]
public void Should_Create_Moq_Data_Example()
{
    var mock = new Mock<Order>();

    var orderInstance = mock.Object as Order;
}
```
If we inspect the above `orderInstance` we will see that the property values aren't very interesting. If we used this object as a parameter to any sort of real behavior we would likely be met with a slew of exceptions. Depending on what we're testing we may not care to deal with those exceptions for the test at hand.
```csharp
CustomerName = null
OrderItems = null
TotalPrice = 0
PaymentMethod = null
```
We can use another .Net package called **AutoFixture** to help us with some simple data generation. In this example we will use **AutoFixture**'s `Fixture` to create an instance of the `Order` class. 
```csharp
[Fact]
public void Should_Create_AutoFixture_Data_Example()
{
    var fixture = new Fixture();

    var order = fixture.Create<Order>();
}
```

Below we can see that AutoFixture was able to provide values other than default for each property.
```csharp
CustomerName =  "CustomerName8c5b0c54-b381-4633-83f0-43685af001c6"
OrderItems = { "32fab4ad-3c4d-4eca-a4ff-46a280c8a01a",
                "c7adf91e-a134-4177-9cff-f6bed751a3f3",
                "ab0d4fd0-1bba-489d-b447-7fc74a81e07c" }
PaymentMethod = "PaymentMethodfc4b163e-38b8-4942-9d52-1090ad0341ee"
TotalPrice = 194
```

We can also ask AutoFixture to create an instance of the `PaymentService`. We have to take care that we are asking AutoFixture for what we really want. The following example shows that AutoFixture will throw an exception if we ask it to create an `IPaymentService` because I has no idea how to instantiate that interface. **Moq** gladly created a mock because it doesn't need the actual concrete type to be successful.
```csharp
[Fact]
public void Should_Fail_Creating_IPaymentService()
{
    var fixture = new Fixture();
    Action createPaymentService = () => fixture.Create<IPaymentService>();

    createPaymentService.Should().Throw<Exception>();
}
```

One way to get a `PaymentService` from AutoFixture it just to ask for it explicitly.
```csharp
[Fact]
public void Should__Not_Fail_Creating_IPaymentService_ConcreteType()
{
    var fixture = new Fixture();

    Action createPaymentService = () => fixture.Create<PaymentService>();

    createPaymentService.Should().NotThrow<Exception>();
}
```

If we are in a situation where we really want to be sticking with the interface we can help out AutoFixture by giving it a `TypeRelay` that maps the interface to the implementation.
```csharp
[Fact]
public void Should__Not_Fail_Creating_IPaymentService_TypeRelay()
{
    var fixture = new Fixture();

    fixture.Customizations.Add(new TypeRelay(
            typeof(IPaymentService),
            typeof(PaymentService)));

    Action createPaymentService = () => fixture.Create<IPaymentService>();

    createPaymentService.Should().NotThrow<Exception>();
}
```

Let's use AutoFixture to test that our `PaymentService` throws the proper exception when given an invalid payment type.
```csharp
[Fact]
public void Should_Throw_Invalid_Payment()
{
    var fixture = new Fixture();

    fixture.Customizations.Add(new TypeRelay(
        typeof(IPaymentService),
        typeof(PaymentService)));

    var paymentService = fixture.Create<IPaymentService>();

    var order = fixture.Build<Order>()
                        .With(o => o.PaymentMethod, string.Empty)
                        .Create();

    Action act = () => paymentService.Pay(order);

    act.Should().Throw<Exception>().WithMessage("Must provide valid payment method.");

}
```

We can combine the goodness of **Moq** and **AutoFixture** by using the `AutoMoqCustomization`.  AutoFixture maintains its own dependency container and by adding this customization we can write tests even easier. In this example we ask the `Fixture` to give us an `OrdersController` and it happily does so. You'll notice that we haven't directly declared any of the controllers dependencies. This is because AutoFixture has leveraged the customization to ask Moq to create a mock instance for anything that it cannot find in its own container. 
```csharp
[Fact]
public void Should_Successfully_Mock_Dependencies()
{
    var fixture = new Fixture();

    fixture.Customize(new AutoMoqCustomization());

    var ordersController = fixture.Create<OrdersController>();

    var order = fixture.Create<Order>();
    var response = ordersController.SubmitOrder(order);

    response.Success.Should().BeFalse();
}
```

More interesting tests will still require more code, but hopefully with the `AutoMoqCustomization` we will only have to write the code that is necessary for the test we are conducting at the time. We can create and setup verify specific instances of objects or mock objects and `inject()` them into the `Fixture` so that AutoFixture will use that same instance anytime that it is needed as a dependency. 
```csharp
[Fact]
public void Should_Ship_Order_When_Payment_Successful()
{
    var fixture = new Fixture();
    fixture.Customize(new AutoMoqCustomization());

    var psMock = fixture.Create<Mock<IPaymentService>>();
    fixture.Inject(psMock);

    var ssMock = fixture.Create<Mock<IShippingService>>();
    fixture.Inject(ssMock);

    var ordersController = fixture.Create<OrdersController>();

    psMock.Setup(paymentService => paymentService.Pay(It.IsAny<Order>()))
        .Returns(new PaymentResult
        {
            Success = true
        });

    var order = fixture.Create<Order>();
    ordersController.SubmitOrder(order);

    ssMock.Verify(shippingService => shippingService.Ship(It.IsAny<Order>()), Times.Once);
}
```

Instead of `Create()` and `Inject()` we can also call `Freeze()`. This seems to have fallen out of favor, but it is good to be aware of it and you can decide if using it is right for your test suite. 
```csharp
[Fact]
public void Should_Ship_Order_When_Payment_Successful_UsingFreeze()
{
    var fixture = new Fixture();
    fixture.Customize(new AutoMoqCustomization());

    var psMock = fixture.Freeze<Mock<IPaymentService>>();
    var ssMock = fixture.Freeze<Mock<IShippingService>>();

    var ordersController = fixture.Create<OrdersController>();

    psMock.Setup(paymentService => paymentService.Pay(It.IsAny<Order>()))
        .Returns(new PaymentResult
        {
            Success = true
        });

    var order = fixture.Create<Order>();
    ordersController.SubmitOrder(order);

    ssMock.Verify(shippingService => shippingService.Ship(It.IsAny<Order>()), Times.Once);
}
```

Let's generate a few orders.
```csharp
[Fact]
public void Should_Build_Many_Orders()
{
    var fixture = new Fixture();
    fixture.Customize(new AutoMoqCustomization());

    var orders = fixture.CreateMany<Order>();

    orders.GroupBy(o => o.OrderId).Any(group => group.Count() > 1).Should().BeTrue();
}
```

In the previous data generation example you might have noticed that our string values were `<ProperyName><Guid>`. There will be times that something like a `string` might actually contain a value like a guid id. So what if we need to tell AutoFixture how to generate a value? Instead of `Create()` we can do a `Build().With().Create()` that will give us a chance to customize values.
```csharp
[Fact]
public void Should_Build_Many_Orders_With_Guid_Only()
{
    var fixture = new Fixture();
    fixture.Customize(new AutoMoqCustomization());

    var orders = fixture.Build<Order>()
                        .With(o => o.OrderId, Guid.NewGuid().ToString()) 
                        .CreateMany();

    orders.GroupBy(o => o.OrderId).Any(group => group.Count() > 1).Should().BeTrue();
}
```

An issue in the previous assertion is that each one of our Orders that was generated had the same OrderId which is bound to cause us some issue in our tests. AutoFixture allows customization of the data generation as well. For our given scenario we can use an `ISpecimenBuilder` to hook into AutoFixture's chain of responsibility 
```csharp
public class OrderBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        var expectedProperty = typeof(Order).GetProperty(nameof(Order.OrderId));

        if (expectedProperty.Equals(request))
        {
            return Guid.NewGuid().ToString();
        }

        return new NoSpecimen();
    }

}
```

Now that we have a custom `OrderBuilder` we can add it to our fixture and get the results we want.
```csharp
[Fact]
public void Should_Build_Many_Orders_With_Customization()
{
    var fixture = new Fixture();
    fixture.Customize( new AutoMoqCustomization());

    fixture.Customizations.Add(new OrderBuilder());

    var orders = fixture.CreateMany<Order>();

    orders.GroupBy(o => o.OrderId).Any(group => group.Count() > 1).Should().BeFalse();
}
```

### MassTransit

[MassTransit Test harness](https://masstransit-project.com/usage/testing.html)


### Some further reading

[Mocks Aren't Stubs - Martin Fowler](https://martinfowler.com/articles/mocksArentStubs.html)

### Thoughts on testing methodologies 

*(discussion)*
