using PayOS;

namespace EliosPaymentService.Tests.Fixtures;

public static class PayOSClientFixture
{
    public static PayOSClient CreateTestClient()
    {
        // Create a minimal PayOSClient for testing
        // Note: This requires valid PayOS credentials in test environment
        // For unit tests, we'll use minimal config
        var options = new PayOSOptions
        {
            ClientId = "test-client-id",
            ApiKey = "test-api-key",
            ChecksumKey = "test-checksum-key"
        };
        return new PayOSClient(options);
    }
}

