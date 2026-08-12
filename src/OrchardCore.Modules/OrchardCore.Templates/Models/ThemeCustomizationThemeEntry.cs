namespace OrchardCore.Templates.Models;

public class ThemeCustomizationThemeEntry
{
    public string ThemeId { get; set; }
    public string ThemeName { get; set; }
    public int CloneableTemplateCount { get; set; }
    public bool IsCurrent { get; set; }
}
