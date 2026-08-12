using OrchardCore.Templates.Models;

namespace OrchardCore.Templates.ViewModels;

public class ThemeCustomizationViewModel
{
    public string CurrentThemeId { get; set; }
    public string CurrentThemeName { get; set; }
    public bool CurrentThemeSupportsCustomization { get; set; }
    public bool Initialized { get; set; }
    public string BaseThemeName { get; set; }
    public bool BaseThemeIsActive { get; set; }
    public DateTime? InitializedUtc { get; set; }
    public bool CloneAllTemplatesOnInitialize { get; set; }
    public IReadOnlyList<ThemeCustomizationTemplateEntry> ThemeTemplates { get; set; } = [];
}
