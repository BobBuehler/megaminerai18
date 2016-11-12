using Joueur.cs.Games.Saloon;
using System;
using System.Collections.Generic;
using System.Linq;

static class Solver
{
    public static IEnumerable<Tile> PathSafely(IEnumerable<Point> starts, Func<Point, bool> isGoal)
    {
        var autoStates = AutoStates(40).ToDictionary(s => s.Turn);

        var astar = new AStar<PointAtTurn>(
            starts.Select(p => new PointAtTurn(p, AI._Game.CurrentTurn)),
            pat => isGoal(pat.Point),
            (pat1, pat2) => pat2.Point.ToTile().HasHazard ? 2 : 1,
            pat => 0,
            pat => SafeNeighboors(pat.Point, autoStates[pat.Turn], autoStates[pat.Turn + 1]).Select(p => new PointAtTurn(p, pat.Turn + 2))
        );

        return astar.Path.Select(pat => pat.Point.ToTile());
    }

    public static IEnumerable<Point> SafeNeighboors(Point start, AutoState state, AutoState nextState)
    {
        return Neighboors(start).Where(p => IsSafe(p, state) && IsSafe(p, nextState));
    }

    public static bool IsSafe(Point point, AutoState state)
    {
        return !state.OurCallIn.Equals(point) && !state.TheirCallIn.Equals(point) && !state.Bottles.ContainsKey(point);
    }

    public static IEnumerable<Point> Neighboors(Point point)
    {
        yield return new Point(point.x, point.y - 1);
        yield return new Point(point.x + 1, point.y);
        yield return new Point(point.x, point.y + 1);
        yield return new Point(point.x - 1, point.y);
    }

    public static IEnumerable<Point> CallInPoints(Point youngGun, int count)
    {
        return YoungGunPoints(youngGun, count).Select(p => CallInPoint(p));
    }

    public static IEnumerable<Point> YoungGunPoints(Point youngGun, int count)
    {
        yield return youngGun;
        for (int i = 1; i < count; ++count)
        {
            youngGun = NextYoungGunPoint(youngGun);
            yield return youngGun;
        }
    }

    public static Point NextYoungGunPoint(Point point)
    {
        return NextPoint(point, YoungGunDirection(point));
    }

    public static string YoungGunDirection(Point point)
    {
        if (point.y == 0)
        {
            if (point.x < AI._Game.MapWidth - 1)
            {
                return "East";
            }
            else
            {
                return "South";
            }
        }
        else if (point.y == AI._Game.MapHeight - 1)
        {
            if (point.x > 0)
            {
                return "West";
            }
            else
            {
                return "North";
            }
        }
        else if (point.x == 0)
        {
            return "North";
        }
        else
        {
            return "South";
        }
    }

    public static Dictionary<int, HashSet<Point>> BottleStates()
    {
        var bottleStates = new Dictionary<int, HashSet<Point>>();
        foreach (var bottle in AI._Game.Bottles)
        {
            int turn = AI._Game.CurrentTurn;
            foreach (var point in BottlePath(bottle))
            {
                HashSet<Point> pointSet;
                if (!bottleStates.TryGetValue(turn, out pointSet))
                {
                    pointSet = new HashSet<Point>();
                    bottleStates[turn] = pointSet;
                }
                pointSet.Add(point);
            }
        }
        return bottleStates;
    }

    public static IEnumerable<Point> BottlePath(Bottle bottle)
    {
        var point = bottle.Tile.ToPoint();
        var bottlePath = new List<Point>() { point };
        while (true)
        {
            point = NextPoint(point, bottle.Direction);
            if (IsBottlePathable(point))
            {
                bottlePath.Add(point);
            }
            else
            {
                break;
            }
        }
        return bottlePath;
    }

    public static bool IsBottlePathable(Point point)
    {
        var tile = point.ToTile();
        return !tile.IsBalcony && tile.Furnishing == null && tile.Cowboy == null;
    }

    public static Point NextPoint(Point point, string direction)
    {
        switch(direction)
        {
            case "North":
                return new Point(point.x, point.y - 1);
            case "East":
                return new Point(point.x + 1, point.y);
            case "South":
                return new Point(point.x, point.y + 1);
            case "West":
                return new Point(point.x - 1, point.y);
        }
        return new Point(0, 0); // BAD
    }

    public static Point CallInPoint(Point youngGun)
    {
        var point = new Point();

        if (youngGun.x == 0)
        {
            point.x = 1;
        }
        else if (youngGun.x == AI._Game.MapWidth - 1)
        {
            point.x = youngGun.x - 1;
        }

        if (youngGun.y == 0)
        {
            point.y = 1;
        }
        else if (youngGun.y == AI._Game.MapHeight - 1)
        {
            point.y = youngGun.y - 1;
        }

        return point;
    }

    public class AutoState
    {
        public int Turn { get; set; }
        public bool IsOurTurn { get; set; }
        public Point OurYoungGun { get; set; }
        public Point OurCallIn { get { return CallInPoint(OurYoungGun); } }
        public Point TheirYoungGun { get; set; }
        public Point TheirCallIn { get { return CallInPoint(TheirYoungGun); } }
        public Dictionary<Point, Bottle> Bottles { get; set; } // Includes bottes on Points where they are smashing

        public AutoState(int turn, bool isOurTurn, Point ourYoungGun, Point theirYoungGun)
        {
            Turn = turn;
            IsOurTurn = isOurTurn;
            OurYoungGun = ourYoungGun;
            TheirYoungGun = theirYoungGun;
        }
    }

    public static IEnumerable<AutoState> AutoStates(int count)
    {
        var state = new AutoState(AI._Game.CurrentTurn, true, AI._Player.YoungGun.Tile.ToPoint(), AI._OtherPlayer.YoungGun.Tile.ToPoint());
        state.Bottles = AI._Game.Bottles.ToDictionary(b => b.Tile.ToPoint());

        yield return state;

        for (int i = 1; i < count; i++)
        {
            state = NextState(state);
            yield return state;
        }
    }

    public static AutoState NextState(AutoState state)
    {
        var nextState = new AutoState(
            state.Turn + 1,
            !state.IsOurTurn,
            state.IsOurTurn ? state.OurYoungGun : NextYoungGunPoint(state.OurYoungGun),
            state.IsOurTurn ? NextYoungGunPoint(state.TheirYoungGun) : state.TheirYoungGun
        );
        nextState.Bottles = state.Bottles
            .Where(kvp => !kvp.Key.ToTile().IsBalcony && kvp.Key.ToTile().Furnishing == null)
            .ToDictionary(kvp => NextPoint(kvp.Key, kvp.Value.Direction), kvp => kvp.Value);

        return nextState;
    }

    public class PointAtTurn
    {
        public Point Point { get; set; }
        public int Turn { get; set; }

        public PointAtTurn(Point point, int turn)
        {
            Point = point;
            Turn = turn;
        }

        public override bool Equals(object obj)
        {
            return obj is PointAtTurn
                && ((PointAtTurn)obj).Point.Equals(Point)
                && ((PointAtTurn)obj).Turn == Turn;
        }

        public override int GetHashCode()
        {
            return Turn + Point.GetHashCode();
        }
    }
}
