namespace ShortlinkApi.Models
{
    public class ShortenedUrl
    {
        public int Id { get; set; }
        public string OriginalUrl { get; set; }
        public string ShortCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ClickCount { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
