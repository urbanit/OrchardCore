using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Liquid;
using TheAdminLiquidStartup = OrchardCore.Themes.TheAdminLiquid.Startup;

namespace OrchardCore.Tests.Themes.TheAdminLiquid;

public class TheAdminLiquidTemplateOptionsTests
{
    [Fact]
    public async Task NotifyEntry_RenderedInTemplate_ExposesTypeAndMessage()
    {
        // Arrange
        var services = new ServiceCollection();
        new TheAdminLiquidStartup().ConfigureServices(services);
        var options = services.BuildServiceProvider().GetRequiredService<IOptions<TemplateOptions>>().Value;

        var entry = new NotifyEntry
        {
            Type = NotifyType.Success,
            Message = new HtmlString("Saved"),
            DismissalMilliseconds = 5000,
        };

        var context = new LiquidTemplateContext(new ServiceCollection().BuildServiceProvider(), options);
        context.SetValue("entry", entry);

        Assert.True(new FluidParser().TryParse("{{ entry.Type }}|{{ entry.Message }}|{{ entry.DismissalMilliseconds }}", out var template, out var error), error);

        // Act
        var result = await template.RenderAsync(context);

        // Assert
        Assert.Equal("Success|Saved|5000", result);
    }
}
