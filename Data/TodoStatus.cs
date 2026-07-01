namespace uwap.WebFramework.Plugins;

/// <summary>
/// The possible states of a to-do module item.
/// </summary>
public enum TodoStatus
{
    /// <summary>
    /// The to-do item is in progress.
    /// </summary>
    Active,
    
    /// <summary>
    /// The to-do item is yet to be worked on.
    /// </summary>
    Waiting,
    
    /// <summary>
    /// The to-do item has been completed.
    /// </summary>
    Done
}
