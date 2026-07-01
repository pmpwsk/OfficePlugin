using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// Information about a module, not persisted.
/// </summary>
public class ModuleInfo(Module id, string slug, string name, string description)
{
    /// <summary>
    /// Information about the to-do list module.
    /// </summary>
    public static readonly ModuleInfo Todo = new(Module.Todo, "todo", "To-do list", "Manage your tasks.");
    
    /// <summary>
    /// A collection of all available modules.
    /// </summary>
    public static readonly IReadOnlyCollection<ModuleInfo> All = [ Todo ];
    
    /// <summary>
    /// Attempts to find the module with the given slug and throws a <c>DatabaseEntryMissingException</c> if no such module exists.
    /// </summary>
    public static ModuleInfo GetBySlug(string slug)
    {
        foreach (var module in All)
            if (module.Slug == slug)
                return module;
        
        throw new DatabaseEntryMissingException();
    }
    
    /// <summary>
    /// The module's identifier for persistence.
    /// </summary>
    public readonly Module Id = id;
    
    /// <summary>
    /// The module's URL slug.
    /// </summary>
    public readonly string Slug = slug;
    
    /// <summary>
    /// The module's display name.
    /// </summary>
    public readonly string Name = name;
    
    /// <summary>
    /// The module's caption.
    /// </summary>
    public readonly string Description = description;
}
