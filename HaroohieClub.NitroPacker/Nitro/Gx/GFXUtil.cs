using System;
using System.Drawing;

namespace HaroohieClub.NitroPacker.Nitro.Gx;

/// <summary>
/// Graphics utility
/// </summary>
public class GfxUtil
{
    /// <summary>
    /// Converts between color formats
    /// </summary>
    /// <param name="inColor">Input color</param>
    /// <param name="inputFormat">Input color format</param>
    /// <param name="outputFormat">Output color format</param>
    /// <returns>uint representation of the color</returns>
    public static uint[] ConvertColorFormat(ReadOnlySpan<uint> inColor, ColorFormat inputFormat, ColorFormat outputFormat)
    {
        uint[] output = new uint[inColor.Length];
        for (int i = 0; i < inColor.Length; i++)
            output[i] = ConvertColorFormat(inColor[i], inputFormat, outputFormat);
        return output;
    }

    /// <summary>
    /// Converts between color formats
    /// </summary>
    /// <param name="inColor">Input color</param>
    /// <param name="inputFormat">Input color format</param>
    /// <param name="outputFormat">Output color format</param>
    /// <returns>uint representation of the color</returns>
    public static uint[] ConvertColorFormatFromU16(ReadOnlySpan<ushort> inColor, ColorFormat inputFormat,
        ColorFormat outputFormat)
    {
        uint[] output = new uint[inColor.Length];
        for (int i = 0; i < inColor.Length; i++)
            output[i] = ConvertColorFormat(inColor[i], inputFormat, outputFormat);
        return output;
    }

    /// <summary>
    /// Converts between color formats
    /// </summary>
    /// <param name="inColor">Input color</param>
    /// <param name="inputFormat">Input color format</param>
    /// <param name="outputFormat">Output color format</param>
    /// <returns>ushort representation of the color</returns>
    public static ushort[] ConvertColorFormatToU16(ReadOnlySpan<uint> inColor, ColorFormat inputFormat,
        ColorFormat outputFormat)
    {
        ushort[] output = new ushort[inColor.Length];
        for (int i = 0; i < inColor.Length; i++)
            output[i] = (ushort)ConvertColorFormat(inColor[i], inputFormat, outputFormat);
        return output;
    }

    /// <summary>
    /// Converts between color formats
    /// </summary>
    /// <param name="inColor">Input color</param>
    /// <param name="inputFormat">Input color format</param>
    /// <param name="outputFormat">Output color format</param>
    /// <returns>ushort representation of the color</returns>
    public static ushort[] ConvertColorFormatU16(ReadOnlySpan<ushort> inColor, ColorFormat inputFormat,
        ColorFormat outputFormat)
    {
        ushort[] output = new ushort[inColor.Length];
        for (int i = 0; i < inColor.Length; i++)
            output[i] = (ushort)ConvertColorFormat(inColor[i], inputFormat, outputFormat);
        return output;
    }

    /// <summary>
    /// Converts between color formats
    /// </summary>
    /// <param name="inColor">Input color</param>
    /// <param name="inputFormat">Input color format</param>
    /// <param name="outputFormat">Output color format</param>
    /// <returns>uint representation of the color</returns>
    public static uint ConvertColorFormat(uint inColor, ColorFormat inputFormat, ColorFormat outputFormat)
    {
        if (inputFormat == outputFormat)
            return inColor;
        //From color format to components:
        uint a, mask;
        if (inputFormat.ASize == 0)
            a = 255;
        else
        {
            mask = ~(0xFFFFFFFFu << inputFormat.ASize);
            a = ((inColor >> inputFormat.AShift & mask) * 255u + mask / 2) / mask;
        }

        mask = ~(0xFFFFFFFFu << inputFormat.RSize);
        uint r = ((inColor >> inputFormat.RShift & mask) * 255u + mask / 2) / mask;
        mask = ~(0xFFFFFFFFu << inputFormat.GSize);
        uint g = ((inColor >> inputFormat.GShift & mask) * 255u + mask / 2) / mask;
        mask = ~(0xFFFFFFFFu << inputFormat.BSize);
        uint b = ((inColor >> inputFormat.BShift & mask) * 255u + mask / 2) / mask;
        return ToColorFormat(a, r, g, b, outputFormat);
    }

    /// <summary>
    /// Converts from components to a particular color format
    /// </summary>
    /// <param name="r">Red</param>
    /// <param name="g">Green</param>
    /// <param name="b">Blue</param>
    /// <param name="outputFormat">Output color format</param>
    /// <returns>unit representation of teh color</returns>
    public static uint ToColorFormat(int r, int g, int b, ColorFormat outputFormat)
        => ToColorFormat(255u, (uint)r, (uint)g, (uint)b, outputFormat);

    /// <summary>
    /// Converts from components to a particular color format
    /// </summary>
    /// <param name="a">Alpha</param>
    /// <param name="r">Red</param>
    /// <param name="g">Green</param>
    /// <param name="b">Blue</param>
    /// <param name="outputFormat">Output color format</param>
    /// <returns>uint representation of the color</returns>
    public static uint ToColorFormat(int a, int r, int g, int b, ColorFormat outputFormat)
        => ToColorFormat((uint)a, (uint)r, (uint)g, (uint)b, outputFormat);

    /// <summary>
    /// Converts from components to a particular color format
    /// </summary>
    /// <param name="r">Red</param>
    /// <param name="g">Green</param>
    /// <param name="b">Blue</param>
    /// <param name="outputFormat">Output color format</param>
    /// <returns>unit representation of teh color</returns>
    public static uint ToColorFormat(uint r, uint g, uint b, ColorFormat outputFormat)
        => ToColorFormat(255u, r, g, b, outputFormat);

