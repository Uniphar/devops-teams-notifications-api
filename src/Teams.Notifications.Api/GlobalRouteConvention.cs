using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace Teams.Notifications.Api;

internal sealed class GlobalRouteConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel routePrefix;

    public GlobalRouteConvention(string appPathPrefix)
    {
        ArgumentNullException.ThrowIfNull(appPathPrefix);
        routePrefix = new AttributeRouteModel(new RouteAttribute(appPathPrefix));
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var selector in application.Controllers.SelectMany(c => c.Selectors))
            selector.AttributeRouteModel = selector.AttributeRouteModel != null
                ? AttributeRouteModel.CombineAttributeRouteModel(routePrefix, selector.AttributeRouteModel)
                : routePrefix;
    }
}