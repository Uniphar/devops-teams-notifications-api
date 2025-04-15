using System;

namespace Teams.Notifications.Api.Models
{
    public record FileErrorModel
    {
        public required string FileName { get; init; }
        public required string System { get; init; }
        public required string JobId { get; init; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime? OriginalErrorTimestamp { get; set; }
        public required FileErrorStatusEnum Status { get; set; }
        public override int GetHashCode() => (System, JobId, FileName).GetHashCode();
    }

    public enum FileErrorStatusEnum
    {
        Succes = 0, Failed = -1, InProgress = 1
    }
}
