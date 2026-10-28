using OrchardCore.DisplayManagement.Manifest;
using OrchardCore.Modules.Manifest;

[assembly: Theme(
    Name = "The Admin Liquid Theme",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "The markup of The Admin theme, rendered with Liquid templates, which can be cloned and customized as admin templates.",
    BaseTheme = "TheAdmin",
    Dependencies =
    [
        "OrchardCore.Liquid",
    ],
    Tags =
    [
        ManifestConstants.AdminTag,
        "Liquid",
    ]
)]
