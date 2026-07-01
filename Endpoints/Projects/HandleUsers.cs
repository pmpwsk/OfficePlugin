using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class OfficePlugin
{
    [Endpoint("/users")]
    protected async Task<IResponse> HandleUsersAsync(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        
        // data
        var projectId = req.Query.GetOrThrow("project");
        var moduleSlug = req.Query.GetOrThrow("module");
        var project = await Projects.LoadIfAllowed(projectId, req);
        var module = ModuleInfo.GetBySlug(moduleSlug);
        
        if (!project.CheckPermission(req, module.Id, PermissionLevel.Manage))
            return StatusResponse.Forbidden;
        
        var page = new Page(req, true, $"{module.Name} users");
        
        // navigation
        page.Sidebar.Items.Add(new LinkButton(
            new("bi bi-arrow-left", "Project"),
            $"project?id={project.Id}"
        ));
        foreach (var otherModule in ModuleInfo.All)
            if (project.CheckPermission(req, otherModule.Id, PermissionLevel.Manage))
                page.Sidebar.Items.Add(new LinkButton(
                    $"{otherModule.Name}",
                    $"users?project={project.Id}&module={otherModule.Slug}"
                ));
        
        // content
        page.Sections.Add(new Section(
            $"{module.Name} users",
            [
                new Subsection(
                    $"{project.Name} ({await project.Owner.GetAnyNameAsync(req)})",
                    [
                        ..(await project.ListPermissionsByModuleAsync(module.Id, req))
                        .Select(tuple => new BigServerActionButton(
                            tuple.Name,
                            [ tuple.Permission.Level.ToString() ],
                            _ => tuple.Permission.User.Matches(req)
                            ? DialogBuilder.DynamicErrorActionAsync(page, "You can't manage yourself.")
                            : DialogBuilder.DynamicDialogActionAsync(
                                page,
                                "Module member",
                                [
                                    new Paragraph($"User: {tuple.Name}"),
                                    new Paragraph($"Module: {module.Name}"),
                                    new DynamicSelector<PermissionLevel>(
                                        page,
                                        "Permission level",
                                        tuple.Permission.Level,
                                        [
                                            new(PermissionLevel.Read, "Read", "Only viewing."),
                                            new(PermissionLevel.Edit, "Edit", "Viewing and editing."),
                                            new(PermissionLevel.Manage, "Manage", "Viewing, editing and managing.")
                                        ]
                                    ).Save(out var levelInput),
                                    new Row(
                                        new ContinueButton(),
                                        new ServerSubmitButton(
                                            new("bi bi-trash", "Remove"),
                                            _ => DialogBuilder.DynamicDialogActionAsync(
                                                page,
                                                "Remove member",
                                                [
                                                    new Paragraph("Are you sure you want to remove this member?"),
                                                    new Row(
                                                        new ContinueButton(),
                                                        new DialogBackButton(page)
                                                    )
                                                ],
                                                async _ =>
                                                {
                                                    await Projects.TransactionAsync(project.Id,
                                                        data =>
                                                        {
                                                            data.Value.Permissions.RemoveAll(perm =>
                                                                perm.User.Equals(tuple.Permission.User) &&
                                                                perm.Module == module.Id);
                                                        });
                                                    return new Reload();
                                                }
                                            )
                                        ),
                                        new DialogCancelButton(page)
                                    )
                                ],
                                async _ =>
                                {
                                    await Projects.TransactionAsync(project.Id, data =>
                                    {
                                        data.Value.Permissions.RemoveAll(perm =>
                                            perm.User.Equals(tuple.Permission.User) && perm.Module == module.Id);
                                        data.Value.Permissions.Add(new(tuple.Permission.User, module.Id,
                                            levelInput.Value));
                                    });
                                    return new Reload();
                                }
                            )
                        ))
                    ]
                ),
                new ServerForm(
                    null,
                    [
                        new TextBox("member-name", "Add a member...", null, TextBoxRole.NoSpellcheck).Save(out var usernameInput)
                    ],
                    async _ =>
                    {
                        if (usernameInput.IsEmpty(out var username))
                            return DialogBuilder.DynamicErrorAction(page, "Please enter a username.");
                        
                        var user = await req.UserTable.FindByUsernameAsync(username);
                        if (user == null)
                            return DialogBuilder.DynamicErrorAction(page, "This user does not exist.");
                        
                        var userRef = new UserReference(req.UserTable.Name, user.Id);
                        if (project.Owner.Equals(userRef) || project.Permissions.Any(perm => perm.User.Equals(userRef) && perm.Module == module.Id))
                            return DialogBuilder.DynamicErrorAction(page, "This user is already a member.");
                        
                        await Projects.TransactionAsync(project.Id, data =>
                        {
                            data.Value.Permissions.RemoveAll(perm => perm.User.Equals(userRef) && perm.Module == module.Id);
                            data.Value.Permissions.Add(new(userRef, module.Id, PermissionLevel.Read));
                        });
                        
                        return new Reload();
                    }
                )
            ]
        ));
        
        return page;
    }
}