using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using HtmlAgilityPack;
using MudBlazor;

namespace personal_website_blazor.Components.Shared
{
    public class MudHtmlRenderer : ComponentBase
    {
        [Parameter]
        public string HtmlContent { get; set; } = string.Empty;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (string.IsNullOrEmpty(HtmlContent))
                return;

            var doc = new HtmlDocument();
            doc.LoadHtml(HtmlContent);

            RenderNode(builder, doc.DocumentNode);
        }

        private void RenderNode(RenderTreeBuilder builder, HtmlNode node)
        {
            foreach (var child in node.ChildNodes)
            {
                switch (child.NodeType)
                {
                    case HtmlNodeType.Text:
                        builder.AddContent(0, child.InnerText);
                        break;

                    case HtmlNodeType.Element:
                        RenderElement(builder, child);
                        break;
                }
            }
        }

        private void RenderElement(RenderTreeBuilder builder, HtmlNode node)
        {
            switch (node.Name.ToLowerInvariant())
            {
                case "h1":
                    RenderMudText(builder, node, Typo.h1);
                    break;
                case "h2":
                    RenderMudText(builder, node, Typo.h2);
                    break;
                case "h3":
                    RenderMudText(builder, node, Typo.h3);
                    break;
                case "h4":
                    RenderMudText(builder, node, Typo.h4);
                    break;
                case "h5":
                    RenderMudText(builder, node, Typo.h5);
                    break;
                case "h6":
                    RenderMudText(builder, node, Typo.h6);
                    break;
                case "p":
                    RenderMudText(builder, node, Typo.body1);
                    break;
                case "a":
                    RenderMudLink(builder, node);
                    break;
                case "img":
                    RenderMudImage(builder, node);
                    break;
                case "pre":
                    RenderMudPre(builder, node);
                    break;
                case "code":
                    RenderMudCode(builder, node);
                    break;
                default:
                    // Fallback for known containers or just pass through
                    builder.OpenElement(0, node.Name);
                    foreach (var attribute in node.Attributes)
                    {
                        builder.AddAttribute(1, attribute.Name, attribute.Value);
                    }
                    RenderNode(builder, node);
                    builder.CloseElement();
                    break;
            }
        }

        private void RenderMudText(RenderTreeBuilder builder, HtmlNode node, Typo typo)
        {
            builder.OpenComponent<MudText>(0);
            builder.AddAttribute(1, nameof(MudText.Typo), typo);

            // Map common classes or styles if needed, or pass child content
            // For Text, we want to render the inner content
            builder.AddAttribute(2, nameof(MudText.ChildContent), (RenderFragment)((b) =>
            {
                RenderNode(b, node);
            }));

            builder.CloseComponent();
        }

        private void RenderMudLink(RenderTreeBuilder builder, HtmlNode node)
        {
            builder.OpenComponent<MudLink>(0);

            var href = node.GetAttributeValue("href", string.Empty);
            builder.AddAttribute(1, nameof(MudLink.Href), href);

            var target = node.GetAttributeValue("target", string.Empty);
            if (!string.IsNullOrEmpty(target))
            {
                builder.AddAttribute(2, nameof(MudLink.Target), target);
            }

            builder.AddAttribute(3, nameof(MudLink.ChildContent), (RenderFragment)((b) =>
            {
                RenderNode(b, node);
            }));

            builder.CloseComponent();
        }

        private void RenderMudImage(RenderTreeBuilder builder, HtmlNode node)
        {
            builder.OpenComponent<MudImage>(0);

            var src = node.GetAttributeValue("src", string.Empty);
            builder.AddAttribute(1, nameof(MudImage.Src), src);

            var alt = node.GetAttributeValue("alt", string.Empty);
            builder.AddAttribute(2, nameof(MudImage.Alt), alt);

            // Optional: Make it responsive
            builder.AddAttribute(3, nameof(MudImage.Fluid), true);
            builder.AddAttribute(4, nameof(MudImage.Elevation), 25);
            builder.AddAttribute(5, nameof(MudImage.Class), "rounded-lg my-4");

            builder.CloseComponent();
        }

        private void RenderMudPre(RenderTreeBuilder builder, HtmlNode node)
        {
            // Check if there's a code element inside
            var codeNode = node.SelectSingleNode(".//code");

            if (codeNode != null)
            {
                // Extract language from class attribute (e.g., "language-csharp")
                string language = string.Empty;
                var classAttr = codeNode.GetAttributeValue("class", string.Empty);

                if (!string.IsNullOrEmpty(classAttr))
                {
                    var classes = classAttr.Split(' ');
                    foreach (var cls in classes)
                    {
                        if (cls.StartsWith("language-"))
                        {
                            language = cls.Replace("language-", "");
                            break;
                        }
                    }
                }

                // Get the code content
                string code = codeNode.InnerText;

                // Render CodeComponent
                builder.OpenComponent<CodeComponent>(0);
                builder.AddAttribute(1, nameof(CodeComponent.Code), code);
                builder.AddAttribute(2, nameof(CodeComponent.Language), language);
                builder.CloseComponent();
            }
            else
            {
                // Fallback: render as basic MudPaper if no code element found
                builder.OpenComponent<MudPaper>(0);
                builder.AddAttribute(1, nameof(MudPaper.Elevation), 0);
                builder.AddAttribute(2, nameof(MudPaper.Outlined), true);
                builder.AddAttribute(3, nameof(MudPaper.Class), "pa-4 my-4 overflow-x-auto");
                builder.AddAttribute(4, nameof(MudPaper.Style), "background-color: #272727; color: #f8f8f2;");

                builder.AddAttribute(5, nameof(MudPaper.ChildContent), (RenderFragment)((b) =>
                {
                    b.OpenElement(0, "pre");
                    RenderNode(b, node);
                    b.CloseElement();
                }));

                builder.CloseComponent();
            }
        }

        private void RenderMudCode(RenderTreeBuilder builder, HtmlNode node)
        {
            // If parent is PRE, it is a block code, usually handled by RenderMudPre wrapping.
            // But we still render the code element itself here.
            bool isBlock = node.ParentNode.Name.Equals("pre", StringComparison.OrdinalIgnoreCase);

            if (isBlock)
            {
                builder.OpenElement(0, "code");
                // Block code styling if needed, otherwise inherit from pre/paper
                RenderNode(builder, node);
                builder.CloseElement();
            }
            else
            {
                // Inline code
                builder.OpenElement(0, "code");
                builder.AddAttribute(1, "class", "px-1 py-0.5 rounded mud-typography-body2");
                builder.AddAttribute(2, "style", "background-color: rgba(255,255,255,0.1); color: var(--mud-palette-primary); font-family: monospace;");
                RenderNode(builder, node);
                builder.CloseElement();
            }
        }
    }
}
