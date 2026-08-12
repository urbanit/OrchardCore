using OrchardCore.Documents;
using OrchardCore.Modules;
using OrchardCore.Templates.Models;

namespace OrchardCore.Templates.Services;

[Feature("OrchardCore.ThemeCustomization")]
public class ThemeCustomizationManager
{
    private readonly IDocumentManager<ThemeCustomizationDocument> _documentManager;

    public ThemeCustomizationManager(IDocumentManager<ThemeCustomizationDocument> documentManager)
    {
        _documentManager = documentManager;
    }

    public Task<ThemeCustomizationDocument> LoadDocumentAsync() => _documentManager.GetOrCreateMutableAsync();

    public Task<ThemeCustomizationDocument> GetDocumentAsync() => _documentManager.GetOrCreateImmutableAsync();

    public Task UpdateAsync(ThemeCustomizationDocument document) => _documentManager.UpdateAsync(document);
}
