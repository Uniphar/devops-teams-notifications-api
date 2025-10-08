namespace Teams.Notifications.Api.Models;

public sealed record LogicAppFrontgateFileInformation
{
    public required string file_name { get; init; }

    public string storage_type => "SharePoint";

    public required string storage_reference { get; init; }

    public required string initial_display_name { get; init; }

    public required string storage_folder { get; init; }
}