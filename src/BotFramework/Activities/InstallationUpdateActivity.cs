using System.Diagnostics;

namespace Teams.Cards.BotFramework.Activities;

public abstract record InstallationUpdateActivity : Activity
{
	internal override string Type => "installationUpdate";

	[JsonInclude, DebuggerBrowsable(DebuggerBrowsableState.Never)]
	internal abstract string Action { get; }
}

public sealed record InstallationAddedActivity : InstallationUpdateActivity
{
	internal override string Action => "add";
}

public sealed record InstallationRemovedActivity : InstallationUpdateActivity
{
	internal override string Action => "remove";
}