using System.Text.Encodings.Web;
using System.Text.Json;
using USBSilentCopy.Core;

namespace USBSilentCopy.Profiles;

/// <summary>
/// مدیریت ذخیره‌سازی، خواندن و به‌روزرسانی پروفایل‌های اختصاصی درایوهای USB
/// </summary>
public class ProfileManager
{
    private readonly string _profilesDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ProfileManager(string profilesDirectory)
    {
        _profilesDirectory = Path.GetFullPath(profilesDirectory);
        if (!Directory.Exists(_profilesDirectory))
        {
            Directory.CreateDirectory(_profilesDirectory);
        }
    }

    public string ProfilesDirectory => _profilesDirectory;

    public UsbProfile GetOrCreateProfile(UsbDeviceInfo device)
    {
        var profileId = TemplateExpander.SanitizePathName(device.ProfileIdentifier);
        var profilePath = GetProfileFilePath(profileId);

        if (File.Exists(profilePath))
        {
            try
            {
                var json = File.ReadAllText(profilePath);
                var profile = JsonSerializer.Deserialize<UsbProfile>(json, JsonOptions);
                if (profile != null)
                {
                    profile.LastSeenDate = DateTime.Now;
                    SaveProfile(profile);
                    return profile;
                }
            }
            catch
            {
                // در صورت خطا در فایل پروفایل، پروفایل پیش‌فرض جدید ایجاد می‌شود
            }
        }

        var newProfile = new UsbProfile
        {
            ProfileId = profileId,
            FriendlyName = device.DisplayName,
            Enabled = true,
            CreatedDate = DateTime.Now,
            LastSeenDate = DateTime.Now
        };

        SaveProfile(newProfile);
        return newProfile;
    }

    public bool SaveProfile(UsbProfile profile)
    {
        try
        {
            var profileId = TemplateExpander.SanitizePathName(profile.ProfileId);
            var dir = Path.Combine(_profilesDirectory, profileId);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var filePath = Path.Combine(dir, "profile.json");
            var json = JsonSerializer.Serialize(profile, JsonOptions);
            File.WriteAllText(filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public List<UsbProfile> GetAllProfiles()
    {
        var list = new List<UsbProfile>();
        if (!Directory.Exists(_profilesDirectory))
            return list;

        foreach (var subDir in Directory.GetDirectories(_profilesDirectory))
        {
            var profilePath = Path.Combine(subDir, "profile.json");
            if (File.Exists(profilePath))
            {
                try
                {
                    var json = File.ReadAllText(profilePath);
                    var profile = JsonSerializer.Deserialize<UsbProfile>(json, JsonOptions);
                    if (profile != null)
                    {
                        list.Add(profile);
                    }
                }
                catch
                {
                    // از پروفایل‌های مخدوش صرف‌نظر می‌شود
                }
            }
        }

        return list;
    }

    private string GetProfileFilePath(string profileId)
    {
        return Path.Combine(_profilesDirectory, profileId, "profile.json");
    }
}
