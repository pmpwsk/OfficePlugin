using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// A table containing to-do items.
/// </summary>
public class TodoTable(
    string name,
    List<ClusterNode> clusterNodes,
    ProjectTable projectTable
) : AbstractHierarchyTable<TodoItem>(name, clusterNodes, projectTable)
{
    public static TodoTable Import(string name, List<ClusterNode> clusterNodes, ProjectTable projectTable)
        => Tables.TryGetTable<TodoTable>(name) ?? new TodoTable(name, clusterNodes, projectTable);

    public override ulong TypeIteration
        => 1;

    protected override Module Module
        => Module.Todo;

    /// <summary>
    /// Lists the child items of the given parent, categorized by their status and sorted.
    /// </summary>
    public async Task<List<(TodoStatus Status, List<TodoItem> Items)>> ListChildrenByStatus(TableReference<Project> projectRef, TableReference<TodoItem>? parentRef)
    {
        Dictionary<TodoStatus, List<TodoItem>> categorization = [];
        foreach (var item in await ListChildrenAsync(projectRef, parentRef))
        {
            var list = categorization.GetValueOrAdd(item.Status, () => []);
            list.Add(item);
        }
        
        List<(TodoStatus Status, List<TodoItem> Items)> result = [];
        foreach (var (status, items) in categorization.OrderBy(x => x.Key))
            result.Add((status, items.OrderBy(item => item.CreationDate).ToList()));
        
        return result;
    }
    
    /// <summary>
    /// Determines whether the given parent has any sub-items with a status other than "done".
    /// </summary>
    public async Task<bool> HasUndoneChildren(TableReference<Project> projectRef, TableReference<TodoItem>? parentRef)
    {
        await foreach (var childId in EnumerateChildIdsRecursivelyAsync(projectRef, parentRef))
            if ((await GetByIdAsync(childId)).Status != TodoStatus.Done)
                return true;
        
        return false;
    }
}
