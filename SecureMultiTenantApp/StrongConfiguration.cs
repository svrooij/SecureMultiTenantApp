using System.ComponentModel.DataAnnotations;

namespace SecureMultiTenantApp;

public class StrongConfiguration
{
    [Required]
    public string? ClientId { get; set; }
    [Required]
    public string? ClientCertificateName { get; set; }
    
    public string? ClientCertificateNameNoKey { get; set; }
    [Required]
    public string? TenantId { get; set; }

    [Required]
    public Uri? KeyVaultUri { get; set; }

    [Required]
    public string? GraphScope { get; set; } = "https://graph.microsoft.com/.default";
}
