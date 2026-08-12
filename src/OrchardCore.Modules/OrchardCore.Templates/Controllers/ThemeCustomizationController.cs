using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.Admin;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Modules;
using OrchardCore.Themes.Services;
using OrchardCore.Templates.Models;
using OrchardCore.Templates.Services;
using OrchardCore.Templates.ViewModels;

namespace OrchardCore.Templates.Controllers;

[Admin("ThemeCustomization/{action}/{id?}", "ThemeCustomization.{action}")]
[Feature("OrchardCore.ThemeCustomization")]
public sealed class ThemeCustomizationController : Controller, IAsyncAuthorizationFilter
{
    private readonly IAuthorizationService _authorizationService;
    private readonly ThemeCustomizationService _themeCustomizationService;
    private readonly ISiteThemeService _siteThemeService;
    private readonly INotifier _notifier;

    internal readonly IHtmlLocalizer H;

    public ThemeCustomizationController(
        IAuthorizationService authorizationService,
        ThemeCustomizationService themeCustomizationService,
        ISiteThemeService siteThemeService,
        IHtmlLocalizer<ThemeCustomizationController> htmlLocalizer,
        INotifier notifier)
    {
        _authorizationService = authorizationService;
        _themeCustomizationService = themeCustomizationService;
        _siteThemeService = siteThemeService;
        _notifier = notifier;
        H = htmlLocalizer;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!await _authorizationService.AuthorizeAsync(context.HttpContext.User, ThemeCustomizationPermissions.ManageThemeCustomization))
        {
            context.Result = new ForbidResult();
        }
    }

    [Admin("ThemeCustomization", "ThemeCustomization.Index")]
    public async Task<IActionResult> Index()
        => View(await BuildViewModelAsync());

    [HttpPost]
    public async Task<IActionResult> Initialize(ThemeCustomizationViewModel model)
    {
        var result = await _themeCustomizationService.InitializeAsync(model.CloneAllTemplatesOnInitialize);
        await NotifyAsync(result);

        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(nameof(Index), await BuildViewModelAsync(model.CloneAllTemplatesOnInitialize));
    }

    [HttpPost]
    public async Task<IActionResult> CloneTemplate(string templateName)
    {
        await NotifyAsync(await _themeCustomizationService.CloneTemplateAsync(templateName));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetTemplate(string templateName)
    {
        await NotifyAsync(await _themeCustomizationService.ResetTemplateAsync(templateName));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTemplate(string templateName)
    {
        await NotifyAsync(await _themeCustomizationService.DeleteTemplateAsync(templateName));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ReseedMissingTemplates()
    {
        await NotifyAsync(await _themeCustomizationService.ReseedMissingTemplatesAsync());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reset()
    {
        await NotifyAsync(await _themeCustomizationService.ResetAsync());
        return RedirectToAction(nameof(Index));
    }

    private async Task<ThemeCustomizationViewModel> BuildViewModelAsync(bool cloneAllTemplatesOnInitialize = false)
    {
        var status = await _themeCustomizationService.GetStatusAsync();
        var currentThemeId = await _siteThemeService.GetSiteThemeNameAsync();
        var currentTheme = await _themeCustomizationService.GetCurrentThemeAsync();
        var baseThemeName = status.Initialized ? await _themeCustomizationService.GetThemeDisplayNameAsync(status.BaseThemeId) : null;
        var themeTemplates = await _themeCustomizationService.GetThemeTemplatesAsync();

        return new ThemeCustomizationViewModel
        {
            CurrentThemeId = currentThemeId,
            CurrentThemeName = currentTheme?.ThemeName ?? currentThemeId,
            CurrentThemeSupportsCustomization = currentTheme != null,
            Initialized = status.Initialized,
            BaseThemeName = baseThemeName,
            BaseThemeIsActive = string.Equals(status.BaseThemeId, currentThemeId, StringComparison.OrdinalIgnoreCase),
            InitializedUtc = status.InitializedUtc,
            CloneAllTemplatesOnInitialize = cloneAllTemplatesOnInitialize,
            ThemeTemplates = themeTemplates,
        };
    }

    private async Task NotifyAsync(ThemeCustomizationOperationResult result)
    {
        var message = H[result.MessageKey, result.MessageArgs];

        if (result.Succeeded)
        {
            await _notifier.SuccessAsync(message);
            return;
        }

        await _notifier.WarningAsync(message);
    }
}
