using OrchardCore.Data.Documents;

namespace OrchardCore.Templates.Models;

public class ThemeCustomizationDocument : Document
{
    public bool Initialized { get; set; }
    public string BaseThemeId { get; set; }
    public string InitializedByUserId { get; set; }
    public DateTime? InitializedUtc { get; set; }
    public List<string> ClonedTemplates { get; init; } = [];
}
