using uwap.WebFramework.Responses.Actions;
using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class OfficePlugin
{
    [Endpoint("/project")]
    protected async Task<Page> HandleProjectAsync(Request req)
    {
        req.ForceGET();
        
        // data
        var projectId = req.Query.GetOrThrow("id");
        var project = await Projects.LoadIfAllowed(projectId, req);
        
        var page = new Page(req, true, project.Name);
        
        // navigation
        if (req.LoggedIn)
        {
            page.Sidebar.Items.Add(new LinkButton(
                new("bi bi-arrow-left", "Projects"),
                "."
            ));
            var profile = await Profiles.GetOrCreateAsync(req);
            var projects = await Projects.ListUserProjects(profile);
            foreach (var otherProject in projects)
                page.Sidebar.Items.Add(new LinkButton(
                    $"{otherProject.Name} ({await otherProject.Owner.GetAnyNameAsync(req)})",
                    $"project?id={otherProject.Id}"
                ));
        }
        
        // content
        page.Sections.Add(new Section(
            "Project",
            [
                new Subsection(
                    $"{project.Name} ({await project.Owner.GetAnyNameAsync(req)})",
                    []
                ).Save(out var subsection)
            ]
        ));
        
        if (project.Owner.Matches(req))
            subsection.Content.Add(new Row(
                new ServerActionButton(
                    new("bi bi-pen", "Edit"),
                    _ => DialogBuilder.SaveObjectDialogActionAsync(
                        page,
                        project,
                        "Edit project",
                        [
                            new RequiredTextBoxBuilder<Project>(
                                obj => ref obj.Name,
                                TextBoxRole.NoAutocomplete,
                                "project-name",
                                "Enter a name...",
                                "Please enter a name.",
                                async value =>
                                {
                                    if (await Projects.NameExistsAsync(value, project.IdNullable))
                                        return "This name already exists.";

                                    return null;
                                }
                            )
                        ],
                        null,
                        applicator => PersistOtherObject(project, Projects, "project", applicator)
                    )
                ),
                new ServerActionButton(
                    new("bi bi-trash", "Delete"),
                    _ => DialogBuilder.DynamicDialogActionAsync(
                        page,
                        "Delete project",
                        [
                            new Paragraph($"Are you sure you want to delete \"{project.Name}\"?"),
                            new Row(
                                new ContinueButton(),
                                new DialogCancelButton(page)
                            )
                        ],
                        async _ =>
                        {
                            await TodoItems.DeleteForProjectAsync(project);
                            await Projects.DeleteAsync(project);
                            return new Navigate(".");
                        }
                    )
                )
            ));
        
        foreach (var module in ModuleInfo.All)
            if (project.CheckPermission(req, module.Id, PermissionLevel.Read))
                subsection.Content.Add(new BigLinkButton(
                    module.Name,
                    [module.Description],
                    $"{module.Slug}?project={project.Id}"
                ));
        
        return page;
    }
}