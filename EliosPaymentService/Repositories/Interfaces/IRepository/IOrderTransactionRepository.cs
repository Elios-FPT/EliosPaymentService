using System.Collections.Generic;
using System.Threading.Tasks;
using EliosPaymentService.Models;

namespace EliosPaymentService.Repositories.Interfaces.IRepository;

public interface IOrderTransactionRepository : IRepository<OrderTransaction>
{
    Task<IEnumerable<OrderTransaction>> GetByOrderIdAsync(int orderId);
    Task<IEnumerable<OrderTransaction>> GetByOrderCodeAsync(long orderCode);
    Task<IEnumerable<OrderTransaction>> GetByPaymentLinkIdAsync(string paymentLinkId);
    Task DeleteByOrderIdAsync(int orderId);
}

