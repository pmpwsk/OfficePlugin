using System.Text.Json.Serialization;
using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// Item of the to-do module.
/// </summary>
public class TodoItem : AbstractHierarchyValue<TodoItem>
{
    /// <summary>
    /// The current status of the item.
    /// </summary>
    public TodoStatus Status;
    
    /// <summary>
    /// The item's description.
    /// </summary>
    public string? Description;
    
    public TodoItem(TableReference<Project> project, TableReference<TodoItem>? parent, string name, Request req)
        : base(project, parent, name, req)
    {
        Status = TodoStatus.Waiting;
        Description = null;
    }
    
    [JsonConstructor]
    public TodoItem(
        EntryState state,
        TableReference<Project> project,
        TableReference<TodoItem>? parent,
        string name,
        DateTime creationDate,
        UserReference creationUser,
        DateTime modificationDate,
        UserReference modificationUser,
        TodoStatus status,
        string? description)
        : base(state, project, parent, name, creationDate, creationUser, modificationDate, modificationUser)
    {
        Status = status;
        Description = description;
    }
}
