using Joueur.cs.Games.Saloon;
using System;
using System.Collections.Generic;
using System.Linq;

static class Solver
{
    public static void GreedySwarmAndPlay()
    {
        var assignedPianos = new HashSet<Point>();
        var movingCowboys = new HashSet<Point>();
        var assignedGoals = new HashSet<Point>();
        while (true)
        {
            var pianos = AI._Game.Furnishings.Where(f => f.IsPiano && !f.IsDestroyed && !f.IsPlaying && !assignedPianos.Contains(f.ToPoint()))
                .Select(t => t.ToPoint())
                .ToHashSet();
            var cowboys = AI._Player.Cowboys.Where(c => !c.IsDead && !c.IsDrunk && c.CanMove && !movingCowboys.Contains(c.ToPoint()))
                .Select(c => c.ToPoint())
                .ToHashSet();

            if (pianos.Count == 0 || cowboys.Count == 0)
            {
                break;
            }

            var paths = cowboys.ToDictionary(c => c, c => PathSafely(new[] { c }, pianos, p => movingCowboys.Contains(p), p => assignedGoals.Contains(p))).Where(kvp => kvp.Value.Count() > 0);
            if (paths.Any())
            {
                var path = paths.MinByValue(kvp => Math.Max((kvp.Value.Count() - 2) * 2, kvp.Key.ToTile().Cowboy.TurnsBusy)).Value.ToList();
                var cowboy = path.First().Cowboy;
                if (!CornerCase(path))
                {
                    MoveAndPlay(path);
                }
                movingCowboys.Add(cowboy.ToPoint());
                assignedGoals.Add(path[path.Count - 2].ToPoint());
                assignedPianos.Add(path[path.Count - 1].ToPoint());
                AI._IsAPlayer.Add(cowboy.ToPoint());
            }
            else
            {
                break;
            }
        }
    }

    public static bool CornerCase(List<Tile> playPath)
    {
        if (playPath.Count == 2 && playPath[0].HasHazard)
        {
            Console.WriteLine("Maybe Corner: " + playPath[0].Stringify());
            var cowboy = playPath[0].Cowboy;
            var piano = playPath[1].Furnishing;
            //var autoStates = AutoStates(3).ToList();
            var newPositions = Neighboors(piano.ToPoint())
                .Where(n => n.ManhattanDistance(cowboy.ToPoint()) == 2)
                .Where(n => !n.ToTile().HasHazard && IsWalkable(n));
            if (!newPositions.Any())
            {
                return false;
            }
            var newPath = PathSafely(new[] { cowboy.ToPoint() }, newPositions);
            if (newPath.Count() == 3)
            {
                Console.WriteLine("Corner Case: {0} -> {1}", newPath.ElementAt(0).Stringify(), newPath.ElementAt(1).Stringify());
                cowboy.Play(piano);
                cowboy.Move(newPath.ElementAt(1));
                return true;
            }
        }
        return false;
    }

    public static void MoveAndPlay(IEnumerable<Tile> path)
    {
        var cowboy = path.First().Cowboy;
        if (cowboy.CanMove && path.Count() > 2)
        {
            Console.WriteLine("Move [{0}]", String.Join(",", path.Select(t => t.Stringify()).ToArray()));
            cowboy.Move(path.ElementAt(1));
        }
        if (cowboy.TurnsBusy == 0 && path.Count() <= 3)
        {
            Console.WriteLine("Play [{0}]", String.Join(",", path.Select(t => t.Stringify()).ToArray()));
            cowboy.Play(path.Last().Furnishing);
        }
    }

