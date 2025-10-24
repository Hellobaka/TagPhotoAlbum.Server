using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TagPhotoAlbum.Server.Models;

public class Passkey
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [JsonIgnore]
    [Required]
    public User User { get; set; } = null!;

    [Required]
    public string CredentialId { get; set; } = string.Empty;

    [Required]
    public string PublicKey { get; set; } = string.Empty;

    [Required]
    public int UserHandle { get; set; }

    [Required]
    public int Counter { get; set; }

    [Required]
    public string DeviceType { get; set; } = string.Empty;

    [Required]
    public string DeviceName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}