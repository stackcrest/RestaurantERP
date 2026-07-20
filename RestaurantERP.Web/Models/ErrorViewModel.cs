namespace RestaurantERP.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public int StatusCode { get; set; } = 500;
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "An unexpected error occurred. Please try again.";
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public bool IsDevelopment { get; set; }
    public string? Detail { get; set; }
}
