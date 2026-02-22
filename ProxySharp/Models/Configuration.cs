using System.ComponentModel.DataAnnotations;

namespace ProxySharp.Models;

public class Configuration
{
    [Required(ErrorMessage = "BaseUrl is required.")]
    [Url(ErrorMessage = "BaseUrl must be a valid URL.")]
    public string BaseUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "AuthTimeout is required.")]
    [Range(1, 60, ErrorMessage = "AuthTimeout must be a positive integer.")]
    public int AuthTimeout { get; set; } = 30;

    [Required(ErrorMessage = "RequestTimeout is required.")]
    [Range(1, 60, ErrorMessage = "RequestTimeout must be a positive integer.")]
    public int RequestTimeout { get; set; } = 60;
}
