using uwap.WebFramework.Database;
using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// A table containing projects.
/// </summary>
public class ProjectTable(
    string name,
    List<ClusterNode> clusterNodes
) : AbstractOfficeTable<Project>(name, clusterNodes)
{
    protected readonly TableValueCache<Project,string> NameCache = new(obj => obj.Name);

    /// <summary>
    /// Index to find projects a user has any amount of access to.
    /// </summary>
    private M2MTableIndex<Project, UserReference> UsersWithAnyPermissionIndex
        = new(project => ((IEnumerable<UserReference>)[ project.Owner, ..project.Permissions.Select(perm => perm.User) ]).Distinct().ToList());

    protected override IEnumerable<ITableIndex<Project>> Indices
        => [ ..base.Indices, NameCache, UsersWithAnyPermissionIndex ];

    public new static ProjectTable Import(string name, List<ClusterNode> clusterNodes)
        => Tables.TryGetTable<ProjectTable>(name) ?? new ProjectTable(name, clusterNodes);

    public override ulong TypeIteration
        => 1;
    
    public Task<Project> CreateAsync(string name, UserReference owner)
        => CreateAsync(new(name, owner));
    
    /// <summary>
    /// Lists the projects the user has access to, in correct order.
    /// </summary>
    public async Task<List<Project>> ListUserProjects(Profile profile)
        => (await ListExistingByIdsAsync(await UsersWithAnyPermissionIndex.GetAsync(profile.User)))
            .OrderByDescending(project => profile.PrimaryProject.Matches(project))
            .ThenByDescending(project => project.Owner.Equals(profile.User))
            .ThenBy(project => project.Name)
            .ToList();
    
    /// <summary>
    /// Retrieves the project with the given ID if the given request has a permission with the desired access level.<br/>
    /// If the project doesn't exist or the request isn't allowed to access it, an appropriate exception is thrown.
    /// </summary>
    public async Task<Project> LoadIfAllowed(string id, Request req, bool redirect = true)
    {
        var project = await GetByIdAsync(id);
        if (!project.HasAnyPermission(req))
            if (req.LoggedIn || !redirect)
                throw new ForcedResponse(StatusResponse.Forbidden);
            else
                throw new ForcedResponse(new RedirectToLoginResponse(req));
        
        return project;
    }
    
    /// <summary>
    /// Determines whether the given name (case-insensitive) exists.
    /// </summary>
    public async Task<bool> NameExistsAsync(string name, string? excludedId)
    {
        foreach (var id in ListExistingIds())
            if (id != excludedId && (await NameCache.GetAsync(id)).Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        
        return false;
    }
}
