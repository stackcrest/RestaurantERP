namespace RestaurantERP.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public ResponseMeta? Meta { get; set; }
    public PaginationInfo? Pagination { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Success", PaginationInfo? pagination = null) =>
        new() { Success = true, Message = message, Data = data, Pagination = pagination, Meta = new ResponseMeta() };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? new List<string>(), Meta = new ResponseMeta() };
}

public class ResponseMeta
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N")[..12];
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
}
