using uwap.WebFramework.Database;
using uwap.WebFramework.Responses;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// An abstract table for entries within an object hierarchy. 
/// </summary>
public abstract class AbstractHierarchyTable<T>(
    string name,
    List<ClusterNode> clusterNodes,
    ProjectTable projectTable
) : AbstractOfficeTable<T>(name, clusterNodes) where T : AbstractHierarchyValue<T>
{
    protected ProjectTable ProjectTable = projectTable;
    
    protected abstract Module Module { get; }
    
    /// <summary>
    /// Index to find children of a given parent.
    /// </summary>
    protected readonly MultipleTableIndex<T,(TableReference<Project> Project, TableReference<T>? Parent)> ChildrenIndex = new(obj => (obj.Project, obj.Parent));
    
    protected readonly TableValueCache<T,string> NameCache = new(obj => obj.Name);

    protected override IEnumerable<ITableIndex<T>> Indices
        => [ ..base.Indices, ChildrenIndex, NameCache ];

    public override Task DeleteAsync(T value)
        => DeleteByIdAsync(value.Id, value.Project);
    
    private async Task DeleteByIdAsync(string id, TableReference<Project> projectRef)
    {
        foreach (var child in await ChildrenIndex.GetAsync((projectRef, new(Name, id))))
            await DeleteByIdAsync(child, projectRef);
        
        await RoughDeleteByIdAsync(id);
    }
    
    /// <summary>
    /// Deletes all objects within a project.
    /// </summary>
    public async Task DeleteForProjectAsync(Project project)
    {
        var projectRef = new TableReference<Project>(project);
        foreach (var child in await ChildrenIndex.GetAsync((projectRef, null)))
            await DeleteByIdAsync(child, projectRef);
    }

    /// <summary>
    /// Lists the child objects of the given parent reference.
    /// </summary>
    public async Task<List<T>> ListChildrenAsync(TableReference<Project> projectRef, TableReference<T>? parentRef)
    {
        var ids = await ChildrenIndex.GetAsync((projectRef, parentRef));
        return await ListExistingByIdsAsync(ids);
    }
    
    /// <summary>
    /// Retrieves the object with the given ID if the given request has a permission with the desired access level.<br/>
    /// If the object doesn't exist or the request isn't allowed to access it, an appropriate exception is thrown.
    /// </summary>
    public async Task<T> LoadIfAllowedAsync(string id, PermissionLevel permissionLevel, Request req, bool redirect = true)
    {
        var value = await GetByIdAsync(id);
        
        var project = await value.Project.GetAsync();
        if (!project.CheckPermission(req, Module, permissionLevel))
            if (req.LoggedIn || !redirect)
                throw new ForcedResponse(StatusResponse.Forbidden);
            else
                throw new ForcedResponse(new RedirectToLoginResponse(req));
        
        return value;
    }
    
    /// <summary>
    /// Determines whether the given name (case-insensitive) exists in the given parent, while ignoring the excluded ID.
    /// </summary>
    public async Task<bool> NameExistsAsync(TableReference<Project> projectRef, TableReference<T>? parentRef, string name, string? excludedId)
    {
        foreach (var id in await ChildrenIndex.GetAsync((projectRef, parentRef)))
            if (id != excludedId && (await NameCache.GetAsync(id)).Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        
        return false;
    }
    
    /// <summary>
    /// Recursively enumerates the IDs of all objects that are children (or grandchildren, ...) to the given parent reference.
    /// </summary>
    public async IAsyncEnumerable<string> EnumerateChildIdsRecursivelyAsync(TableReference<Project> projectRef, TableReference<T>? parentRef)
    {
        foreach(var id in await ChildrenIndex.GetAsync((projectRef, parentRef)))
        {
            yield return id;
            await foreach (var subId in EnumerateChildIdsRecursivelyAsync(projectRef, new(Name, id)))
                yield return subId;
        }
    }
    
    /// <summary>
    /// Recursively retrieves the children of the given parent that have names that match the given query.
    /// </summary>
    public async Task<List<T>> SearchByNameAsync(TableReference<Project> projectRef, TableReference<T>? parentRef, string query)
    {
        var querySplit = query.Split(' ');
        List<(T Item, uint Relevance)> results = [];
        await foreach (var id in EnumerateChildIdsRecursivelyAsync(projectRef, parentRef))
        {
            var name = (await NameCache.GetAsync(id)).ToLower();
            uint relevance = 0;
            if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                relevance++;
            if (name.Contains(query, StringComparison.OrdinalIgnoreCase))
                relevance++;
            if (querySplit.Any(part => name.StartsWith(part, StringComparison.OrdinalIgnoreCase)))
                relevance++;
            if (querySplit.Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase)))
                relevance++;
            
            if (relevance > 0)
                results.Add((await GetByIdAsync(id), relevance));
        }
        
        return results
            .OrderByDescending(pair => pair.Relevance)
            .ThenBy(pair => pair.Item.Name)
            .Select(pair => pair.Item)
            .ToList();
    }
    
    /// <summary>
    /// Modifies the object with the given ID by applying the given action.
    /// </summary>
    public Task ModifyAsync(string id, Request req, TransactionDelegate<T> action)
        => TransactionAsync(id, data =>
        {
            action(data);
            data.Value.ModificationDate = DateTime.UtcNow;
            data.Value.ModificationUser = new(req);
        });
    
    /// <summary>
    /// Sets the name of the object with the given ID to the given value.
    /// </summary>
    public Task RenameAsync(string id, string newName, Request req)
        => ModifyAsync(id, req, data => data.Value.Name = newName);
    
    /// <summary>
    /// Sets the parent of the object with the given ID to the given value.
    /// </summary>
    public Task MoveAsync(string id, TableReference<T>? newParentRef, Request req)
        => ModifyAsync(id, req, data => data.Value.Parent = newParentRef);
}
