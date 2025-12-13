using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using EliosPaymentService.Controllers;
using EliosPaymentService.Models;
using EliosPaymentService.Services;
using EliosPaymentService.Tests.Common;
using Xunit;

namespace EliosPaymentService.Tests.Controllers;

public class OrderControllerTests
{
    private readonly Mock<IOrderService> _mockOrderService;
    private readonly Mock<OrderTransactionService> _mockOrderTransactionService;
    private readonly Mock<OrderInvoiceService> _mockOrderInvoiceService;
    private readonly OrderController _controller;

    public OrderControllerTests()
    {
        // PayOSClient cannot be mocked, so we create a real instance with test config
        var payOSOptions = new PayOS.PayOSOptions
        {
            ClientId = "test-client-id",
            ApiKey = "test-api-key",
            ChecksumKey = "test-checksum-key"
        };
        var payOSClient = new PayOSClient(payOSOptions);

        _mockOrderService = new Mock<IOrderService>();
        _mockOrderTransactionService = new Mock<OrderTransactionService>(
            Mock.Of<EliosPaymentService.Repositories.Interfaces.IRepository.IOrderTransactionRepository>());
        _mockOrderInvoiceService = new Mock<OrderInvoiceService>(
            Mock.Of<EliosPaymentService.Repositories.Interfaces.IRepository.IOrderInvoiceRepository>());

        _controller = new OrderController(
            payOSClient,
            _mockOrderService.Object,
            _mockOrderTransactionService.Object,
            _mockOrderInvoiceService.Object);
    }

    [Fact]
    public async Task GetByUserId_WhenValidRequest_ShouldReturnPagedOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = 1;
        var limit = 10;
        var orders = new List<Order> { TestDataBuilder.CreateOrder(userId: userId) };
        var totalCount = 1;
        var statistics = TestDataBuilder.CreateOrderStatistics();

        _mockOrderService
            .Setup(s => s.GetByUserIdAsync(userId, page, limit, null, "createdAt", true))
            .ReturnsAsync((orders, totalCount));

