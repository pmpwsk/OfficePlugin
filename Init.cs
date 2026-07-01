using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

public partial class OfficePlugin : Plugin
{
    public OfficePlugin(List<ClusterNode> clusterNodes)
    {
        Projects = ProjectTable.Import(ProjectTableName, clusterNodes);
        Profiles = ProfileTable.Import(ProfileTableName, clusterNodes, Projects);
        TodoItems = TodoTable.Import(TodoTableName, clusterNodes, Projects);
    }
}