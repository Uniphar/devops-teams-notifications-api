using Microsoft.Extensions.Logging;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Logs;
using Teams.Notifications.Api.Telemetry;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Teams.Notifications.Api.Tests.Telemetry
{
    [TestClass, TestCategory("Unit")]
    public class CustomEventLogRecordProcessorTests
    {
        [TestMethod]
        public void OnEnd_ShouldSendCustomEvent_ForFileLockedIOException()
        {
            // Arrange
            var exception = new IOException("The process cannot access the file 'C:\\Temp\\test.txt' because it is being used by another process.");

            var mockTelemetryClient = new Mock<ICustomEventTelemetryClient>();
            var processor = new CustomEventLogRecordProcessor(mockTelemetryClient.Object);
            var logRecordExporter = new InMemoryLogRecordExporter();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.AddProcessor(processor);
                    options.AddProcessor(new SimpleLogRecordExportProcessor(logRecordExporter));

                });
            });
            var logger = loggerFactory.CreateLogger("test");

            // Act
            logger.LogError(exception, "Error while moving file");

            // Assert
            mockTelemetryClient.Verify(x =>
                x.TrackEvent("LockedFile",
                    It.Is<object>(logRecord => logRecord.ToString().Contains(exception.Message))), Times.Once);

            Assert.IsFalse(logRecordExporter.ExportedLogs.Any(log => log.LogLevel >= LogLevel.Error));
        }

        [TestMethod]
        public void OnEnd_ShouldNotSendCustomEvent_ForGenericException()
        {
            // Arrange
            var exception = new Exception("Some other error");

            var mockTelemetryClient = new Mock<ICustomEventTelemetryClient>();
            var processor = new CustomEventLogRecordProcessor(mockTelemetryClient.Object);
            var logRecordExporter = new InMemoryLogRecordExporter();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.AddProcessor(processor);
                    options.AddProcessor(new SimpleLogRecordExportProcessor(logRecordExporter));

                });
            });
            var logger = loggerFactory.CreateLogger("test");

            // Act
            logger.LogError(exception, "Error while moving file");

            // Assert
            mockTelemetryClient.Verify(x => x.TrackEvent(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
            Assert.IsTrue(logRecordExporter.ExportedLogs.Any(log => log.Exception == exception));
        }
    }

    public class InMemoryLogRecordExporter : BaseExporter<LogRecord>
    {
        public List<LogRecord> ExportedLogs { get; } = [];

        public override ExportResult Export(in Batch<LogRecord> batch)
        {
            foreach (var logRecord in batch)
            {
                ExportedLogs.Add(logRecord);
            }
            return ExportResult.Success;
        }
    }

}
