using uwap.WebFramework.Accounts;
using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// A table containing user profiles.
/// </summary>
public class ProfileTable(
    string name,
    List<ClusterNode> clusterNodes,
    ProjectTable projectTable
) : AbstractOfficeTable<Profile>(name, clusterNodes)
{
    private ProjectTable ProjectTable = projectTable;
    
    /// <summary>
    /// Index to find user profiles by their user reference.
    /// </summary>
    public UniqueTableIndex<Profile, UserReference> UserIndex = new(profile => profile.User);

    protected override IEnumerable<ITableIndex<Profile>> Indices => [ UserIndex ];
        
    public static ProfileTable Import(string name, List<ClusterNode> clusterNodes, ProjectTable projectTable)
        => Tables.TryGetTable<ProfileTable>(name) ?? new ProfileTable(name, clusterNodes, projectTable);

    public override ulong TypeIteration
        => 1;
    
    /// <summary>
    /// Retrieves the user profile for the given user or creates a new profile for that user if no such profile exists.
    /// </summary>
    public Task<Profile> GetOrCreateAsync(Request req)
        => GetOrCreateAsync(req.User);
    
    /// <summary>
    /// Retrieves the user profile for the given user or creates a new profile for that user if no such profile exists.
    /// </summary>
    public async Task<Profile> GetOrCreateAsync(User user)
    {
        var userReference = new UserReference(user);
        var profile = await GetByIdNullableAsync(await UserIndex.GetAsync(userReference));
        
        if (profile == null)
        {
            var project = await ProjectTable.CreateAsync("Personal", userReference);
            profile = await CreateAsync(new Profile(userReference, new(project)));
        }
        
        return profile;
    }
    
    /// <summary>
    /// Determines the name of the given user to use in user interfaces.<br/>
    /// Priority: DisplayName, User.Username, "Unknown user"
    /// </summary>
    public async Task<string> GetAnyNameAsync(UserReference userReference, Request req)
    {
        var profile = await GetByIdNullableAsync(await UserIndex.GetAsync(userReference));
        return profile != null
            ? await profile.GetAnyNameAsync(req)
            : (await userReference.GetNullableAsync(req))?.Username ?? "Unknown user";
    }
    
    /// <summary>
    /// Finds the profile name of the given user, or returns null if no name was set.
    /// </summary>
    public async Task<string?> GetProfileNameAsync(UserReference userReference)
    {
        var profile = await GetByIdNullableAsync(await UserIndex.GetAsync(userReference));
        return profile?.DisplayName;
    }
}
