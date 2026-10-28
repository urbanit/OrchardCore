using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Admin.Models;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Liquid;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Settings;
using OrchardCore.Themes.Services;
using OrchardCore.Themes.TheAdminLiquid.Liquid;

namespace OrchardCore.Themes.TheAdminLiquid;

public sealed class Startup : StartupBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.Configure<TemplateOptions>(options =>
        {
            options.Scope.SetValue("TheAdmin", new ObjectValue(new TheAdminLiquidAccessor()));
            options.MemberAccessStrategy.Register<TheAdminLiquidAccessor, FluidValue>(async (_, name, context) => await GetTheAdminValueAsync(name, context));

            // The admin menu items are shapes generated from a class that is a list of its items, which
            // Liquid would take for an array rather than for an object with properties.
            options.ValueConverters.Add(value => value is NavigationItemViewModel item ? new ObjectValue(item) : null);
            options.MemberAccessStrategy.Register<NavigationItemViewModel>();
            options.MemberAccessStrategy.Register<MenuItem>();

            // A notification is also the list of its properties, used as the arguments of a shape, which
            // Liquid would take for an array. Its type is rendered by its name, e.g. 'Success', as in Razor.
            options.ValueConverters.Add(value => value is NotifyEntry entry ? new ObjectValue(entry) : null);
            options.ValueConverters.Add(value => value is NotifyType type ? new StringValue(type.ToString()) : null);
            options.MemberAccessStrategy.Register<NotifyEntry>();

            options.MemberAccessStrategy.Register<SelectListItem>();
        });

        services.AddLiquidFilter<AdminMenuTextFilter>("admin_menu_text");
        services.AddLiquidFilter<TagAddClassFilter>("tag_add_class");
        services.AddLiquidFilter<TagMergeAttributeFilter>("tag_merge_attribute");
        services.AddLiquidFilter<HtmlContentFilter>("html_content");
        services.AddLiquidFilter<UniqueIdFilter>("unique_id");
    }

    private static async ValueTask<FluidValue> GetTheAdminValueAsync(string name, TemplateContext context)
    {
        var services = ((LiquidTemplateContext)context).Services;

        switch (name)
        {
            case "ThemeMode":
                return new StringValue(await services.GetRequiredService<ThemeTogglerService>().CurrentTheme());

            case "Tenant":
                return new StringValue(services.GetRequiredService<ThemeTogglerService>().CurrentTenant);

            case "AdminUrlPrefix":
                return new StringValue(services.GetRequiredService<IOptions<AdminOptions>>().Value.AdminUrlPrefix);

            case "Navbar":
                var navbar = await services.GetRequiredService<IDisplayManager<Navbar>>()
                    .BuildDisplayAsync(services.GetRequiredService<IUpdateModelAccessor>().ModelUpdater, "DetailAdmin");

                return FluidValue.Create(navbar, context.Options);

            case nameof(AdminSettings.DisplayTitlesInTopbar):
            case nameof(AdminSettings.DisplayMenuFilter):
            case nameof(AdminSettings.MenuBehavior):
                var site = await services.GetRequiredService<ISiteService>().GetSiteSettingsAsync();
                var settings = site.GetOrCreate<AdminSettings>();

                return name switch
                {
                    nameof(AdminSettings.DisplayTitlesInTopbar) => BooleanValue.Create(settings.DisplayTitlesInTopbar),
                    nameof(AdminSettings.DisplayMenuFilter) => BooleanValue.Create(settings.DisplayMenuFilter),
                    _ => new StringValue(settings.MenuBehavior == AdminMenuBehavior.Focused ? "focused" : "persistent"),
                };

            default:
                return NilValue.Instance;
        }
    }
}
