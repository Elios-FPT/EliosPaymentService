using EliosPaymentService.Models;
using PayOS.Models.V2.PaymentRequests;

namespace EliosPaymentService.Services;

public interface IOrderService
{
    Task<Order?> GetByIdAsync(int id);
    Task<Order?> GetByOrderCodeAsync(long orderCode);
    Task<Order?> GetByPaymentLinkIdAsync(string paymentLinkId);
    Task<(List<Order> Orders, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int limit,
        PaymentLinkStatus? status = null,
        string sortBy = "createdAt",
        bool descending = true);
    Task<OrderStatistics> GetStatisticsByUserIdAsync(Guid userId);
    Task<Order> CreateAsync(Order order);
    Task<bool> UpdateAsync(Order updatedOrder);
    Task<bool> DeleteAsync(int id);
    Task UpdateUserTokenAsync(int orderId, CancellationToken cancellationToken = default);
}

