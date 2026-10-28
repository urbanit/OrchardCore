using Fluid;
using Fluid.Values;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.AdminMenu;
using OrchardCore.DisplayManagement;
using OrchardCore.Liquid;
using OrchardCore.Localization.Data;
using OrchardCore.Navigation;

namespace OrchardCore.Themes.TheAdminLiquid.Liquid;

/// <summary>
/// Gets the text of an admin menu item, translated with the data localization of its menu:
/// <c>{{ Model | admin_menu_text }}</c>.
/// </summary>
public sealed class AdminMenuTextFilter : ILiquidFilter
{
    public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, LiquidTemplateContext context)
    {
        string text;
        string menuName;

        switch (input.ToObjectValue())
        {
            case NavigationItemViewModel item:
                text = item.Text;
                menuName = item.Item?.MenuName;
                break;

            // A shape that is not a navigation item, e.g. created by a module, holds the same properties.
            case IShape shape:
                text = shape.TryGetProperty("Text", out object value) ? value?.ToString() : null;
                menuName = shape.TryGetProperty("Item", out MenuItem menuItem) ? menuItem?.MenuName : null;
                break;

            default:
                return ValueTask.FromResult<FluidValue>(NilValue.Instance);
        }

        if (text is null)
        {
            return ValueTask.FromResult<FluidValue>(NilValue.Instance);
        }

        var dataLocalizer = context.Services.GetRequiredService<IDataLocalizer>();

        return ValueTask.FromResult<FluidValue>(new StringValue(dataLocalizer[text, DataLocalizationContext.AdminMenu(menuName)]));
    }
}
