namespace ShortlinkApi.Models
{
    public class UrlClick
    {
        public int Id { get; set; }
        public int ShortenedUrlId { get; set; } 
        public string IpAddress { get; set; } 
        public string UserAgent { get; set; } 
        public string DeviceType { get; set; } 
        public string Browser { get; set; } 
        public string Referrer { get; set; }
        public DateTime ClickedAt { get; set; } 

        // Navigation property
        public ShortenedUrl ShortenedUrl { get; set; }
    }
}
