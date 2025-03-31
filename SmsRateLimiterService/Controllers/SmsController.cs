using Microsoft.AspNetCore.Mvc;
using SmsRateLimiterService.Interface;
using SmsRateLimiterService.Models;
using System.Text.RegularExpressions;

namespace SmsRateLimiterService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SmsController : ControllerBase
    {
        private readonly IRateLimiter _rateLimiter;

        public SmsController(IRateLimiter rateLimiter)
        {
            _rateLimiter = rateLimiter;
        }

        [HttpGet("check-limit")]
        public async Task<ActionResult<RateLimitResult>> checkSmsLimit(string phoneNumber)
        {

            // Check if the phone number is missing or empty
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return BadRequest(new { Message = "Phone number is required" });
            }

            // Optional: Validate phone number format (you can adjust this regex as needed)
            var phoneNumberPattern = @"^\+?[1-9]\d{1,14}$"; // Basic international phone number format
            if (!Regex.IsMatch(phoneNumber, phoneNumberPattern))
            {
                return BadRequest(new { Message = "Invalid phone number format" });
            }

            // Check rate limit
            var _result = await _rateLimiter.CanSendSmsAsync(phoneNumber);

            return Ok(new RateLimitResult
            {
                CanSent = _result,
                Message = _result ? "Can send SMS" : "Cannot send SMS"
            });

        }
    }
}
