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
        svg.AppendLine($"  <text x=\"32\" y=\"40\" text-anchor=\"middle\" fill=\"{SiteThemeSvg.TextPrimary}\" font-family=\"{SiteThemeSvg.FontSans}\" font-size=\"20\" font-weight=\"700\" letter-spacing=\"-0.8\">SC</text>");
        svg.AppendLine($"  <circle cx=\"49\" cy=\"50\" r=\"3\" fill=\"{SiteThemeSvg.Sage}\" />");
        svg.AppendLine("</svg>");
        return svg.ToString();
    }
}
