using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using OrchardCore.Liquid;
using OrchardCore.Themes.TheAdminLiquid.Liquid;

namespace OrchardCore.Tests.Themes.TheAdminLiquid;

public class TheAdminLiquidFilterTests
{
    [Fact]
    public async Task TagAddClass_TagBuilder_AppendsClasses()
    {
        // Arrange
        var tag = new TagBuilder("li");
        var filter = new TagAddClassFilter();

        // Act
        await filter.ProcessAsync(new ObjectValue(tag), Arguments("page-item"), CreateContext());
        await filter.ProcessAsync(new ObjectValue(tag), Arguments("active"), CreateContext());

        // Assert
        Assert.Equal("page-item active", tag.Attributes["class"]);
    }

    [Fact]
    public async Task TagAddClass_NotTagBuilder_ReturnsNil()
    {
        // Act
        var result = await new TagAddClassFilter().ProcessAsync(new StringValue("li"), Arguments("active"), CreateContext());

        // Assert
        Assert.Equal(NilValue.Instance, result);
    }

    [Fact]
    public async Task TagMergeAttribute_ExistingAttribute_KeepsValue()
    {
        // Arrange
        var tag = new TagBuilder("li");
        tag.MergeAttribute("aria-current", "step");
        var filter = new TagMergeAttributeFilter();

        // Act
        await filter.ProcessAsync(new ObjectValue(tag), Arguments("aria-current", "page"), CreateContext());
        await filter.ProcessAsync(new ObjectValue(tag), Arguments("title", "Next"), CreateContext());

        // Assert
        Assert.Equal("step", tag.Attributes["aria-current"]);
        Assert.Equal("Next", tag.Attributes["title"]);
    }

    [Fact]
    public async Task HtmlContent_String_ReturnsHtmlString()
    {
        // Act
        var result = await new HtmlContentFilter().ProcessAsync(new StringValue("<i class=\"fa\"></i>"), Arguments(), CreateContext());

        // Assert
        var html = Assert.IsType<HtmlString>(result.ToObjectValue());
        Assert.Equal("<i class=\"fa\"></i>", html.Value);
    }

    [Fact]
    public async Task UniqueId_Prefix_ReturnsDistinctIds()
    {
        // Arrange
        var filter = new UniqueIdFilter();

        // Act
        var first = (await filter.ProcessAsync(new StringValue("pager-"), Arguments(), CreateContext())).ToStringValue();
        var second = (await filter.ProcessAsync(new StringValue("pager-"), Arguments(), CreateContext())).ToStringValue();

        // Assert
        Assert.StartsWith("pager-", first);
        Assert.Equal("pager-".Length + 32, first.Length);
        Assert.NotEqual(first, second);
    }

    private static FilterArguments Arguments(params string[] values)
    {
        var arguments = new FilterArguments();

        foreach (var value in values)
        {
            arguments.Add(new StringValue(value));
        }

        return arguments;
    }

    private static LiquidTemplateContext CreateContext()
        => new(new ServiceCollection().BuildServiceProvider(), new TemplateOptions());
}
