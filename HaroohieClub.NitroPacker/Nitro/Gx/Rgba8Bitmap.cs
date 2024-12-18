using System;

namespace HaroohieClub.NitroPacker.Nitro.Gx;

/// <summary>
/// An RGBA8 bitmap
/// </summary>
public class Rgba8Bitmap
{
    /// <summary>
    /// Width of the bitmap in pixels
    /// </summary>
    public int Width { get; }
    /// <summary>
    /// Height of the bitmap in pixels
    /// </summary>
    public int Height { get; }
    /// <summary>
    /// A representation of the bitmap's pixel data
    /// </summary>
    public uint[] Pixels { get; }

    /// <summary>
    /// Constructs a blank bitmap with a given width and height
    /// </summary>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    public Rgba8Bitmap(int width, int height)
    {
        Width = width;
        Height = height;

        Pixels = new uint[width * height];
    }

    /// <summary>
    /// Constructs a bitmap from data
    /// </summary>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    /// <param name="data">Pixel data</param>
    public Rgba8Bitmap(int width, int height, uint[] data)
    {
        Width = width;
        Height = height;

        Pixels = new uint[width * height];
        Array.Copy(data, Pixels, Pixels.Length);
    }

    /// <summary>
    /// Gets pixel data at an x-y coordinate
    /// </summary>
    /// <param name="x">x-coordinate</param>
    /// <param name="y">y-coordinate</param>
    public uint this[int x, int y]
    {
        get => Pixels[y * Width + x];
        set => Pixels[y * Width + x] = value;
    }
}