using System;
using Xunit;

namespace Microsoft.AspNet.SignalR.Redis.Tests
{
    public class RedisScaleoutConfigurationFacts
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("connection", null)]
        [InlineData("connection", "")]
        [InlineData(null, "event")]
        [InlineData("", "event")]
        public void ValidateArguments(string connectionString, string eventKey)
        {
            Assert.Throws<ArgumentNullException>(() => new RedisScaleoutConfiguration(connectionString, eventKey));
        }
    }
}