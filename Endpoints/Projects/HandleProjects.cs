using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class OfficePlugin
{
    [Endpoint("/")]
    protected async Task<Page> HandleProjectsAsync(Request req)
    {
        req.ForceGET(); req.ForceLogin();
        
        // data
        var profile = await Profiles.GetOrCreateAsync(req);
        var projects = await Projects.ListUserProjects(profile);
        
        var page = new Page(req, true, "Projects");
        
        // content
        page.Sections.Add(new Section(
            "Projects",
            [
                new Subsection(
                    null,
                    [
                        ..await projects.SelectAsync(async project
                            => new BigLinkButton(
                                project.Name,
                                [await project.Owner.GetAnyNameAsync(req)],
                                $"project?id={project.Id}"
                            )
                        ).ToListAsync()
                    ]
                ),
                new ServerForm(
                    null,
                    [
                        new TextBox("project-name", "Create a project...", null, TextBoxRole.NoAutocomplete).Save(out var nameInput)
                    ],
                    async _ =>
                    {
                        if (nameInput.IsEmpty(out var name))
                        {
                            DialogBuilder.Error(page, "Please enter a name for the project.");
                            return;
                        }

                        var newItem = await Projects.CreateAsync(new(name, req));
                        page.Navigate($"project?id={newItem.Id}");
                    }
                )
            ]
        ));
        
        return page;
    }
}