using Azure.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Teams.Notifications.Api.Tests.Services
{
    [TestClass]
    [TestCategory("Integration")]
    public sealed class TeamsChannelManagingServiceTests
    {
        private static DefaultAzureCredential _defaultAzureCredential;
        private static CancellationToken _cancellationToken;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            var env = context.Properties["Environment"]!.ToString();
            _cancellationToken = context.CancellationTokenSource.Token;
            _defaultAzureCredential = new DefaultAzureCredential();
        }
    }
}
