using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Interfaces;

namespace EliosPaymentService.Services;

public class OrderInvoiceService(IOrderInvoiceRepository invoiceRepository)
{
    private readonly IOrderInvoiceRepository _invoices = invoiceRepository;

    public Task<OrderInvoice?> GetByInvoiceIdAsync(string invoiceId) =>
        _invoices.GetByInvoiceIdAsync(invoiceId);

    public Task<IEnumerable<OrderInvoice>> GetByOrderIdAsync(int orderId) =>
        _invoices.GetByOrderIdAsync(orderId);

    public Task<IEnumerable<OrderInvoice>> GetByOrderCodeAsync(long orderCode) =>
        _invoices.GetByOrderCodeAsync(orderCode);

    public Task<IEnumerable<OrderInvoice>> GetByPaymentLinkIdAsync(string paymentLinkId) =>
        _invoices.GetByPaymentLinkIdAsync(paymentLinkId);

    public async Task CreateInvoicesAsync(IEnumerable<OrderInvoice> invoices)
    {
        var list = invoices.ToList();
        foreach (var invoice in list)
        {
            invoice.Id = 0;
        }

        await _invoices.AddRangeAsync(list);
    }

    public async Task<bool> DeleteInvoicesByOrderIdAsync(int orderId)
    {
        await _invoices.DeleteByOrderIdAsync(orderId);
        return true;
    }
}