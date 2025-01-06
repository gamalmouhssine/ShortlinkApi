
using Microsoft.EntityFrameworkCore;
using ShortlinkApi.Data;
using ShortlinkApi.Models;
using System.Security.Claims;
using UAParser;
using static ShortlinkApi.Services.IUrlShorteningService;

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

        public async Task<string> ShortenUrl(string originalUrl, string userId, string customCode = null, DateTime? expiresAt = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null");

            // Validate URL
            if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
            {
                throw new ArgumentException("Invalid URL format");
            }

            // Use custom code if provided
            string shortCode;
            if (!string.IsNullOrEmpty(customCode))
            {
                if (await _context.ShortenedUrls.AnyAsync(u => u.ShortCode == customCode))
                {
                    throw new ArgumentException("Custom code is already in use");
                }
                shortCode = customCode;
            }
            else
            {
                // Generate unique short code
                do
                {
                    shortCode = GenerateShortCode();
                } while (await _context.ShortenedUrls.AnyAsync(u => u.ShortCode == shortCode));
            }

            var shortenedUrl = new ShortenedUrl
            {
                OriginalUrl = originalUrl,
                ShortCode = shortCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
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

            // Check if the URL has expired
            if (shortenedUrl.ExpiresAt.HasValue && shortenedUrl.ExpiresAt < DateTime.UtcNow)
            {
                return null;
            }

            // Capture request details
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            var referrer = httpContext.Request.Headers["Referer"].ToString();
            var deviceType = GetDeviceType(userAgent);

            // Parse the User-Agent string to get browser information
            var uaParser = Parser.GetDefault();
            var clientInfo = uaParser.Parse(userAgent);
            var browser = $"{clientInfo.UA.Family} {clientInfo.UA.Major}.{clientInfo.UA.Minor}";

            // Create a new UrlClick record
            var urlClick = new UrlClick
            {
                ShortenedUrlId = shortenedUrl.Id,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceType = deviceType,
                Browser = browser, // Store the browser information
                Referrer = referrer,
                ClickedAt = DateTime.UtcNow
            };

            _context.UrlClicks.Add(urlClick);

            // Increment click count
            shortenedUrl.ClickCount++;
            await _context.SaveChangesAsync();

            return shortenedUrl.OriginalUrl;
        }

        public async Task<UrlStatistics> GetUrlStats(string shortCode, string userId)
        {
            var shortenedUrl = await _context.ShortenedUrls
                .Include(u => u.UrlClicks) // Include the UrlClicks navigation property
                .FirstOrDefaultAsync(u => u.ShortCode == shortCode && u.UserId == userId);

            if (shortenedUrl == null)
                return null;

            var deviceStats = shortenedUrl.UrlClicks
                .GroupBy(c => c.DeviceType)
                .Select(g => new DeviceStat
                {
                    DeviceType = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var referrerStats = shortenedUrl.UrlClicks
                .GroupBy(c => c.Referrer)
                .Select(g => new ReferrerStat
                {
                    Referrer = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var dateStats = shortenedUrl.UrlClicks
                .GroupBy(c => c.ClickedAt.Date)
                .Select(g => new DateStat
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var browserStats = shortenedUrl.UrlClicks
                .GroupBy(c => c.Browser)
                .Select(g => new BrowserStat
                {
                    Browser = g.Key,
                    Count = g.Count()
                })
                .ToList();

            return new UrlStatistics
            {
                TotalClicks = shortenedUrl.ClickCount,
                DeviceStats = deviceStats,
                ReferrerStats = referrerStats,
                DateStats = dateStats,
                BrowserStats = browserStats // Include browser statistics
            };
        }

        public async Task<IEnumerable<ShortenedUrl>> GetMyUrls(string userId)
        {
            return await _context.ShortenedUrls
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
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

        private string GetDeviceType(string userAgent)
        {
            if (userAgent.Contains("Mobile"))
                return "Mobile";
            if (userAgent.Contains("Tablet"))
                return "Tablet";
            return "Desktop";
        }

    }
    public class UrlStatistics
    {
        public int TotalClicks { get; set; }
        public IEnumerable<DeviceStat> DeviceStats { get; set; }
        public IEnumerable<ReferrerStat> ReferrerStats { get; set; }
        public IEnumerable<DateStat> DateStats { get; set; }
        public IEnumerable<BrowserStat> BrowserStats { get; set; } // Add this
    }

    public class BrowserStat
    {
        public string Browser { get; set; }
        public int Count { get; set; }
    }

    public class DeviceStat
    {
        public string DeviceType { get; set; }
        public int Count { get; set; }
    }

    public class ReferrerStat
    {
        public string Referrer { get; set; }
        public int Count { get; set; }
    }

    public class DateStat
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
