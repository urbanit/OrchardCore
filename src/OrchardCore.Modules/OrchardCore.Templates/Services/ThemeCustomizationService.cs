using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Extensions;
using OrchardCore.Environment.Extensions.Features;
using OrchardCore.Environment.Shell;
using OrchardCore.Modules;
using OrchardCore.Themes.Services;
using OrchardCore.Templates.Models;

namespace OrchardCore.Templates.Services;

[Feature("OrchardCore.ThemeCustomization")]
public class ThemeCustomizationService
{
    private static readonly StringComparer TemplateNameComparer = StringComparer.OrdinalIgnoreCase;

    private readonly IApplicationContext _applicationContext;
    private readonly IShellFeaturesManager _shellFeaturesManager;
    private readonly TemplatesManager _templatesManager;
    private readonly ThemeCustomizationManager _themeCustomizationManager;
    private readonly ISiteThemeService _siteThemeService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ThemeCustomizationService(
        IApplicationContext applicationContext,
        IShellFeaturesManager shellFeaturesManager,
        TemplatesManager templatesManager,
        ThemeCustomizationManager themeCustomizationManager,
        ISiteThemeService siteThemeService,
        IHttpContextAccessor httpContextAccessor)
    {
        _applicationContext = applicationContext;
        _shellFeaturesManager = shellFeaturesManager;
        _templatesManager = templatesManager;
        _themeCustomizationManager = themeCustomizationManager;
        _siteThemeService = siteThemeService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<ThemeCustomizationDocument> GetStatusAsync() => _themeCustomizationManager.GetDocumentAsync();

    public async Task<IReadOnlyList<ThemeCustomizationThemeEntry>> GetAvailableThemesAsync()
    {
        var currentThemeId = await _siteThemeService.GetSiteThemeNameAsync();
        var features = await _shellFeaturesManager.GetAvailableFeaturesAsync();
        var modules = _applicationContext.Application;
        var results = new List<ThemeCustomizationThemeEntry>();

        foreach (var feature in features.Where(IsSupportedTheme).OrderBy(x => x.Name ?? x.Id))
        {
            var module = modules.GetModule(feature.Extension.Id);
            var cloneCandidates = GetCloneCandidates(module);

            if (cloneCandidates.Count == 0)
            {
                continue;
            }

            results.Add(new ThemeCustomizationThemeEntry
            {
                ThemeId = feature.Id,
                ThemeName = feature.Name ?? feature.Id,
                CloneableTemplateCount = cloneCandidates.Count,
                IsCurrent = string.Equals(feature.Id, currentThemeId, StringComparison.OrdinalIgnoreCase),
            });
        }

        return results;
    }

    public async Task<IReadOnlyList<ThemeCustomizationTemplateEntry>> GetThemeTemplatesAsync(string baseThemeId)
    {
        if (string.IsNullOrWhiteSpace(baseThemeId))
        {
            return [];
        }

        var theme = await GetAvailableThemeAsync(baseThemeId);
        if (theme == null)
        {
            return [];
        }

        var templatesDocument = await _templatesManager.GetTemplatesDocumentAsync();
        var status = await _themeCustomizationManager.GetDocumentAsync();
        var trackedTemplates = string.Equals(status.BaseThemeId, baseThemeId, StringComparison.OrdinalIgnoreCase)
            ? status.ClonedTemplates
            : [];

        return GetCloneCandidates(_applicationContext.Application.GetModule(baseThemeId))
            .Select(candidate => new ThemeCustomizationTemplateEntry
            {
                TemplateName = candidate.TemplateName,
                RelativePath = candidate.RelativePath,
                HasLocalCustomization = templatesDocument.Templates.ContainsKey(candidate.TemplateName),
                IsTrackedCustomization = trackedTemplates.Contains(candidate.TemplateName, TemplateNameComparer),
            })
            .OrderBy(x => x.TemplateName)
            .ToArray();
    }

    public async Task<ThemeCustomizationOperationResult> InitializeAsync(string baseThemeId, bool cloneAllTemplates)
    {
        var document = await _themeCustomizationManager.LoadDocumentAsync();
        if (document.Initialized)
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "Theme customization has already been initialized.",
            };
        }

