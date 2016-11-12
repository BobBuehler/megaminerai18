using Joueur.cs.Games.Saloon;

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
}