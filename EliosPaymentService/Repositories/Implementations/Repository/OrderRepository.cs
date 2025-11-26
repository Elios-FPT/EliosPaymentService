using System.Threading.Tasks;
using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliosPaymentService.Repositories.Implementations.Repository;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(CVBuilderDataContext context) : base(context)
    {
    }

    public Task<Order?> GetByOrderCodeAsync(long orderCode) =>
        Set.AsNoTracking().FirstOrDefaultAsync(o => o.OrderCode == orderCode);

    public Task<Order?> GetByPaymentLinkIdAsync(string paymentLinkId) =>
        Set.AsNoTracking().FirstOrDefaultAsync(o => o.PaymentLinkId == paymentLinkId);

    public Task<Order?> GetByIdWithItemsAsync(int id) =>
        Set.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
}

