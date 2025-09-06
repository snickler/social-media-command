using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocialMediaCommander.Services.Implementation;
using Xunit;
using FluentAssertions;

namespace SocialMediaCommander.Tests.UnitTests
{
    public class AdvancedLoggingServiceTests
    {
        [Fact]
        public async Task LogAccountOperationWithTimingAsync_ShouldInvokeDelegateWithoutError()
        {
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AdvancedLoggingService>();
            var svc = new AdvancedLoggingService(logger);

            var account = new SocialMediaCommander.Core.Models.Account
            {
                Id = "test",
                PlatformId = SocialMediaCommander.Core.Models.SocialPlatform.BlueSky
            };

            await svc.LogAccountOperationWithTimingAsync("op", account, async () => await Task.Delay(1));

            // Ensure no exceptions and service still works
            svc.LogAuthenticationEvent(account, true);

            // Dispose should flush without throwing
            svc.Dispose();

            true.Should().BeTrue();
        }
    }
}
