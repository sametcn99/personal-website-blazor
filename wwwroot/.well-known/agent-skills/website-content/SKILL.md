---
name: website-content
description: Read and use the public blog posts, gists, projects, and repository content exposed by this website.
---

# Website Content

Use the public content APIs to discover and retrieve website content.

## Endpoints

- `GET /api/content/all` lists metadata for all published content.
- `GET /api/content/{section}` lists content in `posts`, `gists`, or `projects`.
- `GET /api/content/{section}/{slug}` retrieves one content item.
- `GET /api/repos` lists public GitHub repositories.
- `GET /feed.json` returns the latest content as a JSON Feed.

Use the `slug` returned by a list endpoint when requesting an individual item.
Respect normal HTTP caching and rate-limit responses.
