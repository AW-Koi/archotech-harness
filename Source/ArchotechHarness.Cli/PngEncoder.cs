using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace ArchotechHarness.Cli;

/// <summary>Encodes top-down 32-bit BGRA pixels as an RGB PNG.</summary>
public static class PngEncoder
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] Encode(byte[] bgra, int width, int height)
    {
        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 2;
        WriteChunk(output, "IHDR", header);

        using (var compressed = new MemoryStream())
        {
            using (var deflater = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
            {
                var row = new byte[1 + (width * 3)];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var source = ((y * width) + x) * 4;
                        row[1 + (x * 3)] = bgra[source + 2];
                        row[2 + (x * 3)] = bgra[source + 1];
                        row[3 + (x * 3)] = bgra[source];
                    }

                    deflater.Write(row);
                }
            }

            WriteChunk(output, "IDAT", compressed.ToArray());
        }

        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        var length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);
        var typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes);
        output.Write(data);

        var crc = 0xFFFFFFFFu;
        foreach (var b in typeBytes.Concat(data))
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        var crcBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc ^ 0xFFFFFFFFu);
        output.Write(crcBytes);
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
