# SmsRateLimiterService

### From: Shivam Bhojani

## Overview

`SmsRateLimiterService` is a .NET Core-based microservice designed to manage SMS sending limits for business phone numbers. The service acts as a gatekeeper between the base SMS sender service and third-party SMS providers, ensuring that messages are sent without exceeding the provider's rate limits.

The service checks two key limits:
- A maximum number of messages per second for each business phone number.
- A maximum number of messages per second for the entire account.

The service leverages Redis for efficient rate-limiting, with support for sliding window limits, automatic cleanup of inactive phone numbers, and scalability in a distributed deployment setup. This ensures that the service can handle a high volume of requests across multiple instances in a distributed environment.

## Key Features
- **Per Phone Number Limit**: Ensures that each business phone number does not exceed the maximum allowed messages per second.
- **Global Account Limit**: Ensures that the total number of messages sent across the entire account does not exceed the maximum allowed messages per second.
- **Sliding Window Rate Limiting**: Uses a sliding window to track messages sent in real-time, allowing precise rate limiting.
- **Automatic Expiry for Inactive Numbers**: Phone numbers that do not send messages for a configured period are automatically removed from tracking, reducing memory usage.
- **Scalability in Distributed Deployment Setup**: The service is designed to scale efficiently in a distributed deployment setup, ensuring that Redis handles the rate limiting consistently across multiple instances of the service.

## Project Structure

The project consists of two main components:

1. **SmsRateLimiterService** - The core service responsible for rate limiting.
2. **SmsRateLimiterTests** - The test project containing unit and integration tests for the service.

### Service Project Structure (`SmsRateLimiterService`)

- **Config**
  - `RateLimiterSettings.cs`: Contains the configuration options for rate limiting, including `PerNumberLimit`, `GlobalLimit`, and `WindowSizeInSeconds`.
  
- **Controllers**
  - `SmsController.cs`: The API controller that exposes an endpoint to check if an SMS can be sent for a given phone number. It uses the `IRateLimiter` interface to perform the rate-limiting check.
  
- **Interface**
  - `IRateLimiter.cs`: The interface that defines the contract for the rate limiter service, specifically the `CanSendSmsAsync` method to check if an SMS can be sent for a phone number.
  
- **Models**
  - `RateLimitResult.cs`: A model representing the result of the rate-limiting check. It contains properties like `CanSent` (indicating if an SMS can be sent) and a `Message` providing additional information.
  
- **Services**
  - `RateLimiterService.cs`: The core implementation of the rate-limiting logic. It interacts with Redis to track and limit the number of SMS messages sent both per phone number and globally across the account.

### Test Project Structure (`SmsRateLimiterTests`)

This project includes unit tests for the `RateLimiterService` class, written using the **xUnit** framework and **Moq** for mocking dependencies. The tests verify the core functionality of the rate limiter, including:

- Verifying that SMS can be sent when both global and per-phone limits are within thresholds.
- Ensuring SMS cannot be sent when either the global or per-phone limits are exceeded.
- Validating the sliding window behavior for both global and per-phone limits, ensuring messages are allowed after the window period expires.

The tests mock the dependencies such as Redis interactions, logging, and rate limiter settings to ensure isolated and controlled test scenarios.

## API Endpoints

The service exposes the following API endpoint:

### `GET api/Sms/check-limit?phoneNumber={phoneNumber}`

This endpoint checks whether an SMS can be sent for a given phone number, based on the rate limits.

#### Query Parameters:
- `phoneNumber` (required): The phone number to check.

#### Example Response

```json
{
  "canSent": true,
  "message": "Can send SMS"
}
```
## Redis Setup Using Docker

To run Redis locally, you can use Docker. Follow these steps to set up a Redis container:

1. **Install Docker**: Ensure Docker is installed on your machine. If it's not, you can download and install it from [Docker's website](https://www.docker.com/get-started).

2. **Pull Redis Docker Image**: Open a terminal and run the following command to pull the Redis image from Docker Hub:

    ```bash
    docker pull redis
    ```

3. **Run Redis in Docker**: After the image is pulled, you can start the Redis container:

    ```bash
    docker run --name redis -p 6379:6379 -d redis
    ```

    This command will:
    - Name the container "redis"
    - Expose Redis on port `6379`
    - Run Redis in detached mode (in the background)

4. **Verify Redis is Running**: To confirm that the Redis container is up and running, use the following command:

    ```bash
    docker ps
    ```

    You should see the "redis" container listed with port `6379` exposed.

5. **Connecting Your Application to Redis**: In the `Program.cs` file, the application is configured to connect to Redis at `localhost:6379`:

    ```csharp
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
    ```

    If Redis is running on a different host or port, update this program.cs file accordingly.


