# Personal Website - Blazor

This project is a Blazor clone of my Next.js-based personal website [github.com/sametcn99/personal-website](https://github.com/sametcn99/personal-website).

## Project Purpose

The main goals of this project are:

- Practice with the Blazor framework
- Learn and explore what can be built with Blazor
- Apply modern web development techniques within the C# and .NET ecosystem

## Technologies

- **Framework:** Blazor Server (.NET 9.0)
- **UI Library:** MudBlazor
- **Markdown Processing:** Markdig
- **HTML Processing:** HtmlAgilityPack
- **YAML Processing:** YamlDotNet

## Architecture

The project is organized with Clean Architecture principles to separate responsibilities clearly:

- **Core/Domain**: Enterprise entities and core business models (`PostModel`, `ContentMetadata`, `SocialMediaLink`)
- **Core/Application**: Use-case contracts and application-level configuration (`IContentService`, `IRssFeedService`, `ISitemapService`, `SocialData`)
- **Infrastructure**: External concerns and concrete implementations (content parsing, RSS generation, sitemap generation)
- **Components**: Blazor presentation layer (pages, layout, shared UI components)
- **wwwroot/content**: Static assets and MD/MDX content

Current folder layout:

```text
Core/
   Application/
      Abstractions/
      Configuration/
   Domain/
      Entities/
Infrastructure/
   Content/
   Feeds/
   Seo/
Components/
content/
wwwroot/
Program.cs
personal-website-blazor.csproj
```

## Features

- [x] **Markdown Content Processing** - Full support for MDX/MD files with YAML frontmatter parsing
- [x] **MudBlazor Component Mapping** - Automatic conversion of HTML elements to MudBlazor components for consistent UI
- [x] **Content Sections** - Organized structure for blog posts, technical gists, and projects
- [x] **Dynamic Routing** - Clean URL structure with section-based routing (`/blog/{slug}`, `/project/{slug}`, `/gist/{slug}`)
- [x] **Syntax Highlighting** - Code block rendering with language-specific highlighting
- [x] **Responsive Design** - Mobile-friendly layout using MudBlazor's grid system
- [x] **Docker Support** - Containerized deployment with optimized Dockerfile
- [x] **Dynamic Sitemap Generation** - Automatic XML sitemap generation based on content structure for improved SEO
- [x] **Custom Code Components** - Enhanced code block components with advanced features and better syntax highlighting
- [x] **RSS Feed** - RSS/Atom feed generation for blog posts and content updates
- [x] **Site-wide Search** - Search functionality across posts, gists, and projects
- [x] **Content Filtering** - Filter posts, gists, and projects by tags, categories, and dates
- [x] **Dynamic Link Redirect Structure** - Flexible URL redirection system for managing content links
- [x] **Full SEO Optimization** - Comprehensive meta tags, Open Graph, Twitter Cards, and structured data implementation
- [x] **Semantic HTML Structure** - Proper semantic HTML5 elements for improved accessibility and SEO
- [x] **Fullscreen Image Viewer** - Modal-based fullscreen image viewing with zoom and pan capabilities
- [x] **Mermaid.js Diagrams** - Support for Mermaid.js diagrams in markdown content for enhanced visualizations

## Setup

### Requirements

- .NET 9.0 SDK or higher

### Running the Project

1. Clone the repository
2. Navigate to the project directory:

   ```bash
   cd personal-website-blazor
   ```

3. Start the development server (without Docker):

   ```bash
   dotnet restore
   dotnet run --launch-profile http
   ```

   Alternative commands from the repository root:

   ```bash
   dotnet watch --launch-profile http
   dotnet run --project personal-website-blazor.csproj --launch-profile http
   dotnet build personal-website-blazor.sln
   ```

4. Open `http://localhost:5117` in your browser

### GitHub Token Configuration

The app reads GitHub token from .NET configuration key `GitHub:Token`.

- Environment variable mapping for this key is: `GitHub__Token`

#### Local Development

Use User Secrets (recommended):

```bash
dotnet user-secrets set "GitHub:Token" "ghp_your_token_here"
```

Or set shell environment variable for current session:

```bash
export GitHub__Token="ghp_your_token_here"
dotnet run --launch-profile http
```

#### Coolify

In Coolify environment/secrets UI, add:

- Key: `GitHub__Token`
- Value: your GitHub token

Then redeploy the service.

## Running with Docker

To run the project in a Docker container:

```bash
docker build -t personal-website-blazor .
docker run -p 8080:8080 personal-website-blazor
```

