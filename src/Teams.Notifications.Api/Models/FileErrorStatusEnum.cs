namespace Teams.Notifications.Api.Models;

/// <summary>
///     Status of the error
/// </summary>
public enum FileErrorStatusEnum
{
    /// <summary>
    ///     Success
    /// </summary>
    Success = 0,

    /// <summary>
    ///     Failed
    /// </summary>
    Failed = -1,

    /// <summary>
    ///     In progress
    /// </summary>
    InProgress = 1,

    /// <summary>
    ///     System has been notified, we are on it!
    /// </summary>
    SystemNotified = 2
}