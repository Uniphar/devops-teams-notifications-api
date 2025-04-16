using System.Diagnostics;

namespace Teams.Cards.BotFramework;

public abstract record InstallationUpdateActivity : Activity
{
	public override string Type => "installationUpdate";

	[JsonInclude, DebuggerBrowsable(DebuggerBrowsableState.Never)]
	public abstract string Action { get; }
}

public sealed record InstallationAddedActivity : InstallationUpdateActivity
{
	public override string Action => "add";
}

public sealed record InstallationRemovedActivity : InstallationUpdateActivity
{
	public override string Action => "remove";
}