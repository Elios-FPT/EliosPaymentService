namespace EliosPaymentService.Models;

/// <summary>
/// Represents a paginated result containing orders, pagination metadata, and user statistics
/// </summary>
public class PagedOrderResult
{
    /// <summary>
    /// List of orders for the current page
    /// </summary>
    public List<Order> Data { get; set; } = [];

    /// <summary>
    /// Pagination information including page number, total count, etc.
    /// </summary>
    public PaginationMetadata Pagination { get; set; } = new();

    /// <summary>
    /// Statistical summary of all user orders (not just current page)
    /// </summary>
    public OrderStatistics Statistics { get; set; } = new();
}

/// <summary>
/// Metadata about pagination state and total record counts
/// </summary>
public class PaginationMetadata
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Total number of records matching the query
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages based on limit
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// Statistical summary of user's order history
/// </summary>
public class OrderStatistics
{
    /// <summary>
    /// Total amount paid across all completed orders
    /// </summary>
    public long TotalSpent { get; set; }

    /// <summary>
    /// Count of orders grouped by their payment status
    /// </summary>
    public Dictionary<string, int> OrderCountByStatus { get; set; } = [];
}
