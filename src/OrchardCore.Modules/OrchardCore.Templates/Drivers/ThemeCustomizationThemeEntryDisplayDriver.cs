using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Modules;
using OrchardCore.Themes.Models;

namespace OrchardCore.Templates.Drivers;

/// <summary>
/// Contributes a "Customize" action to the active, non-admin theme's card on the Themes admin
/// page. Only registered while the "OrchardCore.ThemeCustomization" feature is enabled.
/// </summary>
[Feature("OrchardCore.ThemeCustomization")]
public sealed class ThemeCustomizationThemeEntryDisplayDriver : DisplayDriver<ThemeEntry>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ThemeCustomizationThemeEntryDisplayDriver(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IDisplayResult> DisplayAsync(ThemeEntry model, BuildDisplayContext context)
    {
        if (!model.IsCurrent || model.IsAdmin)
        {
            return null;
        }

        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !await _authorizationService.AuthorizeAsync(user, ThemeCustomizationPermissions.ManageThemeCustomization))
        {
            return null;
        }

        return View("ThemeEntry_SummaryAdmin__Customize", model)
            .Location(OrchardCoreConstants.DisplayType.SummaryAdmin, "FooterEnd:5");
    }
}
