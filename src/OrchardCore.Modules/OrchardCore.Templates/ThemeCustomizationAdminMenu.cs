using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;
using OrchardCore.Modules;

namespace OrchardCore.Templates;

[Feature("OrchardCore.ThemeCustomization")]
public sealed class ThemeCustomizationAdminMenu : AdminNavigationProvider
{
    internal readonly IStringLocalizer S;

    public ThemeCustomizationAdminMenu(IStringLocalizer<ThemeCustomizationAdminMenu> stringLocalizer)
    {
        S = stringLocalizer;
    }

    protected override ValueTask BuildAsync(NavigationBuilder builder)
    {
        builder
            .Add(S["Design"], design => design
                .Add(S["Theme Customization"], S["Theme Customization"].PrefixPosition(), item => item
                    .Action("Index", "ThemeCustomization", "OrchardCore.Templates")
                    .Permission(ThemeCustomizationPermissions.ManageThemeCustomization)
                    .LocalNav()
                )
            );

        return ValueTask.CompletedTask;
    }
}
