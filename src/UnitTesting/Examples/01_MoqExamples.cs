
using FluentAssertions;
using Moq;
using Orders;
using System;
using Xunit;

namespace Examples;

public class MoqExamples
{
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

}

