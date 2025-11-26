using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliosPaymentService.Repositories.Implementations;

public class OrderTransactionRepository : Repository<OrderTransaction>, IOrderTransactionRepository
{
    public OrderTransactionRepository(CVBuilderDataContext context) : base(context)
    {
    }

    public async Task<IEnumerable<OrderTransaction>> GetByOrderIdAsync(int orderId) =>
        await Set.AsNoTracking()
            .Where(t => t.OrderId == orderId)
            .OrderByDescending(t => t.TransactionDateTime)
            .ToListAsync();

    public async Task<IEnumerable<OrderTransaction>> GetByOrderCodeAsync(long orderCode) =>
        await Set.AsNoTracking()
            .Where(t => t.OrderCode == orderCode)
            .OrderByDescending(t => t.TransactionDateTime)
            .ToListAsync();

    public async Task<IEnumerable<OrderTransaction>> GetByPaymentLinkIdAsync(string paymentLinkId) =>
        await Set.AsNoTracking()
            .Where(t => t.PaymentLinkId == paymentLinkId)
            .OrderByDescending(t => t.TransactionDateTime)
            .ToListAsync();

    public async Task DeleteByOrderIdAsync(int orderId)
    {
        var transactions = await Set.Where(t => t.OrderId == orderId).ToListAsync();
        if (transactions.Count == 0)
        {
            return;
        }

        Set.RemoveRange(transactions);
        await Context.SaveChangesAsync();
    }
}

