using System.Collections.Concurrent;
using System.IO;
using System.Text;
using SkiaSharp;
using Svg.Skia;

namespace personal_website_blazor.Services;

public static class FaviconRasterizer
{
    private static readonly ConcurrentDictionary<int, Lazy<byte[]>> PngCache = new();
    private static readonly Lazy<byte[]> IcoCache = new(BuildIcoBytes);

    public static byte[] GetPng(int size)
    {
        if (size is not (16 or 32 or 64 or 180 or 192 or 512))
            throw new ArgumentOutOfRangeException(nameof(size), "Unsupported favicon size.");

        return PngCache.GetOrAdd(size, static key => new Lazy<byte[]>(() => RenderPng(key))).Value;
    }

    public static byte[] GetIco() => IcoCache.Value;

    private static byte[] RenderPng(int size)
    {
        using var svg = new SKSvg();
        svg.FromSvg(FaviconSvgBuilder.Build());

        if (svg.Picture is null)
            throw new InvalidOperationException("The favicon SVG could not be parsed.");

        using var bitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        var bounds = svg.Picture.CullRect;
        var scale = Math.Min(size / bounds.Width, size / bounds.Height);
        canvas.Translate(
            (size - bounds.Width * scale) / 2f - bounds.Left * scale,
            (size - bounds.Height * scale) / 2f - bounds.Top * scale);
        canvas.Scale(scale);
        canvas.DrawPicture(svg.Picture);
        canvas.Flush();

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    private static byte[] BuildIcoBytes()
    {
        var sizes = new[] { 16, 32, 48 };
        var images = sizes.Select(RenderIcoPng).ToArray();
        var directorySize = 6 + (16 * images.Length);
        var offset = directorySize;

        using var stream = new MemoryStream(directorySize + images.Sum(image => image.Length));
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)images.Length);

        for (var index = 0; index < sizes.Length; index++)
        {
            var size = sizes[index];
            var image = images[index];
            writer.Write((byte)size);
            writer.Write((byte)size);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)image.Length);
            writer.Write((uint)offset);
            offset += image.Length;
        }

        foreach (var image in images)
            writer.Write(image);

        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] RenderIcoPng(int size) => RenderPng(size);
}
