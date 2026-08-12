namespace OrchardCore.Templates.Models;

/// <summary>
/// Represents a cloneable Liquid template from the selected base theme and its local customization state.
/// </summary>
public class ThemeCustomizationTemplateEntry
{
    /// <summary>
    /// Gets or sets the Orchard template name.
    /// </summary>
    public string TemplateName { get; set; }

    /// <summary>
    /// Gets or sets the relative path of the source Liquid file inside the base theme.
    /// </summary>
    public string RelativePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a local template override currently exists.
    /// </summary>
    public bool HasLocalCustomization { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the local override is tracked by theme customization.
    /// </summary>
    public bool IsTrackedCustomization { get; set; }
}
