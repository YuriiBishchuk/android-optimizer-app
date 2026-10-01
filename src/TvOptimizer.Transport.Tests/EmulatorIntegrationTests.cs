using System;
using System.Threading.Tasks;
using TvOptimizer.Transport.Adb;
using Xunit;

namespace TvOptimizer.Transport.Tests
{
    public class EmulatorIntegrationTests
    {
        [Fact]
        public async Task ConnectAndShell_ReturnsExpectedOutput()
        {
            await using var client = new AdbClient("127.0.0.1", 5555, tls: false);
            await client.ConnectAsync();
            var result = await client.ShellAsync("echo hello");
            Assert.Contains("hello", result.Trim());
        }

        [Fact]
        public async Task Reconnect_AfterDisconnect_Works()
        {
            // First connection
            await using var client1 = new AdbClient("127.0.0.1", 5555, tls: false);
            await client1.ConnectAsync();
            var result1 = await client1.ShellAsync("echo first");
            Assert.Contains("first", result1.Trim());

            // Second connection (new client)
            await using var client2 = new AdbClient("127.0.0.1", 5555, tls: false);
            await client2.ConnectAsync();
            var result2 = await client2.ShellAsync("echo second");
            Assert.Contains("second", result2.Trim());
        }

        [Fact]
        public async Task SocketCleanup_DisposeAsyncDoesNotThrow()
        {
            var client = new AdbClient("127.0.0.1", 5555, tls: false);
            await client.ConnectAsync();
            await client.ShellAsync("echo test");
            await client.DisposeAsync(); // Should not throw
            Assert.True(true);
        }
    }
}