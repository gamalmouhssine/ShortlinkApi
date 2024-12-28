using Microsoft.AspNetCore.Identity;

namespace ShortlinkApi.Models
{
    public class ApplicationUser:IdentityUser
    {
        public List<ShortenedUrl> ShortenedUrls { get; set; }
    }
}