        _mockOrderService
            .Setup(s => s.GetStatisticsByUserIdAsync(userId))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetByUserId(userId, page, limit);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedOrderResult>().Subject;
        pagedResult.Data.Should().HaveCount(1);
        pagedResult.Pagination.Page.Should().Be(page);
        pagedResult.Pagination.Limit.Should().Be(limit);
        pagedResult.Pagination.TotalCount.Should().Be(totalCount);
        pagedResult.Statistics.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByUserId_WhenInvalidPage_ShouldReturnBadRequest(int invalidPage)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _controller.GetByUserId(userId, invalidPage);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Page must be at least 1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public async Task GetByUserId_WhenInvalidLimit_ShouldReturnBadRequest(int invalidLimit)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _controller.GetByUserId(userId, 1, invalidLimit);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Limit must be between 1 and 100");
    }

    [Fact]
    public async Task GetByUserId_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockOrderService
            .Setup(s => s.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<PaymentLinkStatus?>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetByUserId(userId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetByUserId_WhenNoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = 1;
        var limit = 10;
        var emptyOrders = new List<Order>();
        var totalCount = 0;
        var statistics = TestDataBuilder.CreateOrderStatistics(totalSpent: 0, orderCountByStatus: new Dictionary<string, int>());

        _mockOrderService
            .Setup(s => s.GetByUserIdAsync(userId, page, limit, null, "createdAt", true))
            .ReturnsAsync((emptyOrders, totalCount));

        _mockOrderService
            .Setup(s => s.GetStatisticsByUserIdAsync(userId))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetByUserId(userId, page, limit);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedOrderResult>().Subject;
        pagedResult.Data.Should().BeEmpty();
        pagedResult.Pagination.TotalCount.Should().Be(0);
        pagedResult.Pagination.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task GetByUserId_WhenFilterByStatus_ShouldReturnFilteredOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = 1;
        var limit = 10;
        var status = PaymentLinkStatus.Paid;
        var orders = new List<Order> { TestDataBuilder.CreateOrder(userId: userId, status: status) };
        var totalCount = 1;
        var statistics = TestDataBuilder.CreateOrderStatistics();

        _mockOrderService
            .Setup(s => s.GetByUserIdAsync(userId, page, limit, status, "createdAt", true))
            .ReturnsAsync((orders, totalCount));

        _mockOrderService
            .Setup(s => s.GetStatisticsByUserIdAsync(userId))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetByUserId(userId, page, limit, status);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedOrderResult>().Subject;
        pagedResult.Data.Should().HaveCount(1);
        pagedResult.Data.First().Status.Should().Be(status);
    }

    [Fact]
    public async Task GetByUserId_WhenSortByAmount_ShouldReturnSortedOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = 1;
        var limit = 10;
        var sortBy = "amount";
        var orders = new List<Order> { TestDataBuilder.CreateOrder(userId: userId) };
        var totalCount = 1;
        var statistics = TestDataBuilder.CreateOrderStatistics();

        _mockOrderService
            .Setup(s => s.GetByUserIdAsync(userId, page, limit, null, sortBy, true))
            .ReturnsAsync((orders, totalCount));

        _mockOrderService
            .Setup(s => s.GetStatisticsByUserIdAsync(userId))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetByUserId(userId, page, limit, null, sortBy);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedOrderResult>().Subject;
        pagedResult.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByUserId_WhenDescendingFalse_ShouldReturnAscendingOrders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = 1;
        var limit = 10;
        var descending = false;
        var orders = new List<Order> { TestDataBuilder.CreateOrder(userId: userId) };
        var totalCount = 1;
        var statistics = TestDataBuilder.CreateOrderStatistics();

        _mockOrderService
            .Setup(s => s.GetByUserIdAsync(userId, page, limit, null, "createdAt", descending))
            .ReturnsAsync((orders, totalCount));

        _mockOrderService
            .Setup(s => s.GetStatisticsByUserIdAsync(userId))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetByUserId(userId, page, limit, null, "createdAt", descending);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedOrderResult>().Subject;
        pagedResult.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByUserId_WhenMultiplePages_ShouldReturnCorrectPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = 2;
        var limit = 10;
        var totalCount = 25;
        var orders = new List<Order> { TestDataBuilder.CreateOrder(userId: userId) };
        var statistics = TestDataBuilder.CreateOrderStatistics();

        _mockOrderService
            .Setup(s => s.GetByUserIdAsync(userId, page, limit, null, "createdAt", true))
            .ReturnsAsync((orders, totalCount));

        _mockOrderService
            .Setup(s => s.GetStatisticsByUserIdAsync(userId))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetByUserId(userId, page, limit);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedOrderResult>().Subject;
        pagedResult.Pagination.Page.Should().Be(page);
        pagedResult.Pagination.TotalCount.Should().Be(totalCount);
        pagedResult.Pagination.TotalPages.Should().Be(3); // 25 / 10 = 3 pages
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task GetByUserId_WhenValidLimitBoundary_ShouldReturnSuccess(int validLimit)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var page = 1;
        var orders = new List<Order> { TestDataBuilder.CreateOrder(userId: userId) };
        var totalCount = 1;
        var statistics = TestDataBuilder.CreateOrderStatistics();

        _mockOrderService
            .Setup(s => s.GetByUserIdAsync(userId, page, validLimit, null, "createdAt", true))
            .ReturnsAsync((orders, totalCount));

        _mockOrderService
            .Setup(s => s.GetStatisticsByUserIdAsync(userId))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetByUserId(userId, page, validLimit);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedOrderResult>().Subject;
        pagedResult.Pagination.Limit.Should().Be(validLimit);
    }

    [Fact]
    public async Task GetByUserId_WhenUserIdIsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.Empty;
        var page = 1;
        var limit = 10;
        var emptyOrders = new List<Order>();
        var totalCount = 0;
        var statistics = TestDataBuilder.CreateOrderStatistics(totalSpent: 0, orderCountByStatus: new Dictionary<string, int>());

        _mockOrderService
            .Setup(s => s.GetByUserIdAsync(userId, page, limit, null, "createdAt", true))
            .ReturnsAsync((emptyOrders, totalCount));

        _mockOrderService
            .Setup(s => s.GetStatisticsByUserIdAsync(userId))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetByUserId(userId, page, limit);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedOrderResult>().Subject;
        pagedResult.Data.Should().BeEmpty();
        pagedResult.Pagination.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByUserId_WhenUserIdNotFound_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid(); // Non-existent user ID
        var page = 1;
        var limit = 10;
        var emptyOrders = new List<Order>();
        var totalCount = 0;
        var statistics = TestDataBuilder.CreateOrderStatistics(totalSpent: 0, orderCountByStatus: new Dictionary<string, int>());

        _mockOrderService
            .Setup(s => s.GetByUserIdAsync(userId, page, limit, null, "createdAt", true))
            .ReturnsAsync((emptyOrders, totalCount));

        _mockOrderService
            .Setup(s => s.GetStatisticsByUserIdAsync(userId))
            .ReturnsAsync(statistics);

        // Act
        var result = await _controller.GetByUserId(userId, page, limit);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pagedResult = okResult.Value.Should().BeOfType<PagedOrderResult>().Subject;
        pagedResult.Data.Should().BeEmpty();
        pagedResult.Pagination.TotalCount.Should().Be(0);
        pagedResult.Statistics.TotalSpent.Should().Be(0);
    }

    [Fact]
    public async Task Get_WhenOrderNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var orderId = 1;
        _mockOrderService
            .Setup(s => s.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _controller.Get(orderId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Get_WhenOrderHasNoPaymentLinkId_ShouldReturnNotFound()
    {
        // Arrange
        var orderId = 1;
        var order = TestDataBuilder.CreateOrder(id: orderId, paymentLinkId: null);
        _mockOrderService
            .Setup(s => s.GetByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _controller.Get(orderId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreatePayment_WhenRequestIsNull_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.CreatePayment(null!);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Order data is required");
    }

    [Fact]
    public async Task CreatePayment_WhenModelStateInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new OrderCreateRequest
        {
            TotalAmount = 0, // Invalid
            Items = new List<OrderItemCreateRequest>() // Empty items
        };
        _controller.ModelState.AddModelError("TotalAmount", "TotalAmount is required");

        // Act
        var result = await _controller.CreatePayment(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
    }

    [Fact]
    public async Task CreatePayment_WhenUserIdHeaderMissing_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = TestDataBuilder.CreateOrderCreateRequest();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.CreatePayment(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePayment_WhenUserIdHeaderInvalid_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = TestDataBuilder.CreateOrderCreateRequest();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Request =
                {
                    Headers = { ["X-Auth-Request-User"] = "invalid-guid" }
                }
            }
        };

        // Act
        var result = await _controller.CreatePayment(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
    }

}

