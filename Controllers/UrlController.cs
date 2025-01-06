using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShortlinkApi.Data;
using ShortlinkApi.Models;
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

        public UrlController(IUrlShorteningService urlShorteningService)
        {
            _urlShorteningService = urlShorteningService;
        }

        [HttpPost("shorten")]
        public async Task<IActionResult> ShortenUrl([FromBody] ShortenUrlRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found.");
            }

            try
            {
                var shortCode = await _urlShorteningService.ShortenUrl(request.OriginalUrl, userId, request.CustomCode, request.ExpiresAt);
                var shortUrl = $"{Request.Scheme}://{Request.Host}/{shortCode}";
                return Ok(new { ShortUrl = shortUrl });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("{shortCode}")]
        public async Task<IActionResult> RedirectUrl(string shortCode)
        {
            var originalUrl = await _urlShorteningService.GetOriginalUrl(shortCode);
            if (originalUrl == null)
            {
                return NotFound("Short URL not found or has expired.");
            }

            return Redirect(originalUrl);
        }

        [HttpGet("{shortCode}/stats")]
        public async Task<IActionResult> GetUrlStats(string shortCode)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found.");
            }

            var stats = await _urlShorteningService.GetUrlStats(shortCode, userId);
            if (stats == null)
            {
                return NotFound("Short URL not found or access denied.");
            }

            return Ok(stats);
        }

        [HttpGet("my-urls")]
        public async Task<IActionResult> GetMyUrls()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found.");
            }

            var urls = await _urlShorteningService.GetMyUrls(userId);
            return Ok(urls);
        }
    }
}
