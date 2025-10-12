using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace csharpDialog.Core.Services;

/// <summary>
/// Service for downloading and caching icons from remote URLs
/// Mimics Munki's icon caching behavior for repository-based icon management
/// </summary>
public class IconCacheService
{
    private static readonly string DefaultCacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "csharpDialog",
        "IconCache"
    );

    private static readonly string CimianIconCacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "ManagedInstalls",
        "icons"
    );

    private readonly string _cacheDirectory;
    private readonly string _cimianCacheDirectory;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _cacheExpiration;

    /// <summary>
    /// Creates a new icon cache service
    /// </summary>
    /// <param name="cacheDirectory">Directory to store cached icons (optional)</param>
    /// <param name="cacheExpirationDays">Number of days to cache icons (default: 30)</param>
    public IconCacheService(string? cacheDirectory = null, int cacheExpirationDays = 30)
    {
        _cacheDirectory = cacheDirectory ?? DefaultCacheDirectory;
        _cimianCacheDirectory = CimianIconCacheDirectory;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10); // 10 second timeout for icon downloads
        _cacheExpiration = TimeSpan.FromDays(cacheExpirationDays);

        // Ensure cache directory exists
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>
    /// Gets a cached icon path or downloads it if not cached
    /// </summary>
    /// <param name="iconUrl">URL of the icon to download</param>
    /// <param name="fallbackIcon">Optional fallback icon path if download fails</param>
    /// <returns>Local file path to the cached icon, or fallback icon if download failed</returns>
    public async Task<string> GetCachedIconAsync(string iconUrl, string? fallbackIcon = null)
    {
        if (string.IsNullOrEmpty(iconUrl))
            return fallbackIcon ?? string.Empty;

        try
        {
            // Check if URL or already a local path
            if (!iconUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !iconUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Already a local path or icon name
                return iconUrl;
            }

            // Generate cache filename from URL
            var cacheFileName = GetCacheFileName(iconUrl);
            var cachedPath = Path.Combine(_cacheDirectory, cacheFileName);

            // Check if cached and not expired
            if (File.Exists(cachedPath))
            {
                var fileInfo = new FileInfo(cachedPath);
                if (DateTime.Now - fileInfo.LastWriteTime < _cacheExpiration)
                {
                    return cachedPath;
                }
            }

            // Download the icon
            var iconData = await _httpClient.GetByteArrayAsync(iconUrl);
            await File.WriteAllBytesAsync(cachedPath, iconData);

            return cachedPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download icon from {iconUrl}: {ex.Message}");
            return fallbackIcon ?? string.Empty;
        }
    }

    /// <summary>
    /// Gets a cached icon path synchronously (blocking)
    /// </summary>
    public string GetCachedIcon(string iconUrl, string? fallbackIcon = null)
    {
        return GetCachedIconAsync(iconUrl, fallbackIcon).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Resolves an icon reference to a full URL or local path
    /// Priority: Cimian cache > Local absolute path > csharpDialog cache > URL resolution
    /// </summary>
    /// <param name="iconReference">Icon name, URL, or local path</param>
    /// <param name="baseUrl">Base URL for icon resolution (optional)</param>
    /// <returns>Resolved icon URL or path</returns>
    public string ResolveIconReference(string iconReference, string? baseUrl = null)
    {
        if (string.IsNullOrEmpty(iconReference))
            return string.Empty;

        // Already a full URL
        if (iconReference.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            iconReference.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return iconReference;
        }

        // Already a local absolute path and exists
        if (Path.IsPathRooted(iconReference) && File.Exists(iconReference))
        {
            return iconReference;
        }

        // Priority 1: Check Cimian's icon cache (primary source for Cimian integration)
        var cimianPath = Path.Combine(_cimianCacheDirectory, iconReference);
        if (File.Exists(cimianPath))
        {
            return cimianPath;
        }

        // Priority 2: Check csharpDialog's icon cache
        var cachedPath = Path.Combine(_cacheDirectory, iconReference);
        if (File.Exists(cachedPath))
        {
            return cachedPath;
        }

        // Priority 3: Try to resolve with base URL
        if (!string.IsNullOrEmpty(baseUrl))
        {
            var baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
            var resolvedUri = new Uri(baseUri, iconReference);
            return resolvedUri.ToString();
        }

        // Fallback: Check for generic icon in Cimian cache
        var genericPath = Path.Combine(_cimianCacheDirectory, "_generic.png");
        if (File.Exists(genericPath))
        {
            return genericPath;
        }

        // Return as-is (might be an emoji or other non-file reference)
        return iconReference;
    }

    /// <summary>
    /// Pre-caches a set of icons for offline use
    /// </summary>
    /// <param name="iconUrls">List of icon URLs to pre-cache</param>
    /// <returns>Number of icons successfully cached</returns>
    public async Task<int> PreCacheIconsAsync(params string[] iconUrls)
    {
        int successCount = 0;

        foreach (var iconUrl in iconUrls)
        {
            try
            {
                await GetCachedIconAsync(iconUrl);
                successCount++;
            }
            catch
            {
                // Silently continue with next icon
            }
        }

        return successCount;
    }

    /// <summary>
    /// Clears the icon cache
    /// </summary>
    /// <param name="olderThanDays">Only clear icons older than specified days (0 = clear all)</param>
    public void ClearCache(int olderThanDays = 0)
    {
        if (!Directory.Exists(_cacheDirectory))
            return;

        var cutoffDate = DateTime.Now.AddDays(-olderThanDays);

        foreach (var file in Directory.GetFiles(_cacheDirectory))
        {
            try
            {
                if (olderThanDays == 0 || File.GetLastWriteTime(file) < cutoffDate)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore errors when deleting cache files
            }
        }
    }

    /// <summary>
    /// Gets the total size of the icon cache in bytes
    /// </summary>
    public long GetCacheSize()
    {
        if (!Directory.Exists(_cacheDirectory))
            return 0;

        long totalSize = 0;
        foreach (var file in Directory.GetFiles(_cacheDirectory))
        {
            try
            {
                totalSize += new FileInfo(file).Length;
            }
            catch
            {
                // Ignore errors
            }
        }

        return totalSize;
    }

    /// <summary>
    /// Generates a cache filename from a URL
    /// Uses SHA256 hash to avoid filename issues with special characters
    /// </summary>
    private static string GetCacheFileName(string url)
    {
        // Try to preserve original extension
        var uri = new Uri(url);
        var extension = Path.GetExtension(uri.LocalPath);
        if (string.IsNullOrEmpty(extension))
            extension = ".png"; // Default to PNG

        // Generate hash of URL for cache filename
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(url));
        var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        // Use first 16 characters of hash + extension
        return $"{hashString.Substring(0, 16)}{extension}";
    }

    /// <summary>
    /// Gets information about the cache
    /// </summary>
    /// <param name="includeCimianCache">Include Cimian cache statistics</param>
    public CacheInfo GetCacheInfo(bool includeCimianCache = true)
    {
        var info = new CacheInfo
        {
            CacheDirectory = _cacheDirectory,
            CacheExists = Directory.Exists(_cacheDirectory),
            CimianCacheDirectory = _cimianCacheDirectory,
            CimianCacheExists = Directory.Exists(_cimianCacheDirectory)
        };

        if (info.CacheExists)
        {
            var files = Directory.GetFiles(_cacheDirectory);
            info.FileCount = files.Length;
            info.TotalSizeBytes = GetCacheSize();
        }

        if (includeCimianCache && info.CimianCacheExists)
        {
            var cimianFiles = Directory.GetFiles(_cimianCacheDirectory);
            info.CimianFileCount = cimianFiles.Length;
            
            long cimianSize = 0;
            foreach (var file in cimianFiles)
            {
                try
                {
                    cimianSize += new FileInfo(file).Length;
                }
                catch { }
            }
            info.CimianTotalSizeBytes = cimianSize;
        }

        return info;
    }
}

/// <summary>
/// Information about the icon cache
/// </summary>
public class CacheInfo
{
    public string CacheDirectory { get; set; } = string.Empty;
    public bool CacheExists { get; set; }
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }

    public string CimianCacheDirectory { get; set; } = string.Empty;
    public bool CimianCacheExists { get; set; }
    public int CimianFileCount { get; set; }
    public long CimianTotalSizeBytes { get; set; }

    public string TotalSizeFormatted => FormatSize(TotalSizeBytes);
    public string CimianTotalSizeFormatted => FormatSize(CimianTotalSizeBytes);

    private static string FormatSize(long bytes)
    {
        double size = bytes;
        string[] units = { "B", "KB", "MB", "GB" };
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:F2} {units[unitIndex]}";
    }

    public override string ToString()
    {
        var parts = new List<string>();
        
        if (CacheExists)
            parts.Add($"csharpDialog: {FileCount} files, {TotalSizeFormatted}");
        
        if (CimianCacheExists)
            parts.Add($"Cimian: {CimianFileCount} files, {CimianTotalSizeFormatted}");

        return parts.Count > 0 
            ? $"Icon Caches - {string.Join(" | ", parts)}" 
            : "No icon caches found";
    }
}
