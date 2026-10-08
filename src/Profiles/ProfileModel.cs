using System.Text.Json.Serialization;
using USBSilentCopy.Configuration;

namespace USBSilentCopy.Profiles;

/// <summary>
/// تنظیمات اختصاصی پروفایل یک حافظه USB مشخص
/// </summary>
public class UsbProfile
{
    [JsonPropertyName("profileId")]
    public string ProfileId { get; set; } = string.Empty;

    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("customDestination")]
    public string? CustomDestination { get; set; }

    [JsonPropertyName("backupFolderTemplate")]
    public string? BackupFolderTemplate { get; set; }

    [JsonPropertyName("customSpeedLimitMBps")]
    public int? CustomSpeedLimitMBps { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [JsonPropertyName("lastSeenDate")]
    public DateTime LastSeenDate { get; set; } = DateTime.Now;

    [JsonPropertyName("customFilters")]
    public FilterConfig? CustomFilters { get; set; }
}
