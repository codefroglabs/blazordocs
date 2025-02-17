1. Clone or download the repository (volunteers are welcome to help with NuGet packaging)
2. Add the following to your `Program.cs` file

```csharp
builder.Services.AddMarkdownDocs(builder.Environment.ContentRootPath);
```

3. Add the following to your `.csproj` file

```xml
<ItemGroup>
    <Content Include="Docs\**">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
</ItemGroup>
```

4. Create `Docs.razor`:

```razor
@page "/docs/{**slug}"
@using BlazorDocs.Components

<DocumentationPage Slug="@Slug"/>

@code {
    [Parameter] public string? Slug { get; set; }
}
```

5. Reference the BlazorDocs CSS in your `wwwroot/index.html` file (note: sponsor me or volunteer to help improve this step)

```html
<link href="_content/BlazorDocs/app.css" rel="stylesheet" />
```

6. Add markdown files to the `Docs` folder

## Conventional Routing
BlazorDocs uses conventional routing to determine which markdown file to display. For example, if the user navigates to `/docs/introduction`, BlazorDocs will look for a markdown file named `01-introduction.md` in the `Docs` folder.

The `01-` prefix is used to determine the order in which the markdown files are displayed in the navigation. If you want a markdown file to be displayed first, name it `01-file-name.md`. If you want a markdown file to be displayed second, name it `02-file-name.md`, and so on.

### Directory Structure
Use directories to organize your markdown files. Directories can be ordered by prefixing them with a number. For example, if you want a directory to be displayed first, name it `01-directory-name`. If you want a directory to be displayed second, name it `02-directory-name`, and so on. Whitespace is allowed in directory names.

For a root page for a directory, create a markdown file with the name `index.md`. For example, if you have a directory named `01-directory-name`, create a markdown file named `01-directory-name/index.md`.