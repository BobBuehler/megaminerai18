﻿using Joueur.cs.Games.Saloon;
using System;
using System.Collections.Generic;
using System.Linq;

static class Solver
{
    public static void SwarmPianos()
    {
        while (true)
        {
            var pianos = AI._Game.Furnishings.Where(f => f.IsPiano && !f.IsDestroyed && !f.IsPlaying)
                .Select(t => t.ToPoint())
                .ToHashSet();
            var cowboys = AI._Player.Cowboys.Where(c => !c.IsDead && !c.IsDrunk && c.CanMove && c.TurnsBusy == 0)
                .Select(c => c.ToPoint())
                .ToList();

            if (pianos.Count == 0 || cowboys.Count() == 0)
            {
                break;
            }

            var nth = Math.Min(pianos.Count, cowboys.Count);

            var paths = new List<IEnumerable<Tile>>(nth);
            
            for (int i = 0; i < nth; i++)
            {
                var shortPath = PathSafely(cowboys, p => pianos.Contains(p));
                if (shortPath.Count() == 0)
                {
                    break;
                }
                paths.Add(shortPath);
                pianos.Remove(shortPath.Last().ToPoint());
            }

            if (paths.Count > 0)
            {
                var path = paths.Last();
                MoveAndPlay(path);
                var piano = path.Last();
                pianos.Remove(piano.ToPoint());
            }
            else
            {
                break;
            }
        }
    }

    public static void MoveAndPlay(IEnumerable<Tile> path)
    {
        var cowboy = path.First().Cowboy;
        if (path.Count() > 2)
        {
            Console.WriteLine("Move [{0}]", String.Join(",", path.Select(t => t.Stringify()).ToArray()));
            cowboy.Move(path.ElementAt(1));
        }
        if (path.Count() <= 3)
        {
            Console.WriteLine("Play [{0}]", String.Join(",", path.Select(t => t.Stringify()).ToArray()));
            cowboy.Play(path.Last().Furnishing);
        }
    }

    public static void BeSafe(Cowboy cowboy)
    {
        if (!cowboy.CanMove)
        {
            return;
        }

        var autoStates = AutoStates(2).ToList();
        var point = cowboy.ToPoint();
        if (IsSafe(point, autoStates[0]) && IsSafe(point, autoStates[1]))
        {
            return;
        }

        var walkableAndSafe = Neighboors(point)
            .Where(p => Solver.IsWalkable(p) && IsSafe(p, autoStates[0]) && IsSafe(p, autoStates[1]))
            .ToList();
        if (walkableAndSafe.Any())
        {
            var safeTile = walkableAndSafe.First().ToTile();
            Console.WriteLine("Be safe: [{0},{1}]", cowboy.Tile.Stringify(), safeTile.Stringify());
            cowboy.Move(safeTile);
        }
    }

    public static IEnumerable<Tile> PathSafely(IEnumerable<Point> starts, Func<Point, bool> isGoal)
    {
        var autoStates = AutoStates(AI._Game.MaxTurns - AI._Game.CurrentTurn + 1).ToDictionary(s => s.Turn);

        var astar = new AStar<PointAtTurn>(
            starts.Select(p => new PointAtTurn(p, AI._Game.CurrentTurn)),
            pat => isGoal(pat.Point),
            (pat1, pat2) => pat2.Point.ToTile().HasHazard ? 4 : 2,
            pat => 0,
            pat =>
            {
                if (pat.Turn >= AI._Game.MaxTurns)
                {
                    return Enumerable.Empty<PointAtTurn>();
                }

                var neighboors = Neighboors(pat.Point);
                // Console.WriteLine("Direct: " + String.Join(" ", neighboors.Select(n => n.ToTile().Stringify()).ToArray()));
                var filtered = neighboors
                    .Where(p => isGoal(p) || (IsWalkable(p) && IsSafe(p, autoStates[pat.Turn]) && IsSafe(p, autoStates[pat.Turn + 1])));
                // Console.WriteLine("Filter: " + String.Join(" ", filtered.Select(n => n.ToTile().Stringify()).ToArray()));

                return filtered.Select(p => new PointAtTurn(p, pat.Turn + 2));
            }
        );
        
        return astar.Path.Select(pat => pat.Point.ToTile());
    }

    public static IEnumerable<Point> WalkingNeighboors(Point point)
    {
        return Neighboors(point).Where(p => IsWalkable(p));
    }

    public static bool IsWalkable(Point point)
    {
        var tile = point.ToTile();
        var isWalkable = tile.Cowboy == null && tile.Furnishing == null && !tile.IsBalcony;
        // Console.WriteLine("IsWalkable={0}: {1}, {2}", isWalkable, point, tile.Stringify());
        return isWalkable;
    }

    public static bool IsSafe(Point point, AutoState state)
    {
        if (state.Bottles.ContainsKey(point))
        {
            return false;
        }
        if (!state.IsOurTurn && point.Equals(state.TheirYoungGun))
        {
            return false;
        }
        return true;
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
        Console.WriteLine("NextPoint Bad Direction: " + direction);
        return new Point(0, 0);
    }

    public static Point CallInPoint(Point youngGun)
    {
        var point = new Point(youngGun.x, youngGun.y);

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

        public override string ToString()
        {
            return String.Format("{0}{1}: o{2}-{3} t{4}-{5}, b[{6}]",
                Turn,
                IsOurTurn ? "o" : "t",
                OurYoungGun,
                OurCallIn,
                TheirYoungGun,
                TheirCallIn,
                String.Join(",", Bottles.Keys.ToArray()));
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
            .Where(kvp => IsBottlePathable(kvp.Key))
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
