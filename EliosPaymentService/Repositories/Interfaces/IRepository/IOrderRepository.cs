using System.Threading.Tasks;
using EliosPaymentService.Models;
using PayOS.Models.V2.PaymentRequests;

namespace EliosPaymentService.Repositories.Interfaces.IRepository;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderCodeAsync(long orderCode);
    Task<Order?> GetByPaymentLinkIdAsync(string paymentLinkId);
    Task<Order?> GetByIdWithItemsAsync(int id);

    /// <summary>
    /// Get paginated orders by user ID with optional filtering and sorting
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="limit">Number of items per page</param>
    /// <param name="status">Optional filter by payment status</param>
    /// <param name="sortBy">Sort field (createdAt, orderDate, or amount)</param>
    /// <param name="descending">Sort direction (true for descending)</param>
    /// <returns>Tuple containing list of orders and total count</returns>
    Task<(List<Order> Orders, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int limit,
        PaymentLinkStatus? status = null,
        string sortBy = "createdAt",
        bool descending = true);

    /// <summary>
    /// Get statistical summary of all orders for a specific user
    /// </summary>
    /// <param name="userId">The user's unique identifier</param>
    /// <returns>Order statistics including total spent and status breakdown</returns>
    Task<OrderStatistics> GetStatisticsByUserIdAsync(Guid userId);
}

