using Castle.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmsRateLimiterService.Config;
using SmsRateLimiterService.Services;
using StackExchange.Redis;

namespace SmsRateLimiterTests
{
    public class RateLimiterServiceTests
    {
        private readonly Mock<ILogger<RateLimiterService>> mockLogger;
        private readonly Mock<IConnectionMultiplexer> mockConnectionMultiplexer;
        private readonly Mock<IDatabase> mockDatabase;
        private readonly Mock<IOptions<RateLimiterSettings>> mockRateLimiterSettings;
        private readonly RateLimiterService rateLimiter;

        public RateLimiterServiceTests()
        {
            // 1. Set up mock RateLimiterSettings
            var rateLimiterSettings = new RateLimiterSettings
            {
                PerNumberLimit = 5,
                GlobalLimit = 100,
                WindowSizeInSeconds = 5
            };

            // 2. Set up mock IOptions<RateLimiterSettings>
            mockRateLimiterSettings = new Mock<IOptions<RateLimiterSettings>>();
            mockRateLimiterSettings.Setup(o => o.Value).Returns(rateLimiterSettings);

            // Initialize mocks
            mockLogger = new Mock<ILogger<RateLimiterService>>();
            mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
            mockDatabase = new Mock<IDatabase>();

            // Set up GetDatabase() to return the mocked IDatabase
            mockConnectionMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                                      .Returns(mockDatabase.Object);

            // Instantiate the RateLimiterService with mocked dependencies
            rateLimiter = new RateLimiterService(mockConnectionMultiplexer.Object, mockLogger.Object, mockRateLimiterSettings.Object);
        }

