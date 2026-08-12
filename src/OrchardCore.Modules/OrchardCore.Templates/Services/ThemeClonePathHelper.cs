using OrchardCore.DisplayManagement.Descriptors.ShapeTemplateStrategy;

namespace OrchardCore.Templates.Services;

public static class ThemeClonePathHelper
{
    private static readonly HashSet<string> s_supportedSubPaths = new(new BasicShapeTemplateHarvester().SubPaths(), StringComparer.OrdinalIgnoreCase);

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

        var lastDash = fileName.LastIndexOf('-');
        var lastDot = fileName.LastIndexOf('.');

        templateName = lastDot <= 0 || lastDot < lastDash
            ? Adjust(subPath, fileName, null)
            : Adjust(subPath, fileName[..lastDot], fileName[(lastDot + 1)..]);

        return true;
    }

    private static string Adjust(string subPath, string fileName, string displayType)
    {
        var leader = string.Empty;
        if (subPath.StartsWith("Views/", StringComparison.Ordinal) && !string.Equals(subPath, "Views/Items", StringComparison.OrdinalIgnoreCase))
        {
            leader = string.Concat(subPath.AsSpan("Views/".Length), "_");
        }

        var shapeType = leader + fileName.Replace("--", "__", StringComparison.Ordinal).Replace("-", "__", StringComparison.Ordinal).Replace('.', '_');

        if (string.IsNullOrEmpty(displayType))
        {
            return shapeType;
        }

        var firstBreakingSeparator = shapeType.IndexOf("__", StringComparison.Ordinal);
        if (firstBreakingSeparator <= 0)
        {
            return shapeType + "_" + displayType;
        }

        return string.Concat(shapeType.AsSpan(0, firstBreakingSeparator), "_", displayType, shapeType.AsSpan(firstBreakingSeparator));
    }
}
