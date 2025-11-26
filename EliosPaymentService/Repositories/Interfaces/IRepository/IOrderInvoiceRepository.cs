using System.Collections.Generic;
using System.Threading.Tasks;
using EliosPaymentService.Models;

namespace EliosPaymentService.Repositories.Interfaces.IRepository;

public interface IOrderInvoiceRepository : IRepository<OrderInvoice>
{
    Task<OrderInvoice?> GetByInvoiceIdAsync(string invoiceId);
    Task<IEnumerable<OrderInvoice>> GetByOrderIdAsync(int orderId);
    Task<IEnumerable<OrderInvoice>> GetByOrderCodeAsync(long orderCode);
    Task<IEnumerable<OrderInvoice>> GetByPaymentLinkIdAsync(string paymentLinkId);
    Task DeleteByOrderIdAsync(int orderId);
}