        [Fact]
        public async Task CanSendSmsAsync_ShouldReturnTrue_WhenGlobalANDPhone_UnderLimit()
        {
            // Arrange


            // Mock Redis SortedSetLengthAsync to return 0 (indicating no prior messages)
            mockDatabase.Setup(r => r.SortedSetLengthAsync(It.IsAny<RedisKey>(), double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(0);  // Simulate that no messages have been sent yet

            // Alternatively, if you want to match a specific key:
            mockDatabase.Setup(r => r.SortedSetLengthAsync("smsRate:1234567890", double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(0);  // Simulate no messages for this phone number

            // Mock Redis SortedSetAddAsync (simulating storing timestamps)
            mockDatabase.Setup(r => r.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), CommandFlags.None))
                     .ReturnsAsync(true);

            // Act
            bool result = await rateLimiter.CanSendSmsAsync("1234567890");

            // Assert
            Assert.True(result);  // Should allow SMS sending
        }

        [Fact]
        public async Task CanSendSmsAsync_ShouldReturnTrue_WhenGlobal_UnderLimit_Phone_OverLimit()
        {
            // Arrange


            // Mock Redis SortedSetLengthAsync to return 0 (indicating no prior messages)
            mockDatabase.Setup(r => r.SortedSetLengthAsync(It.IsAny<RedisKey>(), double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(50);  // Simulate that no messages have been sent yet

            // Alternatively, if you want to match a specific key:
            mockDatabase.Setup(r => r.SortedSetLengthAsync("smsRate:1234567890", double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(5);  // Simulate no messages for this phone number

            // Mock Redis SortedSetAddAsync (simulating storing timestamps)
            mockDatabase.Setup(r => r.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), CommandFlags.None))
                     .ReturnsAsync(true);

            // Act
            bool result = await rateLimiter.CanSendSmsAsync("1234567890");

            // Assert
            Assert.False(result);  // Should allow SMS sending
        }

        [Fact]
        public async Task CanSendSmsAsync_ShouldReturnTrue_WhenGlobal_OverLimit_Phone_UnderLimit()
        {
            // Arrange


            // Mock Redis SortedSetLengthAsync to return 0 (indicating no prior messages)
            mockDatabase.Setup(r => r.SortedSetLengthAsync(It.IsAny<RedisKey>(), double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(100);  // Simulate that no messages have been sent yet

            // Alternatively, if you want to match a specific key:
            mockDatabase.Setup(r => r.SortedSetLengthAsync("smsRate:1234567890", double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(1);  // Simulate no messages for this phone number

            // Mock Redis SortedSetAddAsync (simulating storing timestamps)
            mockDatabase.Setup(r => r.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), CommandFlags.None))
                     .ReturnsAsync(true);

            // Act
            bool result = await rateLimiter.CanSendSmsAsync("1234567890");

            // Assert
            Assert.False(result);  // Should allow SMS sending
        }

        [Fact]
        public async Task CanSendSmsAsync_ShouldReturnFalse_WhenGlobalLimitExceed()
        {
            // Arrange


            // Mock Redis SortedSetLengthAsync to return 0 (indicating no prior messages)
            mockDatabase.Setup(r => r.SortedSetLengthAsync(It.IsAny<RedisKey>(), double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(100);  // Simulate that no messages have been sent yet


            mockDatabase.Setup(r => r.SortedSetLengthAsync("smsRate:1234567890", double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(0);  // Simulate no messages for this phone number





            // Mock Redis SortedSetAddAsync (simulating storing timestamps)
            mockDatabase.Setup(r => r.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), CommandFlags.None))
                     .ReturnsAsync(false);

            // Act
            bool result = await rateLimiter.CanSendSmsAsync("1234567890");

            // Assert
            Assert.False(result);  // Should allow SMS sending
        }

        [Fact]
        public async Task CanSendSmsAsync_ShouldReturnFalse_WhenPerNumberLimitExceeded()
        {
            // Arrange


            // Mock Redis SortedSetLengthAsync to return 0 (indicating no prior messages)
            mockDatabase.Setup(r => r.SortedSetLengthAsync(It.IsAny<RedisKey>(), double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(99);  // Simulate that no messages have been sent yet


            mockDatabase.Setup(r => r.SortedSetLengthAsync("smsRate:1234567890", double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(5);  // Simulate no messages for this phone number

            // Act
            bool result = await rateLimiter.CanSendSmsAsync("1234567890");

            // Assert
            Assert.False(result);  // Should allow SMS sending
        }

        [Fact]
        public async Task CanSendSmsAsync_ShouldReturnTrue_WhenPerNumberLimitExceeded_NewComes_After2Seconds()
        {
            // Arrange


            // Mock Redis SortedSetLengthAsync to return 0 (indicating no prior messages)
            mockDatabase.Setup(r => r.SortedSetLengthAsync(It.IsAny<RedisKey>(), double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(90);  // Simulate that no messages have been sent yet


            mockDatabase.Setup(r => r.SortedSetLengthAsync("smsRate:1234567890", double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(5);  // Simulate no messages for this phone 

            // Simulate that the sliding window should have expired or have reduced the count
            mockDatabase.Setup(r => r.SortedSetRemoveRangeByScoreAsync("smsRate:1234567890", double.NegativeInfinity, It.IsAny<double>(), Exclude.None, CommandFlags.None))
                .ReturnsAsync(1);


            // Act - Call to CanSendSmsAsync
            bool result = await rateLimiter.CanSendSmsAsync("1234567890");

            // Assert - Initially, the rate limiter should block sending due to the 5-message limit.
            Assert.False(result); 


            // Add a 2-second delay to simulate the passage of time (this allows the old messages to be removed)
            await Task.Delay(2000);

            // Adjust the mock to return a lower count after 2 seconds (simulate that old messages were removed)
            mockDatabase.Setup(r => r.SortedSetLengthAsync("smsRate:1234567890", double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(3);  // Simulate 3 messages remaining after 2 seconds

            // Act again - After 2 seconds, the count should drop below the limit (since old timestamps are removed)
            result = await rateLimiter.CanSendSmsAsync("1234567890");

            // Assert - After waiting 2 seconds, it should allow the message to be sent.
            Assert.True(result);  
        }


        [Fact]
        public async Task CanSendSmsAsync_ShouldReturnTrue_WhenGlobalLimitExceeded_NewComes_After5Seconds()
        {
            // Arrange
            // Simulate the initial condition where 100 messages have been sent globally within the last 5 seconds

            mockDatabase.Setup(r => r.SortedSetLengthAsync("smsRate:global", double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(100);  

            // Simulate that the sliding window should have expired or reduced the count
            mockDatabase.Setup(r => r.SortedSetRemoveRangeByScoreAsync("smsRate:global", double.NegativeInfinity, It.IsAny<double>(), Exclude.None, CommandFlags.None))
                        .ReturnsAsync(1);  

            // Act - First check before the 2-second delay
            bool resultBeforeDelay = await rateLimiter.CanSendSmsAsync("1234567890");

            // Assert - Initially, the rate limiter should block sending due to the global limit being exceeded
            Assert.False(resultBeforeDelay); 

            // Simulate the passing of time (2 seconds delay)
            await Task.Delay(2000);  

            // Adjust the mock to return a lower count after 2 seconds (simulate that old global messages were removed)
            mockDatabase.Setup(r => r.SortedSetLengthAsync("smsRate:global", double.NegativeInfinity, double.PositiveInfinity, Exclude.None, CommandFlags.None))
                        .ReturnsAsync(95);  

            // Act again after 2 seconds to check if sending is allowed
            bool resultAfterDelay = await rateLimiter.CanSendSmsAsync("1234567890");

            // Assert - After 2 seconds, the global count should be below the limit (allowing SMS to be sent)
            Assert.True(resultAfterDelay);  
        }



    }
}