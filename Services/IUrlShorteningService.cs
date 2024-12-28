namespace ShortlinkApi.Services
{
    public interface IUrlShorteningService
    {
        Task<string> ShortenUrl(string originalUrl,string userId);
        Task<string> GetOriginalUrl(string shortCode);
    }
}
