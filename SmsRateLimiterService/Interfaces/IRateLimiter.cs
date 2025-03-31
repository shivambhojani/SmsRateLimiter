namespace SmsRateLimiterService.Interface
{
    public interface IRateLimiter
    {
        Task<bool> CanSendSmsAsync(string phoneNumber);
    }
}
