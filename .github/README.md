# Personal Website - Blazor

A Blazor-based personal website built with .NET 10.0, featuring a flat feature-based architecture, server-side interactivity, and content-driven pages. This project is a Blazor reimplementation of my original Next.js-based site [github.com/sametcn99/personal-website](https://github.com/sametcn99/personal-website).

## Project Purpose

- Explore Blazor as a full-stack web framework for content-heavy sites
- Apply clean, maintainable architecture patterns within the .NET ecosystem
- Build a production-ready personal site with SSR, API endpoints, and real-time interactivity

## Architecture

The project uses a **flat feature-based architecture** — all layers live at the root. Instead of a traditional Clean Architecture solution with multiple `.csproj` files, the app is organized into feature folders and logical layers within a single web project:

### Design Decisions

- **Single project, flat layout** — No nested solution folders or multi-project references. All code lives under one `.csproj`, reducing build complexity and project reference overhead.
- **Feature-based component organization** — Components are grouped by domain feature (`Blog/`, `Gists/`, `Repositories/`) rather than by type. Shared components live under `Shared/`.
- **Interface-Service separation** — Service contracts in `Interfaces/`, implementations in `Services/`. Registered via DI in `Program.cs`.
- **MVC Controllers for APIs** — REST endpoints use attribute-routed `[ApiController]` classes rather than minimal APIs, providing clearer structure and testability.
- **InteractiveServer render mode** — All Blazor components run server-side via SignalR. No WebAssembly client project needed.
- **MDX content pipeline** — Posts, gists, and projects are authored as `.mdx` files with YAML frontmatter, parsed at runtime by `ContentService`.

## Tech Stack

| Category                | Technology                                        |
|-------------------------|---------------------------------------------------|
| **Framework**           | ASP.NET Core Blazor Server (.NET 10.0)            |
| **Render Mode**         | InteractiveServer (SignalR-based)                 |
| **API Layer**           | ASP.NET Core MVC Controllers (`[ApiController]`)  |
| **Markdown**            | Markdig (MDX/MD parsing, YAML frontmatter)        |
| **HTML Processing**     | HtmlAgilityPack (HTML-to-component mapping)       |
| **YAML**                | YamlDotNet (frontmatter parsing)                  |
| **GitHub API**          | Octokit SDK (repository listing, README fetching) |
| **Caching**             | `IMemoryCache` (in-memory, per-request scoped)    |
| **Analytics**           | Umami (self-hosted, privacy-friendly)             |
| **Diagrams**            | Mermaid.js (client-side rendering via JS interop) |
| **Syntax Highlighting** | Monaco Editor (WASM-based code component)         |
| **Deployment**          | Docker (multi-stage build, Kestrel)               |

## Features

- [x] **MDX Content Pipeline** — YAML frontmatter parsing, table of contents extraction, searchable text generation
- [x] **Blog, Gists, Projects** — Three content sections with list/detail views, tag filtering, and date sorting
- [x] **GitHub Repository Browser** — Live repo listing with sort (stars, updated, created), language filter, fork badges, and star counts
- [x] **Site-wide Search** — Global search across all content types, social links, and GitHub repositories with debounced input
- [x] **Random Article Navigation** — Random post/gist/project suggestions below article prev/next navigation
- [x] **HTML-to-Blazor Component Mapping** — Automatic conversion of markdown-rendered HTML to Blazor components (code blocks, blockquotes, details/summary, tables, mermaid diagrams)
- [x] **Monaco Editor Code Blocks** — WASM-based syntax-highlighted code editor with loading skeleton animation
- [x] **Mermaid.js Diagrams** — Client-side diagram rendering in markdown content
- [x] **RSS/Atom + JSON Feed** — `/rss.xml` and `/feed.json` endpoints for content syndication
- [x] **Dynamic Sitemap** — Auto-generated `/sitemap.xml` based on content structure
- [x] **Full SEO** — Meta tags, Open Graph, Twitter Cards, manifest, robots.txt
- [x] **Security Headers** — CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy
- [x] **API Rate Limiting** — Per-IP sliding window rate limiter (120 req/min) on `/api/*` routes
- [x] **Response Caching** — Configurable cache headers for static assets, feeds, and manifest
- [x] **Forwarded Headers** — Proxy-aware (X-Forwarded-For, X-Forwarded-Proto, X-Forwarded-Host)
- [x] **Antiforgery Protection** — Built-in CSRF validation for state-changing requests
- [x] **CV Page** — Markdown-rendered CV with English/Turkish download links
- [x] **Social Links** — Configurable social link provider with Umami event tracking
- [x] **Dark Theme** — Custom CSS with CSS custom properties, no component library dependency
- [x] **Docker Support** — Multi-stage Dockerfile with .NET SDK build and ASP.NET runtime

## Setup

### Requirements

- .NET 10.0 SDK or higher

### Running Locally

```bash
cd personal-website-blazor
dotnet run --launch-profile http
```

Or with hot reload:

```bash
dotnet watch
```

Open `http://localhost:5117` in your browser.

### GitHub Token Configuration

The app reads GitHub repositories and README via the Octokit SDK. Token is configured via .NET configuration key `GitHub:Token`.

**Local development** (User Secrets):

```bash
dotnet user-secrets set "GitHub:Token" "ghp_your_token_here"
```

**Environment variable**:

```bash
export GitHub__Token="ghp_your_token_here"
```

**Coolify / Docker**: Set `GitHub__Token` in the environment/secrets configuration.

## Running with Docker

```bash
docker build -t personal-website-blazor .
docker run -p 8080:8080 personal-website-blazor
```

## Project Structure Details

### Content Pipeline

Content files live in `content/{posts,gists,projects}/` as `.mdx` files. Each file has YAML frontmatter:

```yaml
---
title: "Post Title"
publishedAt: "2025-01-15"
summary: "Brief description"
tags: ["csharp", "blazor"]
---
```

`ContentService` parses these at startup, builds an in-memory index, and serves them via scoped methods. Search operates over the indexed `SearchableText` field.

### HTML Renderer

`HtmlRenderer` is a custom Blazor component that takes raw HTML (from Markdig output) and maps elements to Blazor components:

- `<pre><code>` → `CodeComponent` (Monaco editor with syntax highlighting)
- `<blockquote>` → `HtmlBlockquote`
- `<details>` → `HtmlDetails`
- `<table>` → Styled table with responsive wrapper
- `<div class="mermaid">` → `MermaidDiagram` (client-side JS rendering)

### API Controllers

| Controller              | Routes                                        | Purpose                                         |
|-------------------------|-----------------------------------------------|-------------------------------------------------|
| `ContentController`     | `/api/content/*`, `/api/readme`, `/api/repos` | Content CRUD, GitHub README, repository listing |
| `MetadataController`    | `/manifest.webmanifest`, `/opengraph-image`   | PWA manifest, Open Graph SVG image              |
| `SyndicationController` | `/rss.xml`, `/sitemap.xml`, `/feed.json`      | RSS feed, sitemap, JSON feed                    |

### Middleware Pipeline (Program.cs)

1. Exception handler (production only)
2. Forwarded headers
3. HSTS + HTTPS redirect (production only)
4. Security headers (CSP, X-Frame-Options, etc.)
5. API rate limiting (120 req/min per IP)
6. Response cache policy (static assets, feeds, manifest)
7. Antiforgery validation
8. MVC Controllers (`MapControllers()`)
9. Razor Components (`MapRazorComponents` + InteractiveServer)