    public static void BeSafe(Cowboy cowboy)
    {
        if (!cowboy.CanMove || cowboy.IsDrunk || cowboy.IsDead)
        {
            return;
        }

        var autoStates = AutoStates(3).ToList();
        Func<Point, bool> isGood = p =>
        {
            if (!AI._IsAPlayer.Contains(cowboy.ToPoint()))
            {
                if (HasHazard(p))
                {
                    return false;
                }
                if (Neighboors(p).Any(n => HasCowboy(n, "Brawler")))
                {
                    return false;
                }
            }
            return IsSafe(p, autoStates[0]) && IsSafe(p, autoStates[1]) && IsSafe(p, autoStates[2]);
        };

        var point = cowboy.ToPoint();
        if (isGood(point))
        {
            return;
        }

        var walkableAndSafe = Neighboors(point)
            .Where(p => Solver.IsWalkable(p) && isGood(p))
            .ToList();
        if (walkableAndSafe.Any())
        {
            var safeTile = walkableAndSafe.First().ToTile();
            Console.WriteLine("Be safe: [{0},{1}]", cowboy.Tile.Stringify(), safeTile.Stringify());
            cowboy.Move(safeTile);
            cowboy.LogScared();
        }
    }

    public static bool HasHazard(Point point)
    {
        return point.ToTile().HasHazard;
    }

    public static bool HasCowboy(Point point, string job)
    {
        var tile = point.ToTile();
        return tile.Cowboy != null && tile.Cowboy.Job == job;
    }

    public static IEnumerable<Point> SafePathSingle(Point start, Point goal, Dictionary<int, AutoState> autoStates)
    {
        var astar = new AStar<PointAtTurn>(
            new [] { new PointAtTurn(start, AI._Game.CurrentTurn) },
            pat => pat.Point.Equals(goal),
            (pat1, pat2) => pat2.Point.ToTile().HasHazard ? 6 : 2,
            pat => pat.Point.ManhattanDistance(goal),
            pat =>
            {
                if (pat.Turn >= AI._Game.MaxTurns)
                {
                    return Enumerable.Empty<PointAtTurn>();
                }

                var neighboors = Neighboors(pat.Point);
                // Console.WriteLine("Direct: " + String.Join(" ", neighboors.Select(n => n.ToTile().Stringify()).ToArray()));
                var filtered = neighboors
                    .Where(p => p.Equals(goal) || (IsWalkable(p) && IsSafe(p, autoStates[pat.Turn]) && IsSafe(p, autoStates[pat.Turn + 1])));
                // Console.WriteLine("Filter: " + String.Join(" ", filtered.Select(n => n.ToTile().Stringify()).ToArray()));

                return filtered.Select(p => new PointAtTurn(p, pat.Turn + 2));
            }
        );

        return astar.Path.Select(pat => pat.Point);
    }

    public static IEnumerable<Tile> PathSafely(IEnumerable<Point> starts, IEnumerable<Point> goals, Func<Point, bool> futureEmpty, Func<Point, bool> futureFull)
    {
        var autoStates = AutoStates(AI._Game.MaxTurns - AI._Game.CurrentTurn + 1).ToDictionary(s => s.Turn);
        Func<PointAtTurn, int> h = pat => goals.Min(g => g.ManhattanDistance(pat.Point));
        if (!goals.Any())
        {
            h = pat => 0;
        }
        var goalSet = goals.ToHashSet();
        Func<Point, int, bool> goodNeighbor = (p, turn) =>
        {
            var isGoal = goalSet.Contains(p);
            if (!IsWalkable(p) && isGoal)
            {
                return true;
            }
            var isSafe = IsSafe(p, autoStates[turn]) && IsSafe(p, autoStates[turn + 1]);
            if (isSafe)
            {
                if (goalSet.Contains(p))
                {
                    return true;
                }
                var isFuture = turn >= AI._Game.CurrentTurn + 2;
                if (isFuture)
                {
                    return (IsWalkable(p) || futureEmpty(p)) && !futureFull(p);
                }
                else
                {
                    return IsWalkable(p);
                }
            }
            return false;
        };

        var astar = new AStar<PointAtTurn>(
            starts.Select(p => new PointAtTurn(p, AI._Game.CurrentTurn)),
            pat => goalSet.Contains(pat.Point),
            (pat1, pat2) => pat2.Point.ToTile().HasHazard ? 4 : 2,
            h.Memoize(),
            pat =>
            {
                if (pat.Turn >= AI._Game.MaxTurns)
                {
                    return Enumerable.Empty<PointAtTurn>();
                }

                var neighboors = Neighboors(pat.Point);
                // Console.WriteLine("Direct: " + String.Join(" ", neighboors.Select(n => n.ToTile().Stringify()).ToArray()));
                var filtered = neighboors.Where(p => goodNeighbor(p, pat.Turn));
                // Console.WriteLine("Filter: " + String.Join(" ", filtered.Select(n => n.ToTile().Stringify()).ToArray()));

                return filtered.Select(p => new PointAtTurn(p, pat.Turn + 2));
            }
        );

        return astar.Path.Select(pat => pat.Point.ToTile());
    }

