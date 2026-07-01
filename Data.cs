using uwap.WebFramework.Database;
using uwap.WebFramework.Responses.Actions;

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
    
    private static async Task<IActionResponse> PersistHierarchyObject<C>(C obj, AbstractHierarchyTable<C> table, string slug, Action<C> applicator, Request req) where C : AbstractHierarchyValue<C>
    {
        if (obj.IdNullable == null)
        {
            applicator(obj);
            await table.CreateAsync(obj);
            return new Navigate($"{slug}?id={obj.Id}");
        }
        else
        {
            await table.ModifyAsync(obj.Id, req, data => applicator(data.Value));
            return new Reload();
        }
    }
    
    private static async Task<IActionResponse> PersistOtherObject<C>(C obj, Table<C> table, string slug, Action<C> applicator) where C : AbstractTableValue
    {
        if (obj.IdNullable == null)
        {
            applicator(obj);
            await table.CreateAsync(obj);
            return new Navigate($"{slug}?id={obj.Id}");
        }
        else
        {
            await table.TransactionAsync(obj.Id, data => applicator(data.Value));
            return new Reload();
        }
    }
}