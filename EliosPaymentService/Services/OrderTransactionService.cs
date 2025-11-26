using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Interfaces;

namespace EliosPaymentService.Services;

public class OrderTransactionService(IOrderTransactionRepository transactionRepository)
{
    private readonly IOrderTransactionRepository _transactions = transactionRepository;

    public Task<IEnumerable<OrderTransaction>> GetByOrderIdAsync(int orderId) =>
        _transactions.GetByOrderIdAsync(orderId);

    public Task<IEnumerable<OrderTransaction>> GetByOrderCodeAsync(long orderCode) =>
        _transactions.GetByOrderCodeAsync(orderCode);

    public Task<IEnumerable<OrderTransaction>> GetByPaymentLinkIdAsync(string paymentLinkId) =>
        _transactions.GetByPaymentLinkIdAsync(paymentLinkId);

    public async Task CreateTransactionAsync(OrderTransaction transaction)
    {
        transaction.Id = 0;
        await _transactions.AddAsync(transaction);
    }

    public async Task CreateTransactionsAsync(IEnumerable<OrderTransaction> transactions)
    {
        var list = transactions.ToList();
        foreach (var t in list)
        {
            t.Id = 0;
        }

        await _transactions.AddRangeAsync(list);
    }

    public async Task<bool> DeleteTransactionsByOrderIdAsync(int orderId)
    {
        await _transactions.DeleteByOrderIdAsync(orderId);
        return true;
    }
}