
using Microsoft.EntityFrameworkCore;
using ShortlinkApi.Data;
using ShortlinkApi.Models;
using System.Security.Claims;

namespace ShortlinkApi.Services
{
    public class UrlShorteningService : IUrlShorteningService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 6;

        public UrlShorteningService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task<string> ShortenUrl(string originalUrl,string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null");

            // Validate URL
            if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
            {
                throw new ArgumentException("Invalid URL format");
            }

            // Generate unique short code
            string shortCode;
            do
            {
                shortCode = GenerateShortCode();
            } while (await _context.ShortenedUrls.AnyAsync(u => u.ShortCode == shortCode));

            var shortenedUrl = new ShortenedUrl
            {
                OriginalUrl = originalUrl,
                ShortCode = shortCode,
                CreatedAt = DateTime.UtcNow,
                ClickCount = 0,
                UserId = userId 
            };

            _context.ShortenedUrls.Add(shortenedUrl);
            await _context.SaveChangesAsync();

            return shortCode;
        }

        public async Task<string> GetOriginalUrl(string shortCode)
        {
            var shortenedUrl = await _context.ShortenedUrls
            .FirstOrDefaultAsync(u => u.ShortCode == shortCode);

            if (shortenedUrl == null)
                return null;

            // Increment click count
            shortenedUrl.ClickCount++;
            await _context.SaveChangesAsync();

            return shortenedUrl.OriginalUrl;
        }

        private string GenerateShortCode()
        {
            var random = new Random();
            var chars = new char[CodeLength];

            for (int i = 0; i < CodeLength; i++)
            {
                chars[i] = AllowedChars[random.Next(AllowedChars.Length)];
            }

            return new string(chars);
        }

    }
}
