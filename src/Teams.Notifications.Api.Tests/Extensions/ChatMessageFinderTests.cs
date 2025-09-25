namespace Teams.Notifications.Api.Tests.Extensions;

[TestClass]
[TestCategory("Unit")]
public class ChatMessageFinderTests
{
    [TestMethod]
    public void GetMessageThatHas_ReturnsFalse_WhenDeleted()
    {
        var msg = new ChatMessage { DeletedDateTime = DateTimeOffset.UtcNow };
        var result = msg.GetCardThatHas("file.json", "uid");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetMessageThatHas_ReturnsFalse_WhenAttachmentsNull()
    {
        var msg = new ChatMessage { Attachments = null };
        var result = msg.GetCardThatHas("file.json", "uid");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetMessageThatHas_ReturnsFalse_WhenAttachmentsEmpty()
    {
        var msg = new ChatMessage
        {
            Attachments = []
        };
        var result = msg.GetCardThatHas("file.json", "uid");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetMessageThatHas_ReturnsFalse_WhenAnyAttachmentContentNull()
    {
        var msg = new ChatMessage
        {
            Attachments = [new ChatMessageAttachment { Content = null }]
        };
        var result = msg.GetCardThatHas("file.json", "uid");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetMessageThatHas_ReturnsFalse_WhenNoMatchingAdaptiveTextBlock()
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = [new AdaptiveTextBlock { Text = " " }]
        };
        var msg = new ChatMessage
        {
            Attachments = [new ChatMessageAttachment { Content = card.ToJson() }]
        };

        var result = msg.GetCardThatHas("file.json", "uid");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetMessageThatHas_ReturnsTrue_WhenMatchingAdaptiveTextBlock()
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = [new AdaptiveTextBlock { Id = "uid", Text = "file.json" }]
        };
        var msg = new ChatMessage
        {
            Attachments = [new ChatMessageAttachment { Content = card.ToJson() }]
        };
        var result = msg.GetCardThatHas("file.json", "uid");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void GetMessageThatHas_ReturnsFalse_WhenMatchingAdaptiveTextBlockButNotFileName()
    {
        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body = [new AdaptiveTextBlock { Id = "uid", Text = "file2.json" }]
        };
        var msg = new ChatMessage
        {
            Attachments = [new ChatMessageAttachment { Content = card.ToJson() }]
        };
        var result = msg.GetCardThatHas("file.json", "uid");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetMessageThatHas_ReturnsFalse_WhenContentIsRandom()
    {
        var msg = new ChatMessage
        {
            Attachments = [new ChatMessageAttachment { Content = "  " }]
        };

        var result = msg.GetCardThatHas("file.json", "uid");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetMessageThatHas_SkipsAttachmentsWithWhitespaceContent()
    {
        var msg = new ChatMessage
        {
            Attachments = [new ChatMessageAttachment { Content = " " }]
        };
        var result = msg.GetCardThatHas("file.json", "uid");
        Assert.IsNull(result);
    }
}