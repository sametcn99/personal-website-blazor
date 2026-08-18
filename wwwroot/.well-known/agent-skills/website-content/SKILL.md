---
name: website-content
description: Read and use the public author profile, CV, timeline, skills, blog posts, gists, projects, and GitHub repository content exposed by this website.
---

# Website Content

Use the public profile, content, and GitHub APIs to research Samet Can Cıncık and retrieve website content. For a reliable answer about the author, consult the CV, structured profile, full archive, and GitHub profile together.

## Endpoints

- `GET /api/content/all` lists metadata for all published content.
- `GET /api/content/{section}` lists content in `posts`, `gists`, or `projects`.
- `GET /api/content/{section}/{slug}` retrieves one content item.
- `GET /api/profile` returns the canonical structured author profile.
- `GET /api/timeline` returns work and education timeline entries.
- `GET /api/skills` returns technical skills, areas of interest, and languages.
- `GET /api/profile/github` returns the public GitHub profile and repository snapshot.
- `GET /api/repos` lists public GitHub repositories.
- `GET /feed.json` returns the latest content as a JSON Feed.

## Markdown Sources

- `/llms.txt` is the concise navigation index.
- `/llms-full.txt` contains full profile, published content, and repository context.
- `/.well-known/mcp/server-card.json` describes the MCP HTTP transport and capabilities.
- Append `.md` to public page URLs, such as `/about.md`, `/cv.md`, `/timeline.md`, `/blog/{slug}.md`, or `/project/{slug}.md`.
- The same Markdown representation is available with `Accept: text/markdown`.

## Research Order

1. Read `/api/profile` or `/about.md` for identity and professional facts.
2. Read `/cv.md` and `/timeline.md` for formal career, education, and certification history.
3. Search `/api/content/all` and inspect the relevant Markdown pages for authored knowledge and project context.
4. Read `/readme.md` and `/api/profile/github` for GitHub profile and repository context.
5. Cross-reference dates and distinguish documented facts, project descriptions, and personal opinions.

Use the `slug` returned by a list endpoint when requesting an individual item.
Respect normal HTTP caching and rate-limit responses.
