using System.Text.Json.Serialization;
using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// A user profile, containing relevant user information.
/// </summary>
[method: JsonConstructor]
public class Profile(
    EntryState state,
    UserReference user,
    string? displayName,
    TableReference<Project> primaryProject
) : AbstractTableValue(state)
{
    /// <summary>
    /// The user the profile belongs to.
    /// </summary>
    public UserReference User = user;
    
    /// <summary>
    /// The user's optional display name.
    /// </summary>
    public string? DisplayName = displayName;
    
    /// <summary>
    /// The ID of the user's primary project, usually the personal project.
    /// </summary>
    public TableReference<Project> PrimaryProject = primaryProject;
    
    public Profile(UserReference user, TableReference<Project> primaryProject)
        : this(EntryState.CreateEmpty(), user, null, primaryProject)
    {
    }
    
    /// <summary>
    /// Determines the name to use in user interfaces.<br/>
    /// Priority: DisplayName, User.Username, "Unknown user"
    /// </summary>
    public async Task<string> GetAnyNameAsync(Request req)
    {
        if (DisplayName != null)
            return DisplayName;
        
        var user = await User.GetNullableAsync(req);
        return user?.Username ?? "Unknown user";
    }
}
