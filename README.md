# Personal Website - Blazor

This project is a Blazor clone of my Next.js-based personal website live at [sametcc.me](https://sametcc.me/).

## Project Purpose

The main goals of this project are:

- Practice with the Blazor framework
- Learn and explore what can be built with Blazor
- Apply modern web development techniques within the C# and .NET ecosystem

> **Note:** The original Next.js version is kept private as the repository contains sensitive data that needs to remain confidential.

## Technologies

- **Framework:** Blazor Server (.NET 9.0)
- **UI Library:** MudBlazor
- **Markdown Processing:** Markdig
- **HTML Processing:** HtmlAgilityPack
- **YAML Processing:** YamlDotNet

## Features

- [x] **Markdown Content Processing** - Full support for MDX/MD files with YAML frontmatter parsing
- [x] **MudBlazor Component Mapping** - Automatic conversion of HTML elements to MudBlazor components for consistent UI
- [x] **Content Sections** - Organized structure for blog posts, technical gists, and projects
- [x] **Dynamic Routing** - Clean URL structure with section-based routing (`/blog/{slug}`, `/project/{slug}`, `/gist/{slug}`)
- [x] **Syntax Highlighting** - Code block rendering with language-specific highlighting
- [x] **Responsive Design** - Mobile-friendly layout using MudBlazor's grid system
- [x] **Docker Support** - Containerized deployment with optimized Dockerfile
- [ ] **Fullscreen Image Viewer** - Modal-based fullscreen image viewing with zoom and pan capabilities
- [x] **Dynamic Sitemap Generation** - Automatic XML sitemap generation based on content structure for improved SEO
- [ ] **Custom Code Components** - Enhanced code block components with advanced features and better syntax highlighting
- [x] **RSS Feed** - RSS/Atom feed generation for blog posts and content updates
- [ ] **Site-wide Search** - Search functionality across posts, gists, and projects
- [ ] **Content Filtering** - Filter posts, gists, and projects by tags, categories, and dates
- [ ] **Dynamic Link Redirect Structure** - Flexible URL redirection system for managing content links
- [ ] **Full SEO Optimization** - Comprehensive meta tags, Open Graph, Twitter Cards, and structured data implementation
- [ ] **Semantic HTML Structure** - Proper semantic HTML5 elements for improved accessibility and SEO

## Setup

### Requirements

- .NET 9.0 SDK or higher

### Running the Project

1. Clone the repository
2. Navigate to the project directory:

   ```bash
   cd personal-website-blazor
   ```

3. Start the development server:

   ```bash
   dotnet watch
   ```

4. Open `https://localhost:5001` in your browser

## Running with Docker

To run the project in a Docker container:

```bash
docker build -t personal-website-blazor .
docker run -p 8080:8080 personal-website-blazor
```
