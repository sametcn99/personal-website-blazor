namespace personal_website_blazor.Models;

internal sealed class RateLimitEntry
{
    public long WindowStart { get; set; }
    public int Count { get; set; }
}
