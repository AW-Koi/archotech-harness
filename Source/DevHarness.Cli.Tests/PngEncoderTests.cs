using System.Buffers.Binary;
using System.IO.Compression;

namespace DevHarness.Cli.Tests;

public class PngEncoderTests
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    // Two pixels in BGRA order: blue then red.
    private static readonly byte[] TwoPixelsBgra = [255, 0, 0, 255, 0, 0, 255, 255];

    [Fact]
    public void Encode_TwoByOneImage_WritesSignatureAndDimensions()
    {
        var png = PngEncoder.Encode(TwoPixelsBgra, width: 2, height: 1);

        Assert.Equal(PngSignature, png[..8]);
        Assert.Equal("IHDR", System.Text.Encoding.ASCII.GetString(png, 12, 4));
        Assert.Equal(2, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)));
        Assert.Equal(1, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
    }

    [Fact]
    public void Encode_BgraPixels_StoresRgbScanlinesWithFilterByte()
    {
        var png = PngEncoder.Encode(TwoPixelsBgra, width: 2, height: 1);

        var idatLength = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(33));
        using var inflater = new ZLibStream(new MemoryStream(png, 41, idatLength), CompressionMode.Decompress);
        using var raw = new MemoryStream();
        inflater.CopyTo(raw);
        Assert.Equal([0, 0, 0, 255, 255, 0, 0], raw.ToArray());
    }
}
