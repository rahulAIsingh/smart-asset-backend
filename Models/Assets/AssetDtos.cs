using System.ComponentModel.DataAnnotations;

namespace SmartAssetManager.Api.Models.Assets;

public sealed class CreateAssetDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? SerialNumber { get; set; }

    [MaxLength(255)]
    public string? DeviceSerialNumber { get; set; }

    [MaxLength(255)]
    public string? Company { get; set; }

    [MaxLength(255)]
    public string? Model { get; set; }

    [MaxLength(128)]
    public string? Department { get; set; }

    [MaxLength(64)]
    public string? WarrantyStart { get; set; }

    [MaxLength(64)]
    public string? WarrantyEnd { get; set; }

    [MaxLength(255)]
    public string? WarrantyVendor { get; set; }

    [MaxLength(4000)]
    public string? Configuration { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

public sealed class UpdateAssetDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? SerialNumber { get; set; }

    [MaxLength(255)]
    public string? DeviceSerialNumber { get; set; }

    [MaxLength(255)]
    public string? Company { get; set; }

    [MaxLength(255)]
    public string? Model { get; set; }

    [MaxLength(128)]
    public string? Department { get; set; }

    [MaxLength(64)]
    public string? WarrantyStart { get; set; }

    [MaxLength(64)]
    public string? WarrantyEnd { get; set; }

    [MaxLength(255)]
    public string? WarrantyVendor { get; set; }

    [MaxLength(4000)]
    public string? Configuration { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}
