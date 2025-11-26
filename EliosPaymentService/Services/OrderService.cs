using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Interfaces;

namespace EliosPaymentService.Services;

public class OrderService(IOrderRepository orderRepository)
{
    private readonly IOrderRepository _orders = orderRepository;

    public Task<Order?> GetByIdAsync(int id) =>
        _orders.GetByIdAsync(id);

    public Task<Order?> GetByOrderCodeAsync(long orderCode) =>
        _orders.GetByOrderCodeAsync(orderCode);

    public Task<Order?> GetByPaymentLinkIdAsync(string paymentLinkId) =>
        _orders.GetByPaymentLinkIdAsync(paymentLinkId);

    public async Task<Order> CreateAsync(Order order)
    {
        // Db will generate identity; ensure Id is default
        order.Id = 0;
        return await _orders.AddAsync(order);
    }

    public async Task<bool> UpdateAsync(Order updatedOrder)
    {
        var existing = await _orders.GetByIdAsync(updatedOrder.Id);
        if (existing is null)
        {
            return false;
        }

        // Basic order information
        existing.OrderCode = updatedOrder.OrderCode;
        existing.TotalAmount = updatedOrder.TotalAmount;
        existing.OrderDate = updatedOrder.OrderDate;
        existing.Description = updatedOrder.Description;
        existing.Items = updatedOrder.Items;

        // Payment link related properties
        existing.PaymentLinkId = updatedOrder.PaymentLinkId;
        existing.QrCode = updatedOrder.QrCode;
        existing.CheckoutUrl = updatedOrder.CheckoutUrl;
        existing.Status = updatedOrder.Status;

        // Amount tracking
        existing.Amount = updatedOrder.Amount;
        existing.AmountPaid = updatedOrder.AmountPaid;
        existing.AmountRemaining = updatedOrder.AmountRemaining;

        // Buyer information
        existing.BuyerName = updatedOrder.BuyerName;
        existing.BuyerCompanyName = updatedOrder.BuyerCompanyName;
        existing.BuyerEmail = updatedOrder.BuyerEmail;
        existing.BuyerPhone = updatedOrder.BuyerPhone;
        existing.BuyerAddress = updatedOrder.BuyerAddress;

        // Payment link details
        existing.Bin = updatedOrder.Bin;
        existing.AccountNumber = updatedOrder.AccountNumber;
        existing.AccountName = updatedOrder.AccountName;
        existing.Currency = updatedOrder.Currency;

        // URLs
        existing.ReturnUrl = updatedOrder.ReturnUrl;
        existing.CancelUrl = updatedOrder.CancelUrl;

        // Timestamps
        existing.CreatedAt = updatedOrder.CreatedAt;
        existing.CanceledAt = updatedOrder.CanceledAt;
        existing.ExpiredAt = updatedOrder.ExpiredAt;
        existing.LastTransactionUpdate = updatedOrder.LastTransactionUpdate;

        // Cancellation
        existing.CancellationReason = updatedOrder.CancellationReason;

        // Invoice settings
        existing.BuyerNotGetInvoice = updatedOrder.BuyerNotGetInvoice;
        existing.TaxPercentage = updatedOrder.TaxPercentage;

        await _orders.UpdateAsync(existing);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _orders.GetByIdAsync(id);
        if (existing is null)
        {
            return false;
        }

        await _orders.DeleteAsync(existing);
        return true;
    }
}