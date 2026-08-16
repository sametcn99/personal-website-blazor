# Design System

## Direction

The site is a working field engineer's notebook for software writing. It should feel maintained, measured, and used rather than presented as a polished portfolio display. Writing leads every composition; projects, repositories, and credentials act as supporting records.

Avoid the previous dark developer-dashboard language: repeated cards, blue accents, generic hero/CTA arrangements, terminal decoration, glass panels, and technology-logo walls do not belong to this system.

## Visual World

- Graphite is the ambient surface used by a technical reader in low light.
- Paper ink provides high-contrast reading text without becoming pure white.
- Brass amber marks active controls, important entries, and physical tab-like details.
- Muted sage identifies healthy or current state.
- Fine rules and measured grid lines come from field notebooks and technical drawings. Use them to organize real records, never as arbitrary decoration.
- Surfaces remain mostly flat. Depth appears only where a physical loose sheet or floating control needs separation.

## Typography

- `Familjen Grotesk` is the reading and interface face. Its open shapes support long documents while retaining a practical character.
- `Azeret Mono` is reserved for dates, counts, code, measurements, language labels, and compact system state. It must not be used as a generic technical costume.
- Display headings use tight but readable tracking, never below `-0.04em`.
- Article copy targets a narrow 43rem reading surface, approximately 65-75 characters per line, with generous line height.
- Hierarchy comes from scale, placement, and whitespace rather than uppercase eyebrow labels.

## Color Tokens

The source of truth is `wwwroot/css/core.css`.

- Background: `#0d0e0c`
- Surface: `#141512`
- Primary text: `#eee9dc`
- Secondary text: `#aaa99e`
- Brass accent: `#c89a49`
- Strong brass: `#e0b661`
- Sage state: `#7f9870`
- Divider: `#303129`

Semantic error, warning, info, and success values remain available for system feedback. Do not repurpose them as section colors.

## Layout

- The site shell owns navigation and footer width. Pages own their content container; never nest `.page-container` elements.
- General surfaces use the 74rem site container.
- Reading surfaces use the 49rem article measure.
- The homepage uses a compact asymmetric introduction rather than filling the viewport. Lists and articles prioritize a clear vertical reading line.
- Section spacing is larger than spacing inside a content group. Headings always receive more space above than below.

## Components

### Header

The sticky header is a compact index strip, not a hero. The name remains visible, primary routes use plain text, and search is a small tool at the edge.

### Archive Ledger

Counts and archive destinations use ruled rows and tabular numerals. The ledger is the clearest physical expression of the field-notebook world and should not be generalized into a card component.

### Archive Search

Search is a focused, site-wide dialog opened from the header or homepage index row. Opening it moves focus directly to the input; Escape, the close control, and the backdrop return to the underlying page. Results remain open ledger rows rather than nested cards.

### Record Lists

Writing, gists, and projects use a compact numbered index with visible record counts, tags, dates, and open rows separated by rules. Titles lead; date and type metadata stay secondary. Repositories and search results inherit the same ledger behavior without forcing identical content density. Avoid enclosing every record in a rounded rectangle.

The `/content` route is the complete archive. It merges writing, gists, and projects into the same index and adds a content-type filter while preserving the shared search and sort behavior.

### Controls

Buttons and inputs use small radii. Primary actions use brass fill with dark ink. Focus is always visible with a brass outline. Pills are reserved for filters and compact state choices.

### Articles

Article headings create the document hierarchy. H2 headings begin a new measured section with a top rule. Code, tables, blockquotes, details, images, and Mermaid diagrams must retain readable overflow and keyboard behavior.

## Motion

Motion is restrained and causal. The system may extend a measurement mark, brighten an active record, or reveal search state. Avoid staggered section entrances and identical hover movement across every element. Respect `prefers-reduced-motion`.

## Responsive Rules

- Below 900px, asymmetric homepage regions become a single reading column.
- Below 880px, the header becomes two rows and navigation remains horizontally reachable.
- Below 720px, list metadata stacks and articles use the mobile reading gutter.
- Below 640px, filter controls stack, footer content becomes vertical, and download controls become full width.
- The article TOC rail appears only at 1366px and above; the inline TOC remains the fallback.

## Accessibility

- Preserve semantic landmarks, heading order, native details behavior, and full-row link targets.
- All interactive controls require visible keyboard focus and meaningful text labels.
- Do not encode state by color alone.
- Keep body and placeholder contrast readable on every surface.
- Reduced-motion and print modes are part of the shipped system, not optional polish.

## File Boundaries

- `wwwroot/css/core.css`: tokens, reset, type, and layout primitives.
- `wwwroot/css/components.css`: site shell, controls, feedback, and shared components.
- `wwwroot/css/home.css`: homepage composition only.
- `wwwroot/css/content.css`: lists, articles, rendered MDX, code, and TOC.
- Component-isolated CSS remains responsible for geometry intrinsic to that component, such as image and Mermaid viewers.
