using System.ComponentModel.DataAnnotations;

namespace RestaurantERP.Web.Models;

public class ContactFormViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Enter a valid phone number")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Subject is required")]
    [StringLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be at least 10 characters")]
    public string Message { get; set; } = string.Empty;
}
