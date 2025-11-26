using System.Threading.Tasks;
using EliosPaymentService.Models;

namespace EliosPaymentService.Repositories.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderCodeAsync(long orderCode);
    Task<Order?> GetByPaymentLinkIdAsync(string paymentLinkId);
    Task<Order?> GetByIdWithItemsAsync(int id);
}

