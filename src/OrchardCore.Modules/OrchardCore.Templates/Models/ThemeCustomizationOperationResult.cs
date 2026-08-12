namespace OrchardCore.Templates.Models;

public class ThemeCustomizationOperationResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; }
    public int TemplatesAffected { get; init; }
}
