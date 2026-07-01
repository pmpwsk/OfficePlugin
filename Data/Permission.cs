using System.Text.Json.Serialization;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// A single permission entry for a combination of module, object ID and permission level.
/// </summary>
[method: JsonConstructor]
public class Permission(
    UserReference user,
    Module module,
    PermissionLevel level
)
{
    /// <summary>
    /// The user the permission is assigned to.
    /// </summary>
    public readonly UserReference User = user;
    
    /// <summary>
    /// The module the permission is limited to.
    /// </summary>
    public readonly Module Module = module;
    
    /// <summary>
    /// The permission level.
    /// </summary>
    public readonly PermissionLevel Level = level;
}
