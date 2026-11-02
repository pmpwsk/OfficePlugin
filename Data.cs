using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

public partial class OfficePlugin(List<ClusterNode> clusterNodes) : Plugin
{   
    public const string ProjectTableName = "OfficePlugin.Projects";
    private readonly ProjectTable Projects = new(ProjectTableName, clusterNodes);
    
    public const string ProfileTableName = "OfficePlugin.Profiles";
    private readonly ProfileTable Profiles = new(ProfileTableName, clusterNodes);
    
    public const string TodoTableName = "OfficePlugin.TodoItems";
    private readonly TodoTable TodoItems = new(TodoTableName, clusterNodes);
    
    public static string FormatDate(DateTime utc)
        => $"{utc.ToLongDateString()}";
}