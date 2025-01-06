using ShortlinkApi.Models;
using System.Net.NetworkInformation;

namespace ShortlinkApi.Services
{
    public interface IUrlShorteningService
    {
        Task<string> ShortenUrl(string originalUrl, string userId, string customCode = null, DateTime? expiresAt = null);
        Task<string> GetOriginalUrl(string shortCode);
        Task<UrlStatistics> GetUrlStats(string shortCode, string userId);
        Task<IEnumerable<ShortenedUrl>> GetMyUrls(string userId);

      
    }
}