        var theme = await GetAvailableThemeAsync(baseThemeId);
        if (theme == null)
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "The selected theme does not support Liquid template cloning.",
            };
        }

        var currentThemeId = await _siteThemeService.GetSiteThemeNameAsync();
        if (!string.Equals(theme.ThemeId, currentThemeId, StringComparison.OrdinalIgnoreCase))
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "The selected base theme must be the current site theme.",
            };
        }

        var module = _applicationContext.Application.GetModule(baseThemeId);
        var cloneCandidates = GetCloneCandidates(module);
        if (cloneCandidates.Count == 0)
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "No supported Liquid templates were found for the selected theme.",
            };
        }

        var clonedTemplateNames = new List<string>();
        var existingLocalCustomizationCount = 0;

        if (cloneAllTemplates)
        {
            var templatesDocument = await _templatesManager.LoadTemplatesDocumentAsync();

            foreach (var candidate in cloneCandidates)
            {
                if (templatesDocument.Templates.ContainsKey(candidate.TemplateName))
                {
                    existingLocalCustomizationCount++;
                    continue;
                }

                templatesDocument.Templates[candidate.TemplateName] = CreateTemplate(candidate, theme.ThemeName);
                clonedTemplateNames.Add(candidate.TemplateName);
            }

            if (clonedTemplateNames.Count > 0)
            {
                await _templatesManager.UpdateTemplatesDocumentAsync(templatesDocument);
            }
        }

        document.Initialized = true;
        document.BaseThemeId = theme.ThemeId;
        document.InitializedUtc = DateTime.UtcNow;
        document.InitializedByUserId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        document.ClonedTemplates.Clear();
        document.ClonedTemplates.AddRange(clonedTemplateNames.OrderBy(x => x));
        await _themeCustomizationManager.UpdateAsync(document);

        return new ThemeCustomizationOperationResult
        {
            Succeeded = true,
            TemplatesAffected = clonedTemplateNames.Count,
            Message = BuildInitializationMessage(theme.ThemeName, cloneAllTemplates, clonedTemplateNames.Count, existingLocalCustomizationCount),
        };
    }

    public async Task<ThemeCustomizationOperationResult> CloneTemplateAsync(string templateName)
    {
        var (document, theme, candidate) = await GetCurrentThemeTemplateContextAsync(templateName);
        if (!document.Initialized || string.IsNullOrWhiteSpace(document.BaseThemeId))
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "Theme customization has not been initialized yet.",
            };
        }

        if (theme == null || candidate == null)
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "The selected theme template could not be found.",
            };
        }

        var templatesDocument = await _templatesManager.LoadTemplatesDocumentAsync();
        if (templatesDocument.Templates.ContainsKey(candidate.TemplateName))
        {
            return new ThemeCustomizationOperationResult
            {
                Message = $"'{candidate.TemplateName}' already has a local customization.",
            };
        }

        templatesDocument.Templates[candidate.TemplateName] = CreateTemplate(candidate, theme.ThemeName);
        await _templatesManager.UpdateTemplatesDocumentAsync(templatesDocument);

        if (!document.ClonedTemplates.Contains(candidate.TemplateName, TemplateNameComparer))
        {
            document.ClonedTemplates.Add(candidate.TemplateName);
            document.ClonedTemplates.Sort(TemplateNameComparer);
            await _themeCustomizationManager.UpdateAsync(document);
        }

        return new ThemeCustomizationOperationResult
        {
            Succeeded = true,
            TemplatesAffected = 1,
            Message = $"'{candidate.TemplateName}' was cloned from '{theme.ThemeName}'.",
        };
    }

    public async Task<ThemeCustomizationOperationResult> ResetTemplateAsync(string templateName)
    {
        var (document, theme, candidate) = await GetCurrentThemeTemplateContextAsync(templateName);
        if (!document.Initialized || string.IsNullOrWhiteSpace(document.BaseThemeId))
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "Theme customization has not been initialized yet.",
            };
        }

        if (theme == null || candidate == null)
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "The selected theme template could not be found.",
            };
        }

        if (!document.ClonedTemplates.Contains(candidate.TemplateName, TemplateNameComparer))
        {
            return new ThemeCustomizationOperationResult
            {
                Message = $"'{candidate.TemplateName}' is not managed by theme customization and cannot be reset.",
            };
        }

        var templatesDocument = await _templatesManager.LoadTemplatesDocumentAsync();
        if (!templatesDocument.Templates.ContainsKey(candidate.TemplateName))
        {
            return new ThemeCustomizationOperationResult
            {
                Message = $"'{candidate.TemplateName}' does not have a local customization to reset.",
            };
        }

        templatesDocument.Templates[candidate.TemplateName] = CreateTemplate(candidate, theme.ThemeName);
        await _templatesManager.UpdateTemplatesDocumentAsync(templatesDocument);

        if (!document.ClonedTemplates.Contains(candidate.TemplateName, TemplateNameComparer))
        {
            document.ClonedTemplates.Add(candidate.TemplateName);
            document.ClonedTemplates.Sort(TemplateNameComparer);
            await _themeCustomizationManager.UpdateAsync(document);
        }

        return new ThemeCustomizationOperationResult
        {
            Succeeded = true,
            TemplatesAffected = 1,
            Message = $"'{candidate.TemplateName}' was reset to the packaged theme version.",
        };
    }

    public async Task<ThemeCustomizationOperationResult> DeleteTemplateAsync(string templateName)
    {
        var (document, _, candidate) = await GetCurrentThemeTemplateContextAsync(templateName);
        if (!document.Initialized || string.IsNullOrWhiteSpace(document.BaseThemeId))
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "Theme customization has not been initialized yet.",
            };
        }

        if (candidate == null)
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "The selected theme template could not be found.",
            };
        }

        if (!document.ClonedTemplates.Contains(candidate.TemplateName, TemplateNameComparer))
        {
            return new ThemeCustomizationOperationResult
            {
                Message = $"'{candidate.TemplateName}' is not managed by theme customization and cannot be deleted.",
            };
        }

        var templatesDocument = await _templatesManager.LoadTemplatesDocumentAsync();
        if (!templatesDocument.Templates.Remove(candidate.TemplateName))
        {
            return new ThemeCustomizationOperationResult
            {
                Message = $"'{candidate.TemplateName}' does not have a local customization to delete.",
            };
        }

        await _templatesManager.UpdateTemplatesDocumentAsync(templatesDocument);

        if (document.ClonedTemplates.RemoveAll(x => string.Equals(x, candidate.TemplateName, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            await _themeCustomizationManager.UpdateAsync(document);
        }

        return new ThemeCustomizationOperationResult
        {
            Succeeded = true,
            TemplatesAffected = 1,
            Message = $"'{candidate.TemplateName}' local customization was deleted.",
        };
    }

    public async Task<ThemeCustomizationOperationResult> ReseedMissingTemplatesAsync()
    {
        var document = await _themeCustomizationManager.LoadDocumentAsync();
        if (!document.Initialized || string.IsNullOrWhiteSpace(document.BaseThemeId))
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "Theme customization has not been initialized yet.",
            };
        }

        var theme = await GetAvailableThemeAsync(document.BaseThemeId);
        if (theme == null)
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "The selected theme does not support Liquid template cloning.",
            };
        }

        var module = _applicationContext.Application.GetModule(document.BaseThemeId);
        var cloneCandidates = GetCloneCandidates(module);
        var templatesDocument = await _templatesManager.LoadTemplatesDocumentAsync();

        var addedCount = 0;
        foreach (var candidate in cloneCandidates)
        {
            if (templatesDocument.Templates.ContainsKey(candidate.TemplateName))
            {
                continue;
            }

            templatesDocument.Templates[candidate.TemplateName] = CreateTemplate(candidate, theme.ThemeName);

            if (!document.ClonedTemplates.Contains(candidate.TemplateName, TemplateNameComparer))
            {
                document.ClonedTemplates.Add(candidate.TemplateName);
            }

            addedCount++;
        }

        if (addedCount == 0)
        {
            return new ThemeCustomizationOperationResult
            {
                Succeeded = true,
                Message = "All theme templates already have local customizations.",
            };
        }

        await _templatesManager.UpdateTemplatesDocumentAsync(templatesDocument);
        document.ClonedTemplates.Sort(TemplateNameComparer);
        await _themeCustomizationManager.UpdateAsync(document);

        return new ThemeCustomizationOperationResult
        {
            Succeeded = true,
            TemplatesAffected = addedCount,
            Message = "Missing theme templates were cloned.",
        };
    }

    public async Task<ThemeCustomizationOperationResult> ResetAsync()
    {
        var document = await _themeCustomizationManager.LoadDocumentAsync();
        if (!document.Initialized)
        {
            return new ThemeCustomizationOperationResult
            {
                Message = "Theme customization has not been initialized yet.",
            };
        }

        var templatesDocument = await _templatesManager.LoadTemplatesDocumentAsync();
        var removedCount = 0;

        foreach (var templateName in document.ClonedTemplates)
        {
            if (templatesDocument.Templates.Remove(templateName))
            {
                removedCount++;
            }
        }

        await _templatesManager.UpdateTemplatesDocumentAsync(templatesDocument);

        document.Initialized = false;
        document.BaseThemeId = null;
        document.InitializedUtc = null;
        document.InitializedByUserId = null;
        document.ClonedTemplates.Clear();
        await _themeCustomizationManager.UpdateAsync(document);

        return new ThemeCustomizationOperationResult
        {
            Succeeded = true,
            TemplatesAffected = removedCount,
            Message = "Tracked theme customizations were reset.",
        };
    }

    private async Task<(ThemeCustomizationDocument Document, ThemeCustomizationThemeEntry Theme, ThemeCloneTemplateEntry Candidate)> GetCurrentThemeTemplateContextAsync(string templateName)
    {
        var document = await _themeCustomizationManager.LoadDocumentAsync();
        if (!document.Initialized || string.IsNullOrWhiteSpace(document.BaseThemeId))
        {
            return (document, null, null);
        }

        var theme = await GetAvailableThemeAsync(document.BaseThemeId);
        if (theme == null)
        {
            return (document, null, null);
        }

        var candidate = GetCloneCandidates(_applicationContext.Application.GetModule(document.BaseThemeId))
            .FirstOrDefault(x => string.Equals(x.TemplateName, templateName, StringComparison.OrdinalIgnoreCase));

        return (document, theme, candidate);
    }

    private async Task<ThemeCustomizationThemeEntry> GetAvailableThemeAsync(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId))
        {
            return null;
        }

        return (await GetAvailableThemesAsync()).FirstOrDefault(x => string.Equals(x.ThemeId, themeId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSupportedTheme(IFeatureInfo feature)
    {
        if (feature.IsAlwaysEnabled || feature.EnabledByDependencyOnly || !feature.IsTheme())
        {
            return false;
        }

        var tags = feature.Extension.Manifest.Tags.ToArray();
        var isHidden = tags.Any(t => string.Equals(t, "hidden", StringComparison.OrdinalIgnoreCase));
        var isAdmin = tags.Any(t => string.Equals(t, OrchardCore.Modules.Manifest.ManifestConstants.AdminTag, StringComparison.OrdinalIgnoreCase));

        return !isHidden && !isAdmin;
    }

    private static List<ThemeCloneTemplateEntry> GetCloneCandidates(Module module)
    {
        var results = new Dictionary<string, ThemeCloneTemplateEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var asset in module.Assets)
        {
            if (string.IsNullOrWhiteSpace(asset.ModuleAssetPath) ||
                !asset.ModuleAssetPath.StartsWith(module.Root, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativePath = asset.ModuleAssetPath[module.Root.Length..];
            if (!ThemeClonePathHelper.TryMapLiquidTemplate(relativePath, out var templateName))
            {
                continue;
            }

            var content = ReadAssetContent(module, relativePath, asset.ProjectAssetPath);
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            results[templateName] = new ThemeCloneTemplateEntry
            {
                TemplateName = templateName,
                RelativePath = relativePath,
                Content = content,
            };
        }

        return results.Values.OrderBy(x => x.TemplateName).ToList();
    }

    private static string ReadAssetContent(Module module, string relativePath, string projectAssetPath)
    {
        if (!string.IsNullOrWhiteSpace(projectAssetPath) && File.Exists(projectAssetPath))
        {
            return File.ReadAllText(projectAssetPath);
        }

        var fileInfo = module.GetFileInfo(relativePath);
        if (!fileInfo.Exists)
        {
            return null;
        }

        using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static Template CreateTemplate(ThemeCloneTemplateEntry candidate, string themeName) => new()
    {
        Content = candidate.Content,
        Description = $"Cloned from {themeName} ({candidate.RelativePath}).",
    };

    private static string BuildInitializationMessage(string themeName, bool cloneAllTemplates, int clonedCount, int existingLocalCustomizationCount)
    {
        if (!cloneAllTemplates)
        {
            return $"Theme customization was initialized from '{themeName}'. Theme templates can now be cloned individually.";
        }

        if (clonedCount == 0 && existingLocalCustomizationCount > 0)
        {
            return $"Theme customization was initialized from '{themeName}'. All supported templates already had local customizations.";
        }

        if (existingLocalCustomizationCount > 0)
        {
            return $"Theme customization was initialized from '{themeName}'. {clonedCount} template(s) were cloned and {existingLocalCustomizationCount} already had local customizations.";
        }

        return $"Theme customization was initialized from '{themeName}'. {clonedCount} template(s) were cloned.";
    }
}
