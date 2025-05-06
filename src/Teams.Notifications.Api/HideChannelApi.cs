using Microsoft.AspNetCore.Mvc.Routing;

namespace Teams.Notifications.Api;

internal sealed class HideChannelApi : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        // Channel api is exposed from the agent, but we don't want it to be exposed on swagger
        if (controller.ControllerName == "ChannelApi") controller.ApiExplorer.IsVisible = false;
    }
}