    public static IEnumerable<Tile> PathSafely(IEnumerable<Point> starts, IEnumerable<Point> goals)
    {
        return PathSafely(starts, goals, p => false, p => false);
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
        if (!state.IsOurTurn && point.Equals(state.TheirCallIn))
        {
            return false;
        }
        return true;
    }

    public static IEnumerable<Point> Neighboors(Point point)
    {
        yield return new Point(point.x, point.y - 1);
        yield return new Point(point.x, point.y + 1);
        yield return new Point(point.x + 1, point.y);
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
    
    public static string RelDirection(Point origin, Point target)
    {
        if (origin.x == target.x)
        {
            if (origin.y > target.y)
            {
                return "North";
            }
            else
            {
                return "South";
            }
        }
        else
        {
            if (origin.x < target.x)
            {
                return "East";
            }
            else
            {
                return "West";
            }
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
            Bottles = new Dictionary<Point, Bottle>();
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
        state.Bottles
            .Where(kvp => IsBottlePathable(kvp.Key))
            .ForEach(kvp =>
            {
                var nextPoint = NextPoint(kvp.Key, kvp.Value.Direction);
                if (!nextState.Bottles.Remove(nextPoint))
                {
                    nextState.Bottles.Add(NextPoint(kvp.Key, kvp.Value.Direction), kvp.Value);
                }
            });

        return nextState;
    }
    
    public static IEnumerable<IEnumerable<Point>> WalkableExpansion(Point focus, int maxLength)
    {
        foreach (var direction in new string[] {"North", "East", "South", "West"})
        {
            var counter = 0;
            var nextPoint = NextPoint(focus, direction);
            List<Point> cardinalTrail = new List<Point>();
            while (counter < maxLength && IsWalkable(nextPoint) && !nextPoint.ToTile().IsBalcony)
            {
                cardinalTrail.Add(nextPoint);
                
                counter++;
                nextPoint = NextPoint(nextPoint, direction);
            }
            yield return cardinalTrail;
        }
    }
    
    public static IEnumerable<Point> BottleLaunchExpansion(Point focus, int maxLength)
    {
        foreach (var direction in new string[] {"North", "East", "South", "West"})
        {
            var counter = 0;
            var nextPoint = NextPoint(focus, direction);
            while (counter < maxLength && IsBottlePathable(nextPoint) && !nextPoint.ToTile().IsBalcony)
            {
                yield return nextPoint;
                
                counter++;
                nextPoint = NextPoint(nextPoint, direction);
            }
        }
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

    public static void MoveIn()
    {
        var cowboys = AI._Player.Cowboys.Where(c => !c.IsDead && !c.IsDrunk && c.CanMove)
            .Select(c => c.ToPoint())
            .ToHashSet();
        var inDir = AI._Player.YoungGun.Tile.Y == 0 ? "South" : "North";

        foreach (var cowboy in cowboys)
        {
            var next = NextPoint(cowboy, inDir);
            if (IsWalkable(next))
            {
                cowboy.ToTile().Cowboy.Move(next.ToTile());
            }
        }
    }

    public static bool IsPiano(Point point)
    {
        var tile = point.ToTile();
        return tile != null && tile.Furnishing != null && tile.Furnishing.IsPiano;
    }

    public static bool IsNearPiano(Point point)
    {
        return Neighboors(point).Any(n => IsPiano(n));
    }

    public static bool AnyInRange(Point point, int range, IEnumerable<Point> targets)
    {
        return targets.Any(t => point.IsInRange(range, t));
    }

    public static IEnumerable<Point> Pianos()
    {
        return AI._Game.Furnishings.Select(f => f.ToPoint()).Where(p => IsPiano(p));
    }
}
