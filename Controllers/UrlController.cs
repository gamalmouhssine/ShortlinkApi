using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShortlinkApi.Data;
using ShortlinkApi.Services;
using System.Security.Claims;

namespace ShortlinkApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UrlController : ControllerBase
    {
        private readonly IUrlShorteningService _urlShorteningService;
        private readonly ApplicationDbContext _context;
        public UrlController(IUrlShorteningService urlShorteningService, ApplicationDbContext context)
        {
            _urlShorteningService = urlShorteningService;
            _context = context;
        }

        [HttpGet("my-urls")]
        public async Task<IActionResult> GetMyUrls()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var urls = await _context.ShortenedUrls
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(urls);
        }
        [HttpPost("shorten")]
        public async Task<IActionResult> ShortenUrl([FromBody] string originalUrl)
        {
            try
            {
                // Get the user ID from the claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var shortCode = await _urlShorteningService.ShortenUrl(originalUrl, userId);
                var shortUrl = $"{Request.Scheme}://{Request.Host}/{shortCode}";
                return Ok(new { shortUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
