using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;

namespace EliosPaymentService.Repositories.Implementations.Repository;

public class OrderInvoiceRepository : Repository<OrderInvoice>, IOrderInvoiceRepository
{
    public OrderInvoiceRepository(CVBuilderDataContext context) : base(context)
    {
    }

    public Task<OrderInvoice?> GetByInvoiceIdAsync(string invoiceId) =>
        Set.AsNoTracking().FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

    public async Task<IEnumerable<OrderInvoice>> GetByOrderIdAsync(int orderId) =>
        await Set.AsNoTracking()
            .Where(i => i.OrderId == orderId)
            .OrderByDescending(i => i.IssuedDatetime)
            .ToListAsync();

    public async Task<IEnumerable<OrderInvoice>> GetByOrderCodeAsync(long orderCode) =>
        await Set.AsNoTracking()
            .Where(i => i.OrderCode == orderCode)
            .OrderByDescending(i => i.IssuedDatetime)
            .ToListAsync();

    public async Task<IEnumerable<OrderInvoice>> GetByPaymentLinkIdAsync(string paymentLinkId) =>
        await Set.AsNoTracking()
            .Where(i => i.PaymentLinkId == paymentLinkId)
            .OrderByDescending(i => i.IssuedDatetime)
            .ToListAsync();

    public async Task DeleteByOrderIdAsync(int orderId)
    {
        var invoices = await Set.Where(i => i.OrderId == orderId).ToListAsync();
        if (invoices.Count == 0)
        {
            return;
        }

        Set.RemoveRange(invoices);
        await Context.SaveChangesAsync();
    }
}

