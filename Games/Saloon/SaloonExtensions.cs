using Joueur.cs.Games.Saloon;
using System;

static class SaloonExtensions
{
    public static Point ToPoint(this Tile tile)
    {
        return new Point(tile.X, tile.Y);
    }

    public static Point ToPoint(this Cowboy cowboy)
    {
        return cowboy.Tile.ToPoint();
    }

    public static Point ToPoint(this Furnishing furnishing)
    {
        return furnishing.Tile.ToPoint();
    }

    public static Tile ToTile(this Point point)
    {
        return AI._Game.GetTileAt(point.x, point.y);
    }

    public static string Stringify(this Tile tile)
    {
        var desc = "EMPTY";
        if (tile.IsBalcony)
        {
            desc = "BALCONY";
        }
        else if (tile.Furnishing != null)
        {
            desc = tile.Furnishing.IsPiano ? "PIANO" : "TABLE";
        }
        else
        {
            if (tile.Bottle != null)
            {
                desc = "BOTTLE";
            }
            else if (tile.Cowboy != null)
            {
                desc = "COWBOY";
            }

            if (tile.HasHazard)
            {
                desc += "+HAZARD";
            }
        }
        return String.Format("{0}={1}", tile.ToPoint(), desc);
    }
}