using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Html;
using OrchardCore.Liquid;

namespace OrchardCore.Themes.TheAdminLiquid.Liquid;

/// <summary>
/// Makes a string HTML content, which a shape renders without encoding it when it is one of its properties:
/// <c>{% assign icon = "&lt;i class=\"fa-solid fa-ellipsis\"&gt;&lt;/i&gt;" | html_content %}</c>.
/// </summary>
public sealed class HtmlContentFilter : ILiquidFilter
{
    public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, LiquidTemplateContext context)
        => ValueTask.FromResult<FluidValue>(new ObjectValue(new HtmlString(input.ToStringValue())));
}
