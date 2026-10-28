using Fluid;
using Fluid.Values;
using OrchardCore.Liquid;

namespace OrchardCore.Themes.TheAdminLiquid.Liquid;

/// <summary>
/// Appends a unique suffix to an identifier, e.g. of an element rendered several times in a page:
/// <c>{{ "pager-page-size-" | unique_id }}</c>.
/// </summary>
public sealed class UniqueIdFilter : ILiquidFilter
{
    public ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, LiquidTemplateContext context)
        => ValueTask.FromResult<FluidValue>(new StringValue(input.ToStringValue() + Guid.NewGuid().ToString("n")));
}