    /// <summary>
    /// Converts from components to a particular color format
    /// </summary>
    /// <param name="a">Alpha</param>
    /// <param name="r">Red</param>
    /// <param name="g">Green</param>
    /// <param name="b">Blue</param>
    /// <param name="outputFormat">Output color format</param>
    /// <returns>uint representation of the color</returns>
    public static uint ToColorFormat(uint a, uint r, uint g, uint b, ColorFormat outputFormat)
    {
        uint result = 0;
        uint mask;
        if (outputFormat.ASize != 0)
        {
            mask = ~(0xFFFFFFFFu << outputFormat.ASize);
            result |= (a * mask + 127u) / 255u << outputFormat.AShift;
        }

        mask = ~(0xFFFFFFFFu << outputFormat.RSize);
        result |= (r * mask + 127u) / 255u << outputFormat.RShift;
        mask = ~(0xFFFFFFFFu << outputFormat.GSize);
        result |= (g * mask + 127u) / 255u << outputFormat.GShift;
        mask = ~(0xFFFFFFFFu << outputFormat.BSize);
        result |= (b * mask + 127u) / 255u << outputFormat.BShift;
        return result;
    }

    /// <summary>
    /// Sets the alpha component for a color
    /// </summary>
    /// <param name="color">Color</param>
    /// <param name="a">Alpha to set</param>
    /// <param name="format">The color format</param>
    /// <returns>uint representation fo the new color</returns>
    public static uint SetAlpha(uint color, uint a, ColorFormat format)
    {
        uint result = color;
        if (format.ASize == 0)
            return color;
        uint mask = ~(0xFFFFFFFFu << format.ASize);
        result &= ~(mask << format.AShift);
        result |= (a * mask + 127u) / 255u << format.AShift;
        return result;
    }

    /// <summary>
    /// Interpolates between two colors
    /// </summary>
    /// <param name="colorA">The first color</param>
    /// <param name="factorA">The weight for the first color</param>
    /// <param name="colorB">The second color</param>
    /// <param name="factorB">The weight for the second color</param>
    /// <param name="format">The color format</param>
    /// <returns>The interpolated color</returns>
    public static uint InterpolateColor(uint colorA, int factorA, uint colorB, int factorB, ColorFormat format)
    {
        uint aa, ra, ga, ba;
        uint mask;
        if (format.ASize == 0)
            aa = 255;
        else
        {
            mask = ~(0xFFFFFFFFu << format.ASize);
            aa = ((colorA >> format.AShift & mask) * 255u + mask / 2) / mask;
        }

        mask = ~(0xFFFFFFFFu << format.RSize);
        ra = ((colorA >> format.RShift & mask) * 255u + mask / 2) / mask;
        mask = ~(0xFFFFFFFFu << format.GSize);
        ga = ((colorA >> format.GShift & mask) * 255u + mask / 2) / mask;
        mask = ~(0xFFFFFFFFu << format.BSize);
        ba = ((colorA >> format.BShift & mask) * 255u + mask / 2) / mask;
        uint ab, rb, gb, bb;
        if (format.ASize == 0)
            ab = 255;
        else
        {
            mask = ~(0xFFFFFFFFu << format.ASize);
            ab = ((colorB >> format.AShift & mask) * 255u + mask / 2) / mask;
        }

        mask = ~(0xFFFFFFFFu << format.RSize);
        rb = ((colorB >> format.RShift & mask) * 255u + mask / 2) / mask;
        mask = ~(0xFFFFFFFFu << format.GSize);
        gb = ((colorB >> format.GShift & mask) * 255u + mask / 2) / mask;
        mask = ~(0xFFFFFFFFu << format.BSize);
        bb = ((colorB >> format.BShift & mask) * 255u + mask / 2) / mask;
        return ToColorFormat(
            (uint)((aa * factorA + ab * factorB) / (factorA + factorB)),
            (uint)((ra * factorA + rb * factorB) / (factorA + factorB)),
            (uint)((ga * factorA + gb * factorB) / (factorA + factorB)),
            (uint)((ba * factorA + bb * factorB) / (factorA + factorB)), format);
    }

    /// <summary>
    /// Converts colors to uints
    /// </summary>
    /// <param name="colors">A set of colors to convert</param>
    /// <returns>The uint representations of the inputted colors</returns>
    public static uint[] ColorToU32(ReadOnlySpan<Color> colors)
    {
        var result = new uint[colors.Length];
        for (int i = 0; i < colors.Length; i++)
            result[i] = (uint)colors[i].ToArgb();
        return result;
    }

    /// <summary>
    /// Converts uints to colors
    /// </summary>
    /// <param name="colors">uint representations of the colors</param>
    /// <returns>The converted colors</returns>
    public static Color[] U32ToColor(ReadOnlySpan<uint> colors)
    {
        var result = new Color[colors.Length];
        for (int i = 0; i < colors.Length; i++)
            result[i] = Color.FromArgb((int)colors[i]);
        return result;
    }

    /// <summary>
    /// Untiles image data
    /// </summary>
    /// <param name="data">The image data to untile</param>
    /// <param name="tileSize">The size of each tile</param>
    /// <param name="width">The width of each tile</param>
    /// <param name="height">The height of each tile</param>
    /// <returns>The untiled image data</returns>
    public static uint[] Untile(ReadOnlySpan<uint> data, int tileSize, int width, int height)
    {
        uint[] result = new uint[width * height];
        int offset = 0;
        for (int y = 0; y < height; y += tileSize)
        for (int x = 0; x < width; x += tileSize)
        for (int y2 = 0; y2 < tileSize; y2++)
        for (int x2 = 0; x2 < tileSize; x2++)
            result[(y + y2) * width + x + x2] = data[offset++];
        return result;
    }
}