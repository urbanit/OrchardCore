using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
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
public sealed class ThemeCustomizationController : Controller
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

    [Admin("ThemeCustomization", "ThemeCustomization.Index")]
    public async Task<IActionResult> Index()
    {
        if (!await _authorizationService.AuthorizeAsync(User, ThemeCustomizationPermissions.ManageThemeCustomization))
        {
            return Forbid();
        }

        return View(await BuildViewModelAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Initialize(ThemeCustomizationViewModel model)
    {
        if (!await _authorizationService.AuthorizeAsync(User, ThemeCustomizationPermissions.ManageThemeCustomization))
        {
            return Forbid();
        }

        var result = await _themeCustomizationService.InitializeAsync(model.BaseThemeId, model.CloneAllTemplatesOnInitialize);
        await NotifyAsync(result);

        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(nameof(Index), await BuildViewModelAsync(model.BaseThemeId, model.CloneAllTemplatesOnInitialize));
    }

    [HttpPost]
    public async Task<IActionResult> CloneTemplate(string templateName)
    {
        if (!await _authorizationService.AuthorizeAsync(User, ThemeCustomizationPermissions.ManageThemeCustomization))
        {
            return Forbid();
        }

        await NotifyAsync(await _themeCustomizationService.CloneTemplateAsync(templateName));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetTemplate(string templateName)
    {
        if (!await _authorizationService.AuthorizeAsync(User, ThemeCustomizationPermissions.ManageThemeCustomization))
        {
            return Forbid();
        }

        await NotifyAsync(await _themeCustomizationService.ResetTemplateAsync(templateName));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTemplate(string templateName)
    {
        if (!await _authorizationService.AuthorizeAsync(User, ThemeCustomizationPermissions.ManageThemeCustomization))
        {
            return Forbid();
        }

        await NotifyAsync(await _themeCustomizationService.DeleteTemplateAsync(templateName));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ReseedMissingTemplates()
    {
        if (!await _authorizationService.AuthorizeAsync(User, ThemeCustomizationPermissions.ManageThemeCustomization))
        {
            return Forbid();
        }

        await NotifyAsync(await _themeCustomizationService.ReseedMissingTemplatesAsync());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reset()
    {
        if (!await _authorizationService.AuthorizeAsync(User, ThemeCustomizationPermissions.ManageThemeCustomization))
        {
            return Forbid();
        }

        await NotifyAsync(await _themeCustomizationService.ResetAsync());
        return RedirectToAction(nameof(Index));
    }

    private async Task<ThemeCustomizationViewModel> BuildViewModelAsync(string selectedBaseThemeId = null, bool cloneAllTemplatesOnInitialize = false)
    {
        var status = await _themeCustomizationService.GetStatusAsync();
        var availableThemes = await _themeCustomizationService.GetAvailableThemesAsync();
        var currentThemeId = await _siteThemeService.GetSiteThemeNameAsync();
        var baseThemeId = selectedBaseThemeId ?? status.BaseThemeId;
        var currentTheme = availableThemes.FirstOrDefault(x => string.Equals(x.ThemeId, currentThemeId, StringComparison.OrdinalIgnoreCase));
        var baseTheme = availableThemes.FirstOrDefault(x => string.Equals(x.ThemeId, baseThemeId, StringComparison.OrdinalIgnoreCase));
        var themeTemplates = await _themeCustomizationService.GetThemeTemplatesAsync(baseThemeId);

        return new ThemeCustomizationViewModel
        {
            CurrentThemeId = currentThemeId,
            CurrentThemeName = currentTheme?.ThemeName ?? currentThemeId,
            Initialized = status.Initialized,
            BaseThemeId = baseThemeId,
            BaseThemeName = baseTheme?.ThemeName ?? status.BaseThemeId,
            InitializedUtc = status.InitializedUtc,
            CloneAllTemplatesOnInitialize = cloneAllTemplatesOnInitialize,
            ThemeTemplates = themeTemplates,
            AvailableThemes = availableThemes.Select(x => new SelectListItem($"{x.ThemeName} ({x.CloneableTemplateCount})", x.ThemeId, string.Equals(x.ThemeId, baseThemeId, StringComparison.OrdinalIgnoreCase))).ToArray(),
        };
    }

    private async Task NotifyAsync(ThemeCustomizationOperationResult result)
    {
        if (result.Succeeded)
        {
            await _notifier.SuccessAsync(H[result.Message]);
            return;
        }

        await _notifier.WarningAsync(H[result.Message]);
    }
}
