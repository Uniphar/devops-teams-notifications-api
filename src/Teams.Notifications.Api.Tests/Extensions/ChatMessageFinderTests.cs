using Microsoft.Graph.Beta.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teams.Notifications.Api.Extensions;
using Teams.Notifications.Api.Models;

namespace Teams.Notifications.Api.Tests.Extensions;

[TestClass]
[TestCategory("Unit")]
public class ChatMessageFinderTests
{
    [TestMethod]
    public void BasicTrueTest()
    {
        var fileError = new FileErrorModel
        {
            FileName = "Test.txt",
            System = "FrontGateExample",
            JobId = "file-moving-example",
            Status = FileErrorStatusEnum.Failed
        };
        var chat = new ChatMessage
        {
            Attachments =
            [
                new ChatMessageAttachment
                {
                    Content = "{\r\n  \"type\": \"AdaptiveCard\",\r\n  \"body\": [\r\n    {\r\n      \"color\": \"warning\",\r\n      \"size\": \"large\",\r\n      \"text\": \"🚨 File Processing Error\",\r\n      \"weight\": \"bolder\",\r\n      \"type\": \"TextBlock\"\r\n    },\r\n    {\r\n      \"text\": \"There was an issue processing the file.\",\r\n      \"wrap\": true,\r\n      \"type\": \"TextBlock\"\r\n    },\r\n    {\r\n      \"facts\": [\r\n        {\r\n          \"title\": \"File:\",\r\n          \"value\": \"Test.txt\"\r\n        },\r\n        {\r\n          \"title\": \"System:\",\r\n          \"value\": \"FrontGateExample\"\r\n        },\r\n        {\r\n          \"title\": \"Job ID:\",\r\n          \"value\": \"file-moving-example\"\r\n        },\r\n        {\r\n          \"title\": \"Status:\",\r\n          \"value\": \"❌ Failed\"\r\n        },\r\n        {\r\n          \"title\": \"Timestamp:\",\r\n          \"value\": \"04/18/2025 09:47:05\"\r\n        }\r\n      ],\r\n      \"type\": \"FactSet\"\r\n    }\r\n  ],\r\n  \"actions\": [\r\n    {\r\n      \"verb\": \"process\",\r\n      \"data\": {\r\n        \"action\": \"reprocessFile\"\r\n      },\r\n      \"title\": \"🔁 Reprocess File\",\r\n      \"id\": \"process\",\r\n      \"type\": \"Action.Execute\"\r\n    },\r\n    {\r\n      \"url\": \"https://teams.microsoft.com/file/Report_Q1_2025.xlsx\",\r\n      \"title\": \"📂 Open file in Teams\",\r\n      \"type\": \"Action.OpenUrl\"\r\n    },\r\n    {\r\n      \"card\": {\r\n        \"type\": \"AdaptiveCard\",\r\n        \"body\": [\r\n          {\r\n            \"text\": \"Stack Trace\",\r\n            \"weight\": \"bolder\",\r\n            \"spacing\": \"small\",\r\n            \"type\": \"TextBlock\"\r\n          },\r\n          {\r\n            \"color\": \"attention\",\r\n            \"text\": \"System.NullReferenceException: Object reference not set to an instance of an object.\\n   at FrontGate.Processing.FileHandler.Process(String filePath)\\n   at FrontGate.JobRunner.Run(Job job)\\n   at FrontGate.MainController.Execute()\",\r\n            \"wrap\": true,\r\n            \"fontType\": \"monospace\",\r\n            \"type\": \"TextBlock\"\r\n          }\r\n        ],\r\n        \"version\": \"1.5\"\r\n      },\r\n      \"title\": \"🔍 View Error Details\",\r\n      \"type\": \"Action.ShowCard\"\r\n    }\r\n  ],\r\n  \"version\": \"1.5\"\r\n}"
                }
            ]
        };


        Assert.IsTrue(chat.GetMessageThatHas(fileError));
    }

    [TestMethod]
    public void BasicFalseTest()
    {
        var fileError = new FileErrorModel
        {
            FileName = "Test.txt",
            System = "FrontGateExample",
            JobId = "file-moving-example",
            Status = FileErrorStatusEnum.Failed
        };
        var chatDeleted = new ChatMessage
        {
            DeletedDateTime = new DateTimeOffset()
        };
        var chatAttachmentNull = new ChatMessage
        {
            Attachments = null
        };
        var chatContentNull = new ChatMessage
        {
            Attachments = [new ChatMessageAttachment { Content = null }]
        };
        var chatContentNotRight = new ChatMessage
        {
            Attachments = [new ChatMessageAttachment { Content = "Test stuff something" }]
        };

        Assert.IsFalse(chatDeleted.GetMessageThatHas(fileError));
        Assert.IsFalse(chatAttachmentNull.GetMessageThatHas(fileError));
        Assert.IsFalse(chatContentNull.GetMessageThatHas(fileError));
        Assert.IsFalse(chatContentNotRight.GetMessageThatHas(fileError));
    }
}