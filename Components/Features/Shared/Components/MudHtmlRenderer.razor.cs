using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;
using personal_website_blazor.Components.Features.Shared.HtmlComponents;

namespace personal_website_blazor.Components.Features.Shared.Components
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
                    RenderHtmlTypography(builder, node, Typo.h1);
                    break;
                case "h2":
                    RenderHtmlTypography(builder, node, Typo.h2);
                    break;
                case "h3":
                    RenderHtmlTypography(builder, node, Typo.h3);
                    break;
                case "h4":
                    RenderHtmlTypography(builder, node, Typo.h4);
                    break;
                case "h5":
                    RenderHtmlTypography(builder, node, Typo.h5);
                    break;
                case "h6":
                    RenderHtmlTypography(builder, node, Typo.h6);
                    break;
                case "p":
                    RenderHtmlTypography(builder, node, Typo.body1);
                    break;
                case "ul":
                    RenderHtmlList(builder, node, false);
                    break;
                case "ol":
                    RenderHtmlList(builder, node, true);
                    break;
                case "li":
                    RenderHtmlListItem(builder, node);
                    break;
                case "blockquote":
                    RenderHtmlBlockquote(builder, node);
                    break;
                case "a":
                    RenderHtmlLink(builder, node);
                    break;
                case "img":
                    RenderHtmlImage(builder, node);
                    break;
                case "pre":
                    RenderHtmlPre(builder, node);
                    break;
                case "code":
                    RenderHtmlCode(builder, node);
                    break;
                case "table":
                    RenderHtmlTable(builder, node);
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

        private void RenderHtmlTypography(RenderTreeBuilder builder, HtmlNode node, Typo typo)
        {
            builder.OpenComponent<HtmlTypography>(0);
            builder.AddAttribute(1, nameof(HtmlTypography.Typo), typo);
            builder.AddAttribute(
                2,
                nameof(HtmlTypography.ChildContent),
                (RenderFragment)(
                    (b) =>
                    {
                        RenderNode(b, node);
                    }
                )
            );
            builder.CloseComponent();
        }

        private void RenderHtmlList(RenderTreeBuilder builder, HtmlNode node, bool ordered)
        {
            builder.OpenComponent<HtmlList>(0);
            builder.AddAttribute(1, nameof(HtmlList.Ordered), ordered);
            builder.AddAttribute(
                2,
                nameof(HtmlList.ChildContent),
                (RenderFragment)(
                    (b) =>
                    {
                        RenderNode(b, node);
                    }
                )
            );
            builder.CloseComponent();
        }

        private void RenderHtmlListItem(RenderTreeBuilder builder, HtmlNode node)
        {
            builder.OpenComponent<HtmlListItem>(0);
            builder.AddAttribute(
                1,
                nameof(HtmlListItem.ChildContent),
                (RenderFragment)(
                    (b) =>
                    {
                        RenderNode(b, node);
                    }
                )
            );
            builder.CloseComponent();
        }

        private void RenderHtmlBlockquote(RenderTreeBuilder builder, HtmlNode node)
        {
            builder.OpenComponent<HtmlBlockquote>(0);
            builder.AddAttribute(
                1,
                nameof(HtmlBlockquote.ChildContent),
                (RenderFragment)(
                    (b) =>
                    {
                        RenderNode(b, node);
                    }
                )
            );
            builder.CloseComponent();
        }

        private void RenderHtmlTable(RenderTreeBuilder builder, HtmlNode node)
        {
            builder.OpenComponent<HtmlTable>(0);
            builder.AddAttribute(
                1,
                nameof(HtmlTable.ChildContent),
                (RenderFragment)(
                    (b) =>
                    {
                        RenderNode(b, node);
                    }
                )
            );
            builder.CloseComponent();
        }

        private void RenderHtmlLink(RenderTreeBuilder builder, HtmlNode node)
        {
            builder.OpenComponent<HtmlLink>(0);
            var href = node.GetAttributeValue("href", string.Empty);
            builder.AddAttribute(1, nameof(HtmlLink.Href), href);
            var target = node.GetAttributeValue("target", string.Empty);
            if (!string.IsNullOrEmpty(target))
            {
                builder.AddAttribute(2, nameof(HtmlLink.Target), target);
            }
            builder.AddAttribute(
                3,
                nameof(HtmlLink.ChildContent),
                (RenderFragment)(
                    (b) =>
                    {
                        RenderNode(b, node);
                    }
                )
            );
            builder.CloseComponent();
        }

        private void RenderHtmlImage(RenderTreeBuilder builder, HtmlNode node)
        {
            builder.OpenComponent<HtmlImage>(0);
            var src = node.GetAttributeValue("src", string.Empty);
            builder.AddAttribute(1, nameof(HtmlImage.Src), src);
            var alt = node.GetAttributeValue("alt", string.Empty);
            builder.AddAttribute(2, nameof(HtmlImage.Alt), alt);
            builder.CloseComponent();
        }

        private void RenderHtmlPre(RenderTreeBuilder builder, HtmlNode node)
        {
            var codeNode = node.SelectSingleNode(".//code");

            if (codeNode != null)
            {
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

                string code = codeNode.InnerText;

                if (language.Equals("mermaid", StringComparison.OrdinalIgnoreCase))
                {
                    builder.OpenComponent<MermaidDiagram>(0);
                    builder.AddAttribute(1, nameof(MermaidDiagram.Definition), code);
                    builder.CloseComponent();
                    return;
                }

                builder.OpenComponent<CodeComponent>(0);
                builder.AddAttribute(1, nameof(CodeComponent.Code), code);
                builder.AddAttribute(2, nameof(CodeComponent.Language), language);
                builder.CloseComponent();
            }
            else
            {
                var plainPreContent = HtmlEntity.DeEntitize(node.InnerText).Trim();
                if (IsLikelyMermaidDefinition(plainPreContent))
                {
                    builder.OpenComponent<MermaidDiagram>(0);
                    builder.AddAttribute(1, nameof(MermaidDiagram.Definition), plainPreContent);
                    builder.CloseComponent();
                    return;
                }

                builder.OpenComponent<HtmlPre>(0);
                builder.AddAttribute(
                    1,
                    nameof(HtmlPre.ChildContent),
                    (RenderFragment)(
                        (b) =>
                        {
                            RenderNode(b, node);
                        }
                    )
                );
                builder.CloseComponent();
            }
        }

        private static bool IsLikelyMermaidDefinition(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var firstLine = value
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(firstLine))
            {
                return false;
            }

            return firstLine.StartsWith("graph ", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("flowchart ", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("sequenceDiagram", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("classDiagram", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("stateDiagram", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("erDiagram", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("journey", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("gantt", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("pie", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("mindmap", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("timeline", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("gitGraph", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("quadrantChart", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("requirementDiagram", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("C4Context", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("C4Container", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("C4Component", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("C4Dynamic", StringComparison.OrdinalIgnoreCase)
                || firstLine.StartsWith("C4Deployment", StringComparison.OrdinalIgnoreCase);
        }

        private void RenderHtmlCode(RenderTreeBuilder builder, HtmlNode node)
        {
            bool isBlock = node.ParentNode.Name.Equals("pre", StringComparison.OrdinalIgnoreCase);

            if (isBlock)
            {
                builder.OpenElement(0, "code");
                RenderNode(builder, node);
                builder.CloseElement();
            }
            else
            {
                builder.OpenComponent<HtmlInlineCode>(0);
                builder.AddAttribute(
                    1,
                    nameof(HtmlInlineCode.ChildContent),
                    (RenderFragment)(
                        (b) =>
                        {
                            RenderNode(b, node);
                        }
                    )
                );
                builder.CloseComponent();
            }
        }
    }
}
