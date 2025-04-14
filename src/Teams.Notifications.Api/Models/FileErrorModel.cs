using System;

namespace Teams.Notifications.Api.Models
{
    public record FileErrorModel
    {
        public required string FileName { get; set; }
        public required string System { get; set; }
        public required string JobId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime? OriginalErrorTimestamp { get; set; }
        public required FileErrorStatusEnum Status { get; set; }

    }

    public enum FileErrorStatusEnum
    {
        Succes = 0, Failed = -1, InProgress = 1
    }
}
