using System.Text.Json.Serialization;
using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// Abstract value within an object hierarchy.
/// </summary>
public abstract class AbstractHierarchyValue<P> : AbstractTableValue where P : AbstractTableValue
{
    /// <summary>
    /// The project the object belongs to.
    /// </summary>
    public TableReference<Project> Project;
    
    /// <summary>
    /// The reference to the object's parent, if it has a parent.
    /// </summary>
    public TableReference<P>? Parent;
    
    /// <summary>
    /// The object's name.
    /// </summary>
    public string Name;
    
    /// <summary>
    /// The date when the object was created.
    /// </summary>
    public DateTime CreationDate;
    
    /// <summary>
    /// The user who created the object.
    /// </summary>
    public UserReference CreationUser;
    
    /// <summary>
    /// The date when the object was last changed.
    /// </summary>
    public DateTime ModificationDate;
    
    /// <summary>
    /// The last user who changed the object.
    /// </summary>
    public UserReference ModificationUser;
    
    protected AbstractHierarchyValue(TableReference<Project> project, TableReference<P>? parent, string name, Request req)
        : base(EntryState.CreateEmpty())
    {
        Project = project;
        Parent = parent;
        Name = name;

        var date = DateTime.UtcNow;
        CreationDate = date;
        ModificationDate = date;
        
        var user = new UserReference(req);
        CreationUser = user;
        ModificationUser = user;
    }
    
    [JsonConstructor]
    protected AbstractHierarchyValue(
        EntryState state,
        TableReference<Project> project,
        TableReference<P>? parent,
        string name,
        DateTime creationDate,
        UserReference creationUser,
        DateTime modificationDate,
        UserReference modificationUser)
        : base(state)
    {
        Project = project;
        Parent = parent;
        Name = name;
        CreationDate = creationDate;
        CreationUser = creationUser;
        ModificationDate = modificationDate;
        ModificationUser = modificationUser;
    }
}
