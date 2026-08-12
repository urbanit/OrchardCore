using OrchardCore.Security.Permissions;
using OrchardCore.Modules;

namespace OrchardCore.Templates;

[Feature("OrchardCore.ThemeCustomization")]
public sealed class ThemeCustomizationPermissions : IPermissionProvider
{
    public static readonly Permission ManageThemeCustomization = new("ManageThemeCustomization", "Manage theme customization", isSecurityCritical: true);

    private readonly IEnumerable<Permission> _allPermissions =
    [
        ManageThemeCustomization,
    ];

    public Task<IEnumerable<Permission>> GetPermissionsAsync()
        => Task.FromResult(_allPermissions);

    public IEnumerable<PermissionStereotype> GetDefaultStereotypes() =>
    [
        new PermissionStereotype
        {
            Name = OrchardCoreConstants.Roles.Administrator,
            Permissions = _allPermissions,
        },
    ];
}
