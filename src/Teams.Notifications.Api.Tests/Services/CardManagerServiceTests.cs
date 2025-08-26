using System.Reflection;
using AdaptiveCards;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.Configuration;
using Moq;
using Teams.Notifications.Api.Services.Interfaces;

namespace Teams.Notifications.Api.Tests.Services;

[TestClass]
[TestCategory("Unit")]
public class CardManagerServiceTests
{
    private readonly Mock<IChannelAdapter> _adapterMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ITeamsManagerService> _teamsManagerServiceMock;

    public CardManagerServiceTests()
    {
        _adapterMock = new Mock<IChannelAdapter>();
        _teamsManagerServiceMock = new Mock<ITeamsManagerService>();
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["AZURE_CLIENT_ID"]).Returns("client-id");
        _configMock.Setup(c => c["AZURE_TENANT_ID"]).Returns("tenant-id");
    }

    private CardManagerService CreateService() => new(_adapterMock.Object, _teamsManagerServiceMock.Object, _configMock.Object);

    [TestMethod]
    public async Task DeleteCard_DeletesCard_WhenIdIsFound()
    {
        // Arrange
        var service = CreateService();
        _teamsManagerServiceMock.Setup(x => x.GetTeamIdAsync("team")).ReturnsAsync("teamId");
        _teamsManagerServiceMock.Setup(x => x.CheckBotIsInTeam("teamId")).Returns(Task.CompletedTask);
        _teamsManagerServiceMock.Setup(x => x.GetChannelIdAsync("teamId", "channel")).ReturnsAsync("channelId");
        _teamsManagerServiceMock.Setup(x => x.GetMessageIdByUniqueId("teamId", "channelId", "file.json", "uid")).ReturnsAsync("msgId");
        _adapterMock
            .Setup(x => x.ContinueConversationAsync(
                It.IsAny<string>(),
                It.IsAny<ConversationReference>(),
                It.IsAny<AgentCallbackHandler>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.DeleteCard("file.json", "uid", "team", "channel");

        // Assert
        _adapterMock.Verify(x => x.ContinueConversationAsync(
                "client-id",
                It.Is<ConversationReference>(cr => cr.ActivityId == "msgId"),
                It.IsAny<AgentCallbackHandler>(),
                CancellationToken.None),
            Times.Once);
    }

    [TestMethod]
    public async Task DeleteCard_Throws_WhenIdNotFound()
    {
        // Arrange
        var service = CreateService();
        _teamsManagerServiceMock.Setup(x => x.GetTeamIdAsync(It.IsAny<string>())).ReturnsAsync("teamId");
        _teamsManagerServiceMock.Setup(x => x.CheckBotIsInTeam(It.IsAny<string>())).Returns(Task.CompletedTask);
        _teamsManagerServiceMock.Setup(x => x.GetChannelIdAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("channelId");
        _teamsManagerServiceMock.Setup(x => x.GetMessageIdByUniqueId(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((string?)null);

        // Act & Assert
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => service.DeleteCard("file.json", "uid", "team", "channel"));
    }

    [TestMethod]
    public async Task CreateOrUpdate_CreatesNewCard_WhenNoExistingId()
    {
        // Arrange
        var service = CreateService();
        var model = new BaseTemplateModel { UniqueId = "uid" };
        _teamsManagerServiceMock.Setup(x => x.GetTeamIdAsync("team")).ReturnsAsync("teamId");
        _teamsManagerServiceMock.Setup(x => x.CheckBotIsInTeam("teamId")).Returns(Task.CompletedTask);
        _teamsManagerServiceMock.Setup(x => x.GetChannelIdAsync("teamId", "channel")).ReturnsAsync("channelId");
        _teamsManagerServiceMock.Setup(x => x.GetMessageIdByUniqueId("teamId", "channelId", "file.json", "uid")).ReturnsAsync((string?)null);

        _adapterMock
            .Setup(x => x.ContinueConversationAsync(
                It.IsAny<string>(),
                It.IsAny<ConversationReference>(),
                It.IsAny<AgentCallbackHandler>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.CreateOrUpdate("WelcomeCard.json", model, "team", "channel");

        // Assert
        _adapterMock.Verify(x => x.ContinueConversationAsync(
                "client-id",
                It.IsAny<ConversationReference>(),
                It.IsAny<AgentCallbackHandler>(),
                CancellationToken.None),
            Times.Once);
    }

    [TestMethod]
    public void GetConversationReference_ReturnsExpectedReference()
    {
        // Arrange
        var service = CreateService();
        var channelId = "channelId";

        // Act
        var result = typeof(CardManagerService)
            .GetMethod("GetConversationReference", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, new object[] { channelId }) as ConversationReference;

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("msteams", result.ChannelId);
        Assert.AreEqual("https://smba.trafficmanager.net/emea/tenant-id", result.ServiceUrl);
        Assert.AreEqual(channelId, result.Conversation.Id);
        Assert.AreEqual(channelId, result.ActivityId);
    }

    [TestMethod]
    public async Task BasicCreateCardFromTemplateAsyncTest()
    {
        var model = new LogicAppErrorModel
        {
            TimeStamp = "01-01-1960",
            ObjectType = "test",
            ErrorMessage = "SomeErrorMessage",
            UniqueId = "unique"
        };
        // Arrange
        var result = await CardManagerService.CreateCardFromTemplateAsync("LogicAppError.json", model, _teamsManagerServiceMock.Object, string.Empty, string.Empty, string.Empty);
        // Assert
        Assert.IsNotEmpty(result);
        var item = AdaptiveCard.FromJson(result).Card;
        Assert.IsNotNull(item.Body);
        // 5 items should be left since the rest should be removed
        Assert.AreEqual(5, item.Body.Count);
        foreach (var element in item.Body)
            if (element is AdaptiveTextBlock textBlock)
            {
                Assert.IsFalse(textBlock.Text.Contains("{{"), "No template string should be found!");
                Assert.IsFalse(textBlock.Text.Contains("}}"), "No template string should be found!");
            }
    }
}