using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace ProxySharp.Models;

public class Configuration
{
    [ConfigurationKeyName("baseUrl")]
    [Required(ErrorMessage = "baseUrl is required.")]
    [Url(ErrorMessage = "baseUrl must be a valid URL.")]
    public string BaseUrl { get; set; } = string.Empty;

    [ConfigurationKeyName("authTimeout")]
    [Required(ErrorMessage = "authTimeout is required.")]
    [Range(1, 60, ErrorMessage = "authTimeout must be a positive integer.")]
    public int AuthTimeout { get; set; } = 30;

    [ConfigurationKeyName("requestTimeout")]
    [Required(ErrorMessage = "requestTimeout is required.")]
    [Range(1, 60, ErrorMessage = "requestTimeout must be a positive integer.")]
    public int RequestTimeout { get; set; } = 60;

    [ConfigurationKeyName("authUsername")]
    [Required(ErrorMessage = "authUsername is required.")]
    [MinLength(3, ErrorMessage = "{0} must be {1} characters long.")]
    public string AuthUsername { get; set; } = string.Empty;

    [ConfigurationKeyName("authPassword")]
    [Required(ErrorMessage = "authPassword is required.")]
    [MinLength(3, ErrorMessage = "{0} must be {1} characters long.")]
    public string AuthPassword { get; set; } = string.Empty;

    [ConfigurationKeyName("authEndpoint")]
    [Required(ErrorMessage = "authEndpoint is required.")]
    [MinLength(3, ErrorMessage = "{0} must be {1} characters long.")]
    public string AuthEndpoint { get; set; } = string.Empty;
}
