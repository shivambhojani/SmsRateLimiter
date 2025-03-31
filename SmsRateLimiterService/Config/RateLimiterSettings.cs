namespace SmsRateLimiterService.Config
{
    public class RateLimiterSettings
    {
        public int PerNumberLimit { get; set; }
        public int GlobalLimit { get; set; }
        public int WindowSizeInSeconds { get; set; }
    }
}
