using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrchardCore.Liquid;

namespace OrchardCore.Themes.TheAdminLiquid.Liquid;

/// <summary>
/// Adds a class to the tag a shape is rendered in by its parent, e.g. the item of a pager:
/// <c>{% assign _ = Model.Tag | tag_add_class: "active" %}</c>.
/// </summary>
public sealed class TagAddClassFilter : ILiquidFilter
{
    public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, LiquidTemplateContext context)
    {
        if (input.ToObjectValue() is TagBuilder tag)
        {
            tag.AddCssClass(arguments.At(0).ToStringValue());
        }

        return ValueTask.FromResult<FluidValue>(NilValue.Instance);
    }
}
