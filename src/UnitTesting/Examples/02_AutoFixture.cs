
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using FluentAssertions;
using Moq;
using Orders;
using System;
using System.Linq;
using Xunit;

namespace Examples;

public class AutoFixture
{
    [Fact]
    public void Should_Create_Moq_Data_Example()
    {
        var mock = new Mock<Order>();

        var orderInstance = mock.Object as Order;
    }

    [Fact]
    public void Should_Create_AutoFixture_Data_Example()
    {
        var fixture = new Fixture();

        var order = fixture.Create<Order>();
    }

    [Fact]
    public void Should_Fail_Creating_IPaymentService()
    {
        var fixture = new Fixture();
        Action createPaymentService = () => fixture.Create<IPaymentService>();

        createPaymentService.Should().Throw<Exception>();
    }

    [Fact]
    public void Should__Not_Fail_Creating_IPaymentService_ConcreteType()
    {
        var fixture = new Fixture();

        Action createPaymentService = () => fixture.Create<PaymentService>();

        createPaymentService.Should().NotThrow<Exception>();
    }

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

    [Fact]
    public void Should_Build_Many_Orders()
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        var orders = fixture.CreateMany<Order>();

        (orders.Count() > 1).Should().BeTrue();
    }


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


    [Fact]
    public void Should_Build_Many_Orders_With_Customization()
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization());

        fixture.Customizations.Add(new OrderBuilder());

        var orders = fixture.CreateMany<Order>();

        orders.GroupBy(o => o.OrderId).Any(group => group.Count() > 1).Should().BeFalse();
    }


}

