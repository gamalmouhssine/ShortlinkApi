using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShortlinkApi.Models;

namespace ShortlinkApi.Data
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
       : base(options)
        {
        }
        public DbSet<ShortenedUrl> ShortenedUrls { get; set; }
        public DbSet<UrlClick> UrlClicks { get; set; }
    }
}
