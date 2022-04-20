
using Moq;
using Orders;
using Xunit;

namespace Examples;

public class MoqExamples
{

    // The requirement is something like on a successful payment ship the order

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

        var order = new Order();
        ordersController.SubmitOrder(order);

        mockShippingService.Verify(shippingService => shippingService.Ship(It.IsAny<Order>()), Times.Once);
    }



}

