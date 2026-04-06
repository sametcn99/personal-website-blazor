using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using personal_website_blazor.Components.Features.Shared.HtmlComponents;

namespace personal_website_blazor.Components.Features.Shared.Components;

public class HtmlRenderer : ComponentBase
{
    [Parameter]
    public string HtmlContent { get; set; } = string.Empty;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (string.IsNullOrEmpty(HtmlContent))
        {
            return;
        }

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
                    builder.AddContent(0, HtmlEntity.DeEntitize(child.InnerText));
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
            case "h2":
            case "h3":
            case "h4":
            case "h5":
            case "h6":
                RenderHtmlTypography(builder, node, node.Name.ToLowerInvariant());
                break;
            case "p":
                RenderHtmlTypography(builder, node, "p");
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
            case "details":
                RenderHtmlDetails(builder, node);
                break;
            case "summary":
                RenderHtmlSummary(builder, node);
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

    private void RenderHtmlTypography(RenderTreeBuilder builder, HtmlNode node, string tag)
    {
        builder.OpenComponent<HtmlTypography>(0);
        builder.AddAttribute(1, nameof(HtmlTypography.Tag), tag);
        
        var id = node.GetAttributeValue("id", string.Empty);
        if (!string.IsNullOrEmpty(id))
        {
            builder.AddAttribute(2, nameof(HtmlTypography.Id), id);
        }

        builder.AddAttribute(
            3,
            nameof(HtmlTypography.ChildContent),
            (RenderFragment)(fragmentBuilder => RenderNode(fragmentBuilder, node))
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
            (RenderFragment)(fragmentBuilder => RenderNode(fragmentBuilder, node))
        );
        builder.CloseComponent();
    }

    private void RenderHtmlListItem(RenderTreeBuilder builder, HtmlNode node)
    {
        builder.OpenComponent<HtmlListItem>(0);
        builder.AddAttribute(
            1,
            nameof(HtmlListItem.ChildContent),
            (RenderFragment)(fragmentBuilder => RenderNode(fragmentBuilder, node))
        );
        builder.CloseComponent();
    }

    private void RenderHtmlBlockquote(RenderTreeBuilder builder, HtmlNode node)
    {
        builder.OpenComponent<HtmlBlockquote>(0);
        builder.AddAttribute(
            1,
            nameof(HtmlBlockquote.ChildContent),
            (RenderFragment)(fragmentBuilder => RenderNode(fragmentBuilder, node))
        );
        builder.CloseComponent();
    }

    private void RenderHtmlDetails(RenderTreeBuilder builder, HtmlNode node)
    {
        var summaryNode = GetSummaryNode(node);

        builder.OpenComponent<HtmlDetails>(0);
        builder.AddAttribute(1, nameof(HtmlDetails.Open), node.Attributes.Contains("open"));
        builder.AddAttribute(
            2,
            nameof(HtmlDetails.SummaryContent),
            (RenderFragment)(fragmentBuilder =>
            {
                if (summaryNode is null)
                {
                    return;
                }

                RenderNode(fragmentBuilder, summaryNode);
            })
        );
        builder.AddAttribute(
            3,
            nameof(HtmlDetails.ChildContent),
            (RenderFragment)(fragmentBuilder => RenderNodeChildren(fragmentBuilder, node, summaryNode))
        );
        builder.CloseComponent();
    }

    private void RenderHtmlSummary(RenderTreeBuilder builder, HtmlNode node)
    {
        builder.OpenComponent<HtmlSummary>(0);
        builder.AddAttribute(
            1,
            nameof(HtmlSummary.ChildContent),
            (RenderFragment)(fragmentBuilder => RenderNode(fragmentBuilder, node))
        );
        builder.CloseComponent();
    }

    private void RenderHtmlTable(RenderTreeBuilder builder, HtmlNode node)
    {
        builder.OpenComponent<HtmlTable>(0);
        builder.AddAttribute(
            1,
            nameof(HtmlTable.ChildContent),
            (RenderFragment)(fragmentBuilder => RenderNode(fragmentBuilder, node))
        );
        builder.CloseComponent();
    }

    private void RenderHtmlLink(RenderTreeBuilder builder, HtmlNode node)
    {
        builder.OpenComponent<HtmlLink>(0);
        builder.AddAttribute(1, nameof(HtmlLink.Href), node.GetAttributeValue("href", string.Empty));

        var target = node.GetAttributeValue("target", string.Empty);
        if (!string.IsNullOrEmpty(target))
        {
            builder.AddAttribute(2, nameof(HtmlLink.Target), target);
        }

        builder.AddAttribute(
            3,
            nameof(HtmlLink.ChildContent),
            (RenderFragment)(fragmentBuilder => RenderNode(fragmentBuilder, node))
        );
        builder.CloseComponent();
    }

    private void RenderHtmlImage(RenderTreeBuilder builder, HtmlNode node)
    {
        builder.OpenComponent<HtmlImage>(0);
        builder.AddAttribute(1, nameof(HtmlImage.Src), node.GetAttributeValue("src", string.Empty));
        builder.AddAttribute(2, nameof(HtmlImage.Alt), node.GetAttributeValue("alt", string.Empty));
        builder.CloseComponent();
    }

    private void RenderHtmlPre(RenderTreeBuilder builder, HtmlNode node)
    {
        var codeNode = node.SelectSingleNode(".//code");

        if (codeNode is null)
        {
            builder.OpenComponent<HtmlPre>(0);
            builder.AddAttribute(1, nameof(HtmlPre.ChildContent), (RenderFragment)(fragmentBuilder => RenderNode(fragmentBuilder, node)));
            builder.CloseComponent();
            return;
        }

        var language = string.Empty;
        var classAttr = codeNode.GetAttributeValue("class", string.Empty);

        if (!string.IsNullOrEmpty(classAttr))
        {
            foreach (var cssClass in classAttr.Split(' '))
            {
                if (cssClass.StartsWith("language-", StringComparison.Ordinal))
                {
                    language = cssClass.Replace("language-", string.Empty, StringComparison.Ordinal);
                    break;
                }
            }
        }

        var code = HtmlEntity.DeEntitize(codeNode.InnerText);

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

    private void RenderHtmlCode(RenderTreeBuilder builder, HtmlNode node)
    {
        if (node.ParentNode?.Name.Equals("pre", StringComparison.OrdinalIgnoreCase) == true)
        {
            return;
        }

        builder.OpenComponent<HtmlInlineCode>(0);
        builder.AddAttribute(1, nameof(HtmlInlineCode.ChildContent), (RenderFragment)(fragmentBuilder => fragmentBuilder.AddContent(0, HtmlEntity.DeEntitize(node.InnerText))));
        builder.CloseComponent();
    }

    private static HtmlNode? GetSummaryNode(HtmlNode node)
    {
        foreach (var child in node.ChildNodes)
        {
            if (child.NodeType == HtmlNodeType.Element
                && child.Name.Equals("summary", StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private void RenderNodeChildren(RenderTreeBuilder builder, HtmlNode node, HtmlNode? excludedNode)
    {
        foreach (var child in node.ChildNodes)
        {
            if (ReferenceEquals(child, excludedNode))
            {
                continue;
            }

            switch (child.NodeType)
            {
                case HtmlNodeType.Text:
                    builder.AddContent(0, HtmlEntity.DeEntitize(child.InnerText));
                    break;
                case HtmlNodeType.Element:
                    RenderElement(builder, child);
                    break;
            }
        }
    }
}