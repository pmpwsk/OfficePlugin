using System.Text.Json.Serialization;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Database;

namespace uwap.WebFramework.Plugins;

/// <summary>
/// Contains a reference to a user, using the user table name and user ID.
/// </summary>
public class UserReference : TableReference<User>
{
    [method: JsonConstructor]
    public UserReference(string tableName, string id)
        : base(tableName, id)
    {
    }

    public UserReference(User user)
        : base(user)
    {
    }

    public UserReference(Request req)
        : this(req.User)
    {
    }

    /// <summary>
    /// Attempts to retrieve the <c>User</c> object for this user reference while considering the requesting user.
    /// </summary>
    public Task<User?> GetNullableAsync(Request req)
    {
        if (!Loaded && req.UserTableNullable?.Name == TableName && req.UserNullable?.Id == Id)
        {
            CachedValue = req.User;
            Loaded = true;
            return Task.FromResult<User?>(CachedValue);
        }
        
        return GetNullableAsync();
    }

    /// <summary>
    /// Determines the displayed name for this user reference.
    /// </summary>
    public async Task<string> GetAnyNameAsync(Request req)
        => Tables.TryGetTable<ProfileTable>(OfficePlugin.ProfileTableName, out var profileTable)
            ? await profileTable.GetAnyNameAsync(this, req)
            : "Unknown user";
    
    /// <summary>
    /// Finds the display name of the user, or returns null if no name was set.
    /// </summary>
    public async Task<string?> GetProfileNameAsync()
        => Tables.TryGetTable<ProfileTable>(OfficePlugin.ProfileTableName, out var profileTable)
            ? await profileTable.GetProfileNameAsync(this)
            : null;

    /// <summary>
    /// Returns whether the request is logged into this user.
    /// </summary>
    public bool Matches(Request req)
        => req.LoggedIn
           && req.UserTable.Name == TableName
           && req.User.Id == Id;
}
