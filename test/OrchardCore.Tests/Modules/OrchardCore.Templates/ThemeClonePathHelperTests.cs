using OrchardCore.Templates.Services;

namespace OrchardCore.Tests.Modules.OrchardCore.Templates;

public class ThemeClonePathHelperTests
{
    [Theory]
    [InlineData("Views/Layout.liquid", "layout")]
    [InlineData("Views/Content-BlogPost.Summary.liquid", "content_summary__blogpost")]
    [InlineData("Views/MenuItemLink-ContentMenuItem.liquid", "menuitemlink__contentmenuitem")]
    [InlineData("Views/Parts/Foo-Bar.liquid", "parts_foo__bar")]
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
