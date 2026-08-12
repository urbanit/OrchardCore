using OrchardCore.Templates.Services;

namespace OrchardCore.Tests.Modules.OrchardCore.Templates;

public class ThemeClonePathHelperTests
{
    [Theory]
    [InlineData("Views/Layout.liquid", "Layout")]
    [InlineData("Views/Content-BlogPost.Summary.liquid", "Content_Summary__BlogPost")]
    [InlineData("Views/MenuItemLink-ContentMenuItem.liquid", "MenuItemLink__ContentMenuItem")]
    [InlineData("Views/Parts/Foo-Bar.liquid", "Parts_Foo__Bar")]
    public void TryMapLiquidTemplate_MapsSupportedPaths(string relativePath, string expectedTemplateName)
    {
        Assert.True(ThemeClonePathHelper.TryMapLiquidTemplate(relativePath, out var templateName));
        Assert.Equal(expectedTemplateName, templateName);
    }

    [Theory]
    [InlineData("Views/Layout.cshtml")]
    [InlineData("Recipes/Snippets/Content__ComingSoon.liquid")]
    [InlineData("wwwroot/site.css")]
    public void TryMapLiquidTemplate_RejectsUnsupportedPaths(string relativePath)
    {
        Assert.False(ThemeClonePathHelper.TryMapLiquidTemplate(relativePath, out _));
    }
}
