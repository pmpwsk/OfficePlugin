namespace uwap.WebFramework.Plugins;

/// <summary>
/// The possible user permission levels.
/// </summary>
public enum PermissionLevel
{
    /// <summary>
    /// The user can only read the object.
    /// </summary>
    Read,
    
    /// <summary>
    /// The user can read and edit the object.
    /// </summary>
    Edit,
    
    /// <summary>
    /// The user can read, edit and administer the object.
    /// </summary>
    Manage
}
