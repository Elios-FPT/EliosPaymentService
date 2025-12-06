using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using EliosPaymentService.Models;
using EliosPaymentService.Services;

using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.V2.PaymentRequests.Invoices;

using PayOS;

namespace EliosPaymentService.Controllers;

[ApiController]
[Route("api/payment/[controller]")]
public class OrderController : ControllerBase
{
    private readonly PayOSClient _client;
    private readonly OrderService _orderService;
    private readonly OrderTransactionService _orderTransactionService;
    private readonly OrderInvoiceService _orderInvoiceService;

    public OrderController(
        [FromKeyedServices("OrderClient")] PayOSClient client,
        OrderService orderService,
        OrderTransactionService orderTransactionService,
        OrderInvoiceService orderInvoiceService)
    {
        _client = client;
        _orderService = orderService;
        _orderTransactionService = orderTransactionService;
        _orderInvoiceService = orderInvoiceService;
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<PagedOrderResult>> GetByUserId(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] PaymentLinkStatus? status = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] bool descending = true)
    {
        // Input validation
        if (page < 1)
        {
            return BadRequest("Page must be at least 1");
        }

        if (limit < 1 || limit > 100)
        {
            return BadRequest("Limit must be between 1 and 100");
        }

        try
        {
            // Get paginated orders
            var (orders, totalCount) = await _orderService.GetByUserIdAsync(
                userId, page, limit, status, sortBy, descending);

            // Get statistics
            var statistics = await _orderService.GetStatisticsByUserIdAsync(userId);

            var result = new PagedOrderResult
            {
                Data = orders,
                Pagination = new PaginationMetadata
                {
                    Page = page,
                    Limit = limit,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)limit)
                },
                Statistics = statistics
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"Failed to retrieve orders for user {userId}", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> Get(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null || string.IsNullOrEmpty(order.PaymentLinkId))
        {
            return NotFound();
        }

        try
        {
            var paymentLink = await _client.PaymentRequests.GetAsync(order.PaymentLinkId);

            order.Status = paymentLink.Status;
            order.Amount = paymentLink.Amount;
            order.AmountPaid = paymentLink.AmountPaid;
            order.AmountRemaining = paymentLink.AmountRemaining;
            order.CreatedAt = DateTimeOffset.TryParse(paymentLink.CreatedAt, out var createdAt) ? createdAt : null;
            order.CanceledAt = DateTimeOffset.TryParse(paymentLink.CanceledAt, out var canceledAt) ? canceledAt : null;
            order.CancellationReason = paymentLink.CancellationReason;

            if (paymentLink.Transactions != null && paymentLink.Transactions.Count > 0)
            {
                await _orderTransactionService.DeleteTransactionsByOrderIdAsync(order.Id);

                var transactions = paymentLink.Transactions.Select(t => new OrderTransaction
                {
                    OrderId = order.Id,
                    OrderCode = order.OrderCode,
                    PaymentLinkId = order.PaymentLinkId,
                    Reference = t.Reference,
                    Amount = t.Amount,
                    AccountNumber = t.AccountNumber,
                    Description = t.Description,
                    TransactionDateTime = DateTimeOffset.TryParse(t.TransactionDateTime, out var transactionDateTime) ? transactionDateTime : DateTimeOffset.UtcNow,
                    VirtualAccountName = t.VirtualAccountName,
                    VirtualAccountNumber = t.VirtualAccountNumber,
                    CounterAccountBankId = t.CounterAccountBankId,
                    CounterAccountBankName = t.CounterAccountBankName,
                    CounterAccountName = t.CounterAccountName,
                    CounterAccountNumber = t.CounterAccountNumber
                }).ToList();

                await _orderTransactionService.CreateTransactionsAsync(transactions);
                order.LastTransactionUpdate = DateTimeOffset.UtcNow;
            }

            await _orderService.UpdateAsync(order);
            return Ok(order);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Failed to retrieve order {id}", error = ex.Message });
        }

    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreatePayment(OrderCreateRequest request)
    {
        if (request == null)
        {
            return BadRequest("Order data is required");
        }
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var orderCode = DateTimeOffset.Now.ToUnixTimeSeconds();
        var returnUrl = request.ReturnUrl ?? "https://your-domain.com/success";
        var cancelUrl = request.CancelUrl ?? "https://your-domain.com/cancel";


        var paymentRequest = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = request.TotalAmount,
            Description = request.Description ?? $"order {orderCode}",
            ReturnUrl = returnUrl,
            CancelUrl = cancelUrl,
            BuyerName = request.BuyerName,
            BuyerCompanyName = request.BuyerCompanyName,
            BuyerEmail = request.BuyerEmail,
            BuyerPhone = request.BuyerPhone,
            BuyerAddress = request.BuyerAddress,
            ExpiredAt = request.ExpiredAt?.ToUnixTimeSeconds(),
            Items = [.. request.Items.Select(i => new PaymentLinkItem
            {
                Name = i.Name ?? "",
                Quantity = i.Quantity,
                Price = i.Price,
                Unit = i.Unit,
                TaxPercentage = i.TaxPercentage
            })]
        };

        if (request.BuyerNotGetInvoice.HasValue || request.TaxPercentage.HasValue)
        {
            paymentRequest.Invoice = new InvoiceRequest
            {
                BuyerNotGetInvoice = request.BuyerNotGetInvoice,
                TaxPercentage = request.TaxPercentage
            };
        }

        // Extract ID from header
        var userIdHeader = Request.Headers["X-Auth-Request-User"].ToString();
        if (!Guid.TryParse(userIdHeader, out var ownerId))
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new { message = "Unauthorized: Invalid user ID" });
        }

        try
        {
            var paymentResponse = await _client.PaymentRequests.CreateAsync(paymentRequest);

            var order = new Order
            {
                Id = 0, // Will be set by service
                UserId = ownerId,
                OrderCode = orderCode,
                TotalAmount = request.TotalAmount,
                Description = paymentResponse.Description,
                PaymentLinkId = paymentResponse.PaymentLinkId,
                QrCode = paymentResponse.QrCode,
                CheckoutUrl = paymentResponse.CheckoutUrl,
                Status = paymentResponse.Status,
                Amount = paymentResponse.Amount,
                AmountPaid = 0,
                AmountRemaining = paymentResponse.Amount,
                Bin = paymentResponse.Bin,
                AccountNumber = paymentResponse.AccountNumber,
                AccountName = paymentResponse.AccountName,
                Currency = paymentResponse.Currency,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                CreatedAt = DateTimeOffset.UtcNow,
                BuyerName = request.BuyerName,
                BuyerCompanyName = request.BuyerCompanyName,
                BuyerEmail = request.BuyerEmail,
                BuyerPhone = request.BuyerPhone,
                BuyerAddress = request.BuyerAddress,
                ExpiredAt = request.ExpiredAt?.ToUniversalTime(),
                BuyerNotGetInvoice = request.BuyerNotGetInvoice,
                TaxPercentage = request.TaxPercentage,
                Items = [.. request.Items.Select(i => new OrderItem
                {
                    Name = i.Name,
                    Quantity = i.Quantity,
                    Price = i.Price,
                    Unit = i.Unit,
                    TaxPercentage = i.TaxPercentage
                })]
            };

            var created = await _orderService.CreateAsync(order);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Failed to create order", error = ex.Message });
        }
    }

    [HttpPost("{orderId}/cancel")]
    public async Task<ActionResult<PaymentLink>> CancelPayment(int orderId, string cancellationReason)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        if (order == null || string.IsNullOrEmpty(order.PaymentLinkId))
        {
            return NotFound();
        }

        try
        {

            var paymentLink = await _client.PaymentRequests.CancelAsync(order.PaymentLinkId, cancellationReason ?? "Cancelled by user");

            order.Status = paymentLink.Status;
            order.Amount = paymentLink.Amount;
            order.AmountPaid = paymentLink.AmountPaid;
            order.AmountRemaining = paymentLink.AmountRemaining;
            order.CanceledAt = DateTimeOffset.TryParse(paymentLink.CanceledAt, out var canceledAt) ? canceledAt : null;
            order.CancellationReason = paymentLink.CancellationReason;

            if (paymentLink.Transactions != null && paymentLink.Transactions.Count > 0)
            {
                await _orderTransactionService.DeleteTransactionsByOrderIdAsync(order.Id);

                var transactions = paymentLink.Transactions.Select(t => new OrderTransaction
                {
                    OrderId = order.Id,
                    OrderCode = order.OrderCode,
                    PaymentLinkId = order.PaymentLinkId,
                    Reference = t.Reference,
                    Amount = t.Amount,
                    AccountNumber = t.AccountNumber,
                    Description = t.Description,
                    TransactionDateTime = DateTimeOffset.TryParse(t.TransactionDateTime, out var transactionDateTime) ? transactionDateTime : DateTimeOffset.UtcNow,
                    VirtualAccountName = t.VirtualAccountName,
                    VirtualAccountNumber = t.VirtualAccountNumber,
                    CounterAccountBankId = t.CounterAccountBankId,
                    CounterAccountBankName = t.CounterAccountBankName,
                    CounterAccountName = t.CounterAccountName,
                    CounterAccountNumber = t.CounterAccountNumber
                }).ToList();

                await _orderTransactionService.CreateTransactionsAsync(transactions);
                order.LastTransactionUpdate = DateTimeOffset.UtcNow;
            }

            await _orderService.UpdateAsync(order);
            return Ok(paymentLink);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Failed to cancel order {orderId}", error = ex.Message });
        }
    }

    [HttpGet("{orderId}/invoices")]
    public async Task<ActionResult<OrderInvoicesInfo>> GetInvoices(int orderId)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        if (order == null || string.IsNullOrEmpty(order.PaymentLinkId))
        {
            return NotFound();
        }
        try
        {
            var invoices = await _client.PaymentRequests.Invoices.GetAsync(order.PaymentLinkId);

            await _orderInvoiceService.DeleteInvoicesByOrderIdAsync(order.Id);

            var orderInvoices = invoices.Invoices.Select(i => new OrderInvoice
            {
                OrderId = order.Id,
                OrderCode = order.OrderCode,
                PaymentLinkId = order.PaymentLinkId,
                InvoiceId = i.InvoiceId,
                InvoiceNumber = i.InvoiceNumber,
                IssuedTimestamp = i.IssuedTimestamp,
                IssuedDatetime = i.IssuedDatetime,
                TransactionId = i.TransactionId,
                ReservationCode = i.ReservationCode,
                CodeOfTax = i.CodeOfTax
            }).ToList();

            await _orderInvoiceService.CreateInvoicesAsync(orderInvoices);

            var orderInvoicesInfo = new OrderInvoicesInfo
            {
                Invoices = orderInvoices
            };

            return Ok(orderInvoicesInfo);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Failed to retrieve invoice for order {orderId}", error = ex.Message });
        }
    }

    [HttpGet("{orderId}/invoices/{invoiceId}/download")]
    public async Task<ActionResult> DownloadInvoice(int orderId, string invoiceId)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        if (order == null || string.IsNullOrEmpty(order.PaymentLinkId))
        {
            return NotFound();
        }

        var invoiceList = await _orderInvoiceService.GetByOrderIdAsync(orderId);
        var invoice = invoiceList.FirstOrDefault(i => i.InvoiceId == invoiceId);
        if (invoice == null)
        {
            return NotFound("Invoice not found for this order");
        }

        try
        {
            var invoiceFile = await _client.PaymentRequests.Invoices.DownloadAsync(invoiceId, order.PaymentLinkId);

            var fileName = invoiceFile.FileName ?? $"invoice_{invoiceId}.pdf";
            var contentType = invoiceFile.ContentType ?? "application/pdf";

            return File(invoiceFile.Content, contentType, fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Failed to download invoice {invoiceId}", error = ex.Message });
        }
    }
}