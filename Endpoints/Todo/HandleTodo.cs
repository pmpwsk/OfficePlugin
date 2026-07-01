using uwap.WebFramework.Responses;
using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class OfficePlugin
{
    [Endpoint("/todo")]
    protected async Task<IResponse> HandleTodoAsync(Request req)
    {
        req.ForceGET();
        
        // data
        var module = ModuleInfo.Todo;
        var table = TodoItems;
        Project project;
        TodoItem? item;
        TableReference<TodoItem>? location;
        if (req.Query.TryGetValue("project", out var projectId))
        {
            item = null;
            project = await Projects.LoadIfAllowed(projectId, req);
            location = null;
        }
        else if (req.Query.TryGetValue("id", out var id))
        {
            item = await TodoItems.LoadIfAllowedAsync(id, PermissionLevel.Read, req);
            project = await item.Project.GetAsync();
            location = new(item);
        }
        else
            return StatusResponse.BadRequest;
        TableReference<Project> projectRef = new(project);
        var canEdit = project.CheckPermission(req, module.Id, PermissionLevel.Edit);
        
        
        var page = new Page(req, true, module.Name);
        
        // navigation
        if (item == null)
        {
            page.Sidebar.Items.Add(new LinkButton(
                new("bi bi-arrow-left", "Project"),
                $"project?id={project.Id}"
            ));
            foreach (var otherModule in ModuleInfo.All)
                if (project.CheckPermission(req, otherModule.Id, PermissionLevel.Read))
                    page.Sidebar.Items.Add(new LinkButton(
                        $"{otherModule.Name}",
                        $"{otherModule.Slug}?project={project.Id}"
                    ));
        }
        else
        {
            page.Sidebar.Items.Add(new LinkButton(
                new("bi bi-arrow-left", "Back"),
                item.Parent == null
                    ? $"{module.Slug}?project={item.Project.Id}"
                    : $"{module.Slug}?id={item.Parent.Id}"
            ));
            foreach (var (status, siblings) in await table.ListChildrenByStatus(item.Project, item.Parent))
            {
                page.Sidebar.Items.Add(new Heading3(status.ToString()));
                foreach (var sibling in siblings)
                    page.Sidebar.Items.Add(new LinkButton(
                        sibling.Name,
                        $"{module.Slug}?id={sibling.Id}"
                    ));
            }
        }
        
        // content
        var section = new Section(
            module.Name,
            []
        );
        
        // header
        if (item == null)
        {
            section.Subsections.Add(new Subsection(
                $"{project.Name} ({await project.Owner.GetAnyNameAsync(req)})",
                [
                ]
            ).Save(out var subsection));
            
            if (project.CheckPermission(req, module.Id, PermissionLevel.Manage))
                subsection.Content.Add(new Row(
                    new LinkButton(
                        new("bi bi-people", "Users"),
                        $"users?project={project.Id}&module={module.Slug}"
                    ),
                    project.PublicModules.Contains(module.Id)
                        ? new ServerActionButton(
                            new("bi bi-lock", "Make private"),
                            _ => DialogBuilder.DynamicDialogActionAsync(
                                page,
                                "Make private",
                                [
                                    new Paragraph("Are you sure you want to make this module private?"),
                                    new Row(
                                        new ContinueButton(),
                                        new DialogCancelButton(page)
                                    )
                                ],
                                async _ =>
                                {
                                    await Projects.TransactionAsync(project.Id, data =>
                                    {
                                        data.Value.PublicModules.Remove(module.Id);
                                    });
                                    return new Reload();
                                }
                            )
                        )
                        : new ServerActionButton(
                            new("bi bi-unlock", "Make public"),
                            _ => DialogBuilder.DynamicDialogActionAsync(
                                page,
                                "Make public",
                                [
                                    new Paragraph("Are you sure you want to make this module public?"),
                                    new Row(
                                        new ContinueButton(),
                                        new DialogCancelButton(page)
                                    )
                                ],
                                async _ =>
                                {
                                    await Projects.TransactionAsync(project.Id, data =>
                                    {
                                        if (!data.Value.PublicModules.Contains(module.Id))
                                            data.Value.PublicModules.Add(module.Id);
                                    });
                                    return new Reload();
                                }
                            )
                        )
                ));
        }
        else
        {
            section.Subsections.Add(new Subsection(
                item.Name,
                [
                    new Paragraph($"State: {item.Status}"),
                    new Paragraph($"Created by {await item.CreationUser.GetAnyNameAsync(req)} on {FormatDate(item.CreationDate)}"),
                    new Paragraph($"Modified by {await item.ModificationUser.GetAnyNameAsync(req)} on {FormatDate(item.ModificationDate)}")
                ]
            ).Save(out var subsection));
            
            if (canEdit)
                subsection.Content.Add(new Row(
                    new ServerActionButton(
                        new("bi bi-pen", "Edit"),
                        _ => DialogBuilder.SaveObjectDialogActionAsync(
                            page,
                            item,
                            "Edit item",
                            [
                                new SelectorBuilder<TodoItem, TodoStatus>(
                                    obj => ref obj.Status,
                                    "Select state",
                                    [
                                        new(TodoStatus.Waiting, "Waiting", "Yet to be worked on."),
                                        new(TodoStatus.Active, "Active", "Currently in progress."),
                                        new(TodoStatus.Done, "Done", "Already completed.")
                                    ],
                                    async value =>
                                    {
                                        if (value == TodoStatus.Done
                                            && item.Status != TodoStatus.Done
                                            && await TodoItems.HasUndoneChildren(projectRef, location))
                                            return "This item has sub-items that aren't done yet.";
                                        
                                        return null;
                                    }
                                ),
                                new RequiredTextBoxBuilder<TodoItem>(
                                    obj => ref obj.Name,
                                    TextBoxRole.NoAutocomplete,
                                    "todo-name",
                                    "Enter a name...",
                                    "Please enter a name.",
                                    async value =>
                                    {
                                        if (await table.NameExistsAsync(item.Project, item.Parent, value, item.IdNullable))
                                            return "This name already exists in this location.";
                                        
                                        return null;
                                    }
                                )
                            ],
                            null,
                            applicator => PersistHierarchyObject(item, table, "todo", applicator, req)
                        )
                    ),
                    new ServerActionButton(
                        new("bi bi-trash", "Delete item"),
                        _ => DialogBuilder.DeleteObjectDialogActionAsync(
                            page,
                            item,
                            table,
                            "Delete",
                            item.Name,
                            item.Parent == null
                                ? $"{module.Slug}?project={item.Project.Id}"
                                : $"{module.Slug}?id={item.Parent.Id}"
                        )
                    )
                ));
        }
        
        // children
        foreach (var pair in await table.ListChildrenByStatus(projectRef, location))
            section.Subsections.Add(new Subsection(
                pair.Status.ToString(),
                pair.Items.Select(child => new BigLinkButton(
                    child.Name,
                    [ FormatDate(child.CreationDate) ],
                    $"{module.Slug}?id={child.Id}"
                ))
            ));
        
        // add child
        if (canEdit)
            section.Subsections.Add(new ServerForm(
                null,
                [
                    new TextBox("todo-name", item == null ? "Add an item..." : "Add a nested item...", null, TextBoxRole.NoAutocomplete).Save(out var nameInput)
                ],
                async _ =>
                {
                    if (nameInput.IsEmpty(out var name))
                        return DialogBuilder.DynamicErrorAction(page, "Please enter a name for the item.");
                    
                    if (await table.NameExistsAsync(projectRef, location, name, null))
                        return DialogBuilder.DynamicErrorAction(page, "This name already exists in this location.");

                    var newItem = await table.CreateAsync(new(projectRef, location, name, req));
                    return new Navigate($"{module.Slug}?id={newItem.Id}");
                }
            ));
        
        page.Sections.Add(section);
        
        return page;
    }
}