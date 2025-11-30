using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;
using PayOS.Models.V2.PaymentRequests;
using System.Threading.Tasks;

namespace EliosPaymentService.Repositories.Implementations.Repository;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(CVBuilderDataContext context) : base(context)
    {
    }

    public Task<Order?> GetByOrderCodeAsync(long orderCode) =>
        Set.AsNoTracking().FirstOrDefaultAsync(o => o.OrderCode == orderCode && (o.Status == PaymentLinkStatus.Pending || o.Status == PaymentLinkStatus.Processing) && o.Status != PaymentLinkStatus.Paid);

    public Task<Order?> GetByPaymentLinkIdAsync(string paymentLinkId) =>
        Set.AsNoTracking().FirstOrDefaultAsync(o => o.PaymentLinkId == paymentLinkId);

    public Task<Order?> GetByIdWithItemsAsync(int id) =>
        Set.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

    public async Task<(List<Order> Orders, int TotalCount)> GetByUserIdAsync(
        Guid userId,
        int page,
        int limit,
        PaymentLinkStatus? status = null,
        string sortBy = "createdAt",
        bool descending = true)
    {
        // Build base query filtered by userId
        var query = Set.AsNoTracking()
            .Where(o => o.UserId == userId);

        // Apply status filter if provided
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        // Apply sorting with whitelisted fields (security consideration)
        query = sortBy.ToLower() switch
        {
            "orderdate" => descending
                ? query.OrderByDescending(o => o.OrderDate)
                : query.OrderBy(o => o.OrderDate),
            "amount" => descending
                ? query.OrderByDescending(o => o.Amount)
                : query.OrderBy(o => o.Amount),
            _ => descending // Default to createdAt
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt)
        };

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var orders = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (orders, totalCount);
    }

    public async Task<OrderStatistics> GetStatisticsByUserIdAsync(Guid userId)
    {
        // Get all orders for the user (AsNoTracking for performance)
        var orders = await Set.AsNoTracking()
            .Where(o => o.UserId == userId)
            .ToListAsync();

        // Calculate total spent (only from paid orders)
        var totalSpent = orders
            .Where(o => o.Status == PaymentLinkStatus.Paid)
            .Sum(o => o.AmountPaid);

        // Group orders by status and count them
        var statusBreakdown = orders
            .GroupBy(o => o.Status)
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.Count()
            );

        return new OrderStatistics
        {
            TotalSpent = totalSpent,
            OrderCountByStatus = statusBreakdown
        };
    }
}

