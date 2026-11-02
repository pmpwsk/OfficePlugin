namespace uwap.WebFramework.Plugins;

public partial class OfficePlugin
{   
    public const string ProjectTableName = "OfficePlugin.Projects";
    private readonly ProjectTable Projects;
    
    public const string ProfileTableName = "OfficePlugin.Profiles";
    private readonly ProfileTable Profiles;
    
    public const string TodoTableName = "OfficePlugin.TodoItems";
    private readonly TodoTable TodoItems;
    
    public static string FormatDate(DateTime utc)
        => $"{utc.ToLongDateString()}";
}