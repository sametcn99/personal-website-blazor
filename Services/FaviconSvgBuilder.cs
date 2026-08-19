using System.Text;

namespace personal_website_blazor.Services;

public static class FaviconSvgBuilder
{
    public static string Build()
    {
        var svg = new StringBuilder(1800);
        svg.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        svg.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"64\" height=\"64\" viewBox=\"0 0 64 64\" role=\"img\" aria-labelledby=\"favicon-title\">");
        svg.AppendLine("  <title id=\"favicon-title\">Samet Can Cıncık</title>");
        svg.AppendLine($"  <rect width=\"64\" height=\"64\" rx=\"14\" fill=\"{SiteThemeSvg.Background}\" />");
        svg.AppendLine($"  <rect x=\"4\" y=\"4\" width=\"56\" height=\"56\" rx=\"12\" fill=\"{SiteThemeSvg.Surface}\" stroke=\"{SiteThemeSvg.Divider}\" stroke-width=\"2\" />");
        svg.AppendLine($"  <rect x=\"11\" y=\"10\" width=\"42\" height=\"4\" rx=\"2\" fill=\"{SiteThemeSvg.Brass}\" />");
        svg.AppendLine($"  <path d=\"M18 24H13V40H18M46 24H51V40H46\" fill=\"none\" stroke=\"{SiteThemeSvg.Brass}\" stroke-width=\"2.5\" stroke-linecap=\"square\" />");
        svg.AppendLine($"  <path d=\"M29 25H22C20.3 25 19 26.3 19 28V29.5C19 31.2 20.3 32.5 22 32.5H26C27.7 32.5 29 33.8 29 35.5V37C29 38.7 27.7 40 26 40H19\" fill=\"none\" stroke=\"{SiteThemeSvg.TextPrimary}\" stroke-width=\"2.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />");
        svg.AppendLine($"  <path d=\"M45 25H40C38.3 25 37 26.3 37 28V37C37 38.7 38.3 40 40 40H45\" fill=\"none\" stroke=\"{SiteThemeSvg.TextPrimary}\" stroke-width=\"2.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\" />");
        svg.AppendLine($"  <circle cx=\"49\" cy=\"50\" r=\"3\" fill=\"{SiteThemeSvg.Sage}\" />");
        svg.AppendLine("</svg>");
        return svg.ToString();
    }
}
