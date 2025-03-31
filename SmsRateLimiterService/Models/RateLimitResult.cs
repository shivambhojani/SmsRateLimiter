namespace SmsRateLimiterService.Models
{
    public class RateLimitResult
    {
        public Boolean CanSent { get; set; }
        public String Message {  get; set; }
    }
}
