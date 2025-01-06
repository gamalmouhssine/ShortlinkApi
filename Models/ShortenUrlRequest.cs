using System.ComponentModel.DataAnnotations;

namespace ShortlinkApi.Models
{
    public class ShortenUrlRequest
    {
        [Required]
        [Url]
        public string OriginalUrl { get; set; }

        public string CustomCode { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
