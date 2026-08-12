using OrchardCore.DisplayManagement.Descriptors.ShapeTemplateStrategy;

namespace OrchardCore.Templates.Services;

public static class ThemeClonePathHelper
{
    private static readonly BasicShapeTemplateHarvester s_harvester = new();
    private static readonly HashSet<string> s_supportedSubPaths = new(s_harvester.SubPaths(), StringComparer.OrdinalIgnoreCase);

    public static bool TryMapLiquidTemplate(string relativePath, out string templateName)
    {
        templateName = null;

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        relativePath = relativePath.Replace('\\', '/').Trim('/');
        var extension = Path.GetExtension(relativePath);
        if (!string.Equals(extension, ".liquid", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var subPath = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty;
        if (!s_supportedSubPaths.Contains(subPath))
        {
            return false;
        }

        var fileName = Path.GetFileNameWithoutExtension(relativePath);
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        // Delegate to the same shape-harvesting algorithm the display pipeline uses at
        // runtime, so cloned template names always match the shape names theme templates
        // are resolved by.
        var hit = s_harvester.HarvestShape(new HarvestShapeInfo
        {
            SubPath = subPath,
            FileName = fileName,
            RelativePath = relativePath,
            Extension = extension,
        }).FirstOrDefault();

        if (hit == null || string.IsNullOrEmpty(hit.ShapeType))
        {
            return false;
        }

        templateName = hit.ShapeType;

        return true;
    }
}
