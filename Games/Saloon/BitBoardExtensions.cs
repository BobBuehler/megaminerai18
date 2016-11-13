using System;
using System.Collections.Generic;
using System.Collections;

static class BitBoardExtensions
{
    public static int Width = 22;
    public static int Height = 12;

    public static int GetOffset(int x, int y)
    {
        return y * Width + x;
    }

    public static bool Get(this BitArray bits, int x, int y)
    {
        return bits.Get(GetOffset(x, y));
    }

    public static bool Get(this BitArray bits, Point p)
    {
        return bits.Get(p.x, p.y);
    }

    public static void Set(this BitArray bits, int x, int y, bool value)
    {
        bits.Set(GetOffset(x, y), value);
    }

    public static void Set(this BitArray bits, Point p, bool value)
    {
        bits.Set(p.x, p.y, value);
    }

    public static Func<Point, bool> ToFunc(this BitArray bits)
    {
        return p => bits.Get(p);
    }

    public static IEnumerable<Point> ToPoints(this BitArray bits)
    {
        for (int x = 0; x < Width; ++x)
        {
            for (int y = 0; y < Height; ++y)
            {
                if (bits.Get(x, y))
                {
                    yield return new Point(x, y);
                }
            }
        }
    }

    public static BitArray ToBitArray(this IEnumerable<Point> points)
    {
        var bits = new BitArray(Width * Height);
        points.ForEach(p => bits.Set(p, true));
        return bits;
    }

    public static int ManhattanDistance(this Point source, Point target)
    {
        return Math.Abs(source.x - target.x) + Math.Abs(source.y - target.y);
    }

    public static bool IsInRange(this Point source, int range, Point target)
    {
        return range >= source.ManhattanDistance(target);
    }

    public static int Count(this BitArray bits)
    {
        int count = 0;
        for (int i = 0; i < bits.Length; ++i)
        {
            if (bits[i])
            {
                ++count;
            }
        }
        return count;
    }

    public static IEnumerable<Point> PointsInRange(this Point source, int range)
    {
        for (int dy = -range; dy <= range; ++dy)
        {
            int xRange = range - Math.Abs(dy);
            for (int dx = -xRange; dx <= xRange; ++dx)
            {
                yield return new Point(source.x + dx, source.y + dy);
            }
        }
    }
}