using EliosPaymentService.Models;
using PayOS.Models.V2.PaymentRequests;

namespace EliosPaymentService.Tests.Common;

public static class TestDataBuilder
{
    public static Order CreateOrder(
        int id = 1,
        Guid? userId = null,
        long orderCode = 1234567890,
        long totalAmount = 100000,
        string? paymentLinkId = "test-payment-link-id",
        PaymentLinkStatus status = PaymentLinkStatus.Pending)
    {
        return new Order
        {
            Id = id,
            UserId = userId ?? Guid.NewGuid(),
            OrderCode = orderCode,
            TotalAmount = totalAmount,
            PaymentLinkId = paymentLinkId,
            Status = status,
            Amount = totalAmount,
            AmountPaid = 0,
            AmountRemaining = totalAmount,
            Description = "Test order",
            CreatedAt = DateTimeOffset.UtcNow,
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    Name = "Test Item",
                    Quantity = 1,
                    Price = totalAmount,
                    Unit = "item"
                }
            }
        };
    }

    public static OrderCreateRequest CreateOrderCreateRequest(
        long totalAmount = 100000,
        string? description = "Test order request")
    {
        return new OrderCreateRequest
        {
            TotalAmount = totalAmount,
            Description = description,
            ReturnUrl = "https://test.com/return",
            CancelUrl = "https://test.com/cancel",
            BuyerName = "Test Buyer",
            BuyerEmail = "test@example.com",
            Items = new List<OrderItemCreateRequest>
            {
                new OrderItemCreateRequest
                {
                    Name = "Test Item",
                    Quantity = 1,
                    Price = totalAmount
                }
            }
        };
    }

    public static OrderTransaction CreateOrderTransaction(
        int id = 1,
        int orderId = 1,
        long orderCode = 1234567890,
        string reference = "test-ref-123",
        long amount = 100000)
    {
        return new OrderTransaction
        {
            Id = id,
            OrderId = orderId,
            OrderCode = orderCode,
            PaymentLinkId = "test-payment-link-id",
            Reference = reference,
            Amount = amount,
            AccountNumber = "12345678",
            Description = "Test transaction",
            TransactionDateTime = DateTimeOffset.UtcNow
        };
    }

    public static OrderInvoice CreateOrderInvoice(
        int id = 1,
        int orderId = 1,
        long orderCode = 1234567890,
        string invoiceId = "test-invoice-id")
    {
        return new OrderInvoice
        {
            Id = id,
            OrderId = orderId,
            OrderCode = orderCode,
            PaymentLinkId = "test-payment-link-id",
            InvoiceId = invoiceId,
            InvoiceNumber = "INV-001"
        };
    }

    public static OrderStatistics CreateOrderStatistics(
        long totalSpent = 500000,
        Dictionary<string, int>? orderCountByStatus = null)
    {
        return new OrderStatistics
        {
            TotalSpent = totalSpent,
            OrderCountByStatus = orderCountByStatus ?? new Dictionary<string, int>
            {
                { "Paid", 5 },
                { "Pending", 2 }
            }
        };
    }
}

