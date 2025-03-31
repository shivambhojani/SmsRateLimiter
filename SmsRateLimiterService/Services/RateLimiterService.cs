using Microsoft.Extensions.Options;
using SmsRateLimiterService.Config;
using SmsRateLimiterService.Interface;
using StackExchange.Redis;


namespace SmsRateLimiterService.Services
{
    public class RateLimiterService : IRateLimiter
    {
        private readonly IDatabase _redis;
        private readonly ILogger<RateLimiterService> _logger;
        private readonly RateLimiterSettings _settings;

        public RateLimiterService(IConnectionMultiplexer redis, ILogger<RateLimiterService> logger, IOptions<RateLimiterSettings> settings)
        {
            _redis = redis.GetDatabase();
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<bool> CanSendSmsAsync(string phoneNumber)
        {
            try
            {
                string perNumberKey = $"smsRate:{phoneNumber}";
                string globalKey = "smsRate:global";
                double now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                // Remove timestamps that are older than the sliding window size.
                // This ensures that only events within the last `WindowSizeInSeconds` milliseconds are considered.
                // Even though the key remains active, old requests need to be removed for correct rate limiting.
                await _redis.SortedSetRemoveRangeByScoreAsync(perNumberKey, double.NegativeInfinity, now - TimeSpan.FromSeconds(_settings.WindowSizeInSeconds).TotalMilliseconds);
                await _redis.SortedSetRemoveRangeByScoreAsync(globalKey, double.NegativeInfinity, now - TimeSpan.FromSeconds(_settings.WindowSizeInSeconds).TotalMilliseconds);

                double d = now - _settings.WindowSizeInSeconds;

                // Count remaining requests in the sliding window
                long perNumberCount = await _redis.SortedSetLengthAsync(perNumberKey);  
                long globalCount = await _redis.SortedSetLengthAsync(globalKey);

                if (perNumberCount >= _settings.PerNumberLimit || globalCount >= _settings.GlobalLimit)
                {
                    return false;  // Limit exceeded
                }

                // Add the new request to Redis with timestamp as score
                await _redis.SortedSetAddAsync(perNumberKey, now.ToString(), now);
                await _redis.SortedSetAddAsync(globalKey, now.ToString(), now);


                // Set a TTL (Time-To-Live) for the key to automatically remove inactive phone numbers.
                // If a phone number does not send messages for `WindowSizeInSeconds`, Redis will delete the key.
                // This prevents unused data from occupying memory indefinitely.
                await _redis.KeyExpireAsync(perNumberKey, TimeSpan.FromSeconds(_settings.WindowSizeInSeconds));
                await _redis.KeyExpireAsync(globalKey, TimeSpan.FromSeconds(_settings.WindowSizeInSeconds));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CanSendSmsAsync for phone number {PhoneNumber}", phoneNumber);
                return false;
            }
        }
    }
}
