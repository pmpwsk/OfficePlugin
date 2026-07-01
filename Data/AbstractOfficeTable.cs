using System.Text.Json.Serialization;
using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// An abstract table containing the common serializer. 
/// </summary>
[method: JsonConstructor]
public abstract class AbstractOfficeTable<T>(
    string name,
    List<ClusterNode> clusterNodes
) : Table<T>(name, clusterNodes) where T : AbstractTableValue
{
    public override AbstractSerializer Serializer
        => Serializers.Json;
}
