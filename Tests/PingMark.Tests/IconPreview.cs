using System;
using System.IO;
using System.IO.Compression;
using JoksterCube.PingMark.Domain;
using UnityEngine;
using Xunit;

namespace JoksterCube.PingMark.Tests;

// Renders the Perception and Fishsight icons with the mod's own drawing code, for the README. Runs only when PINGMARK_ICON_OUT (a
// folder) is set; otherwise it does nothing. Usage: set PINGMARK_ICON_OUT, then
// dotnet test Tests/PingMark.Tests --filter Render_perception_icon (after a Release build).
public class IconPreview
{
    [Fact]
    public void Render_perception_icon()
    {
        string? output = Environment.GetEnvironmentVariable("PINGMARK_ICON_OUT");
        if (string.IsNullOrEmpty(output)) return;
        Directory.CreateDirectory(output);
        WritePng(Path.Combine(output, "Perception.png"), PerceptionIcon.Render(), 64, 64);
        foreach (FishTagIcon.Variant variant in Enum.GetValues(typeof(FishTagIcon.Variant)))
            WritePng(Path.Combine(output, $"FishTag_{variant}.png"), FishTagIcon.Render(variant), 64, 64);
    }

    [Fact]
    public void Fish_tag_icon_is_drawn_inside_its_edges()
    {
        Color[] colors = FishTagIcon.Render();
        Assert.Equal(64 * 64, colors.Length);
        // The tag fills the middle; the corners stay clear so the HUD frame shows around it.
        Assert.True(colors[34 * 64 + 40].a > 0.99f);
        foreach (int corner in new[] { 63, 63 * 64, 64 * 64 - 1 }) Assert.True(colors[corner].a < 0.05f);
    }

    [Fact]
    public void Perception_icon_is_drawn_inside_its_edges()
    {
        Color[] colors = PerceptionIcon.Render();
        Assert.Equal(64 * 64, colors.Length);
        // The middle is the eye, solid; the corners stay clear so the HUD frame shows around it.
        Assert.True(colors[32 * 64 + 32].a > 0.99f);
        foreach (int corner in new[] { 0, 63, 63 * 64, 64 * 64 - 1 }) Assert.True(colors[corner].a < 0.02f);
    }

    // A plain 8-bit RGBA PNG, row 0 at the top.
    private static void WritePng(string path, Color[] colors, int width, int height)
    {
        byte[] raw = new byte[height * (width * 4 + 1)];
        for (int y = 0; y < height; y++)
        {
            int row = y * (width * 4 + 1);
            for (int x = 0; x < width; x++)
            {
                Color c = colors[y * width + x];
                int i = row + 1 + x * 4;
                raw[i] = Byte(c.r);
                raw[i + 1] = Byte(c.g);
                raw[i + 2] = Byte(c.b);
                raw[i + 3] = Byte(c.a);
            }
        }
        using MemoryStream compressed = new();
        using (ZLibStream zlib = new(compressed, System.IO.Compression.CompressionLevel.Optimal, true)) zlib.Write(raw);

        using BinaryWriter writer = new(File.Create(path));
        writer.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        byte[] header = new byte[13];
        BigEndian(header, 0, width);
        BigEndian(header, 4, height);
        header[8] = 8;
        header[9] = 6;
        Chunk(writer, "IHDR", header);
        Chunk(writer, "IDAT", compressed.ToArray());
        Chunk(writer, "IEND", []);
    }

    private static void Chunk(BinaryWriter writer, string type, byte[] data)
    {
        byte[] length = new byte[4];
        BigEndian(length, 0, data.Length);
        writer.Write(length);
        byte[] body = new byte[4 + data.Length];
        for (int i = 0; i < 4; i++) body[i] = (byte)type[i];
        data.CopyTo(body, 4);
        writer.Write(body);
        byte[] crc = new byte[4];
        BigEndian(crc, 0, (int)Crc(body));
        writer.Write(crc);
    }

    private static uint Crc(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
        }
        return ~crc;
    }

    private static void BigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static byte Byte(float value) => (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
}
