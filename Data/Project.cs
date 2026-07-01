using System.Text.Json.Serialization;
using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// A project, containing relevant project information and a list of permissions.
/// </summary>
[method: JsonConstructor]
public class Project(
    EntryState state,
    string name,
    UserReference owner,
    List<Permission> permissions,
    List<Module> publicModules
) : AbstractTableValue(state)
{
    /// <summary>
    /// The project's name.
    /// </summary>
    public string Name = name;
    
    /// <summary>
    /// The user who owns the project.
    /// </summary>
    public UserReference Owner = owner;
    
    /// <summary>
    /// The combinations of user, module and their corresponding permission level.
    /// </summary>
    public List<Permission> Permissions = permissions;
    
    /// <summary>
    /// The modules that are publicly readable.
    /// </summary>
    public List<Module> PublicModules = publicModules;
    
    public Project(string name, UserReference owner)
        : this(EntryState.CreateEmpty(), name, owner, [], [])
    {
    }
    
    public Project(string name, Request req)
        : this(name, new UserReference(req))
    {
    }
    
    /// <summary>
    /// Determines whether the request is allowed to access the given module with the given permission level.
    /// </summary>
    public bool CheckPermission(Request req, Module module, PermissionLevel level)
    {
        // public module
        if (level == PermissionLevel.Read && PublicModules.Contains(module))
            return true;

        // the rest requires login
        if (!req.LoggedIn)
            return false;

        // project owner
        if (Owner.Matches(req))
            return true;
        
        // matching permissions
        if (Permissions.Any(perm => perm.User.Matches(req) && perm.Module == module && perm.Level >= level))
            return true;
        
        // no matches
        return false;
    }
    
    /// <summary>
    /// Determines whether the request has any amount of access to the project.
    /// </summary>
    public bool HasAnyPermission(Request req)
    {
        // public module
        if (PublicModules.Count > 0)
            return true;

        // the rest requires login
        if (!req.LoggedIn)
            return false;

        // project owner
        if (Owner.Matches(req))
            return true;
        
        // matching permissions
        if (Permissions.Any(perm => perm.User.Matches(req) && perm.Level >= PermissionLevel.Read))
            return true;
        
        // no matches
        return false;
    }
    
    /// <summary>
    /// Lists the permissions for the given module along with their resolved name.
    /// </summary>
    public async Task<List<(Permission Permission, string Name)>> ListPermissionsByModuleAsync(Module module, Request req)
    {
        List<(Permission Permission, string Name)> result = [];
        
        foreach (var perm in Permissions)
            if (perm.Module == module)
            {
                var user = await perm.User.GetNullableAsync(req);
                var displayName = await perm.User.GetProfileNameAsync();
                
                string name;
                if (user == null)
                    name = "Unknown user";
                else if (displayName == null)
                    name = user.Username;
                else
                    name = $"{user.Username} ({displayName})";
                
                result.Add((perm, name));
            }
        
        return result.OrderBy(tuple => tuple.Name).ToList();
    }
}
