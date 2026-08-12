namespace OrchardCore.Templates.Models;

public class ThemeCustomizationOperationResult
{
    public bool Succeeded { get; init; }
    public string MessageKey { get; init; }
    public object[] MessageArgs { get; init; } = [];
    public int TemplatesAffected { get; init; }
}
