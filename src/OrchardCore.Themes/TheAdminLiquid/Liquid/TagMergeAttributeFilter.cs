using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrchardCore.Liquid;

namespace OrchardCore.Themes.TheAdminLiquid.Liquid;

/// <summary>
/// Adds an attribute to the tag a shape is rendered in by its parent, unless it has one:
/// <c>{% assign _ = Model.Tag | tag_merge_attribute: "aria-current", "page" %}</c>.
/// </summary>
public sealed class TagMergeAttributeFilter : ILiquidFilter
{
    public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, LiquidTemplateContext context)
    {
        if (input.ToObjectValue() is TagBuilder tag)
        {
            tag.MergeAttribute(arguments.At(0).ToStringValue(), arguments.At(1).ToStringValue());
        }

        return ValueTask.FromResult<FluidValue>(NilValue.Instance);
    }
}
