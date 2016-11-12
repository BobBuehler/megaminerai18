using System;
using System.Collections.Generic;
using System.Linq;

class AStar<T>
{
    public LinkedList<T> Path;
    public HashSet<T> Closed;
    public HashSet<T> Open;
    public Dictionary<T, T> From; // A shortest path from any start to key goes through value-key edge.
    public Dictionary<T, int> GScore; // A shortest path from any start to key requires value cost.
    public Dictionary<T, int> FScore; // Lower bound total cost from a start to a goal given that it goes through key.

    public AStar(IEnumerable<T> starts, Func<T, bool> isGoal, Func<T, T, int> costCalc, Func<T, int> hCalc, Func<T, IEnumerable<T>> neighboorCalc)
    {
        Path = new LinkedList<T>();

        Closed = new HashSet<T>();
        Open = new HashSet<T>(starts);
        From = new Dictionary<T, T>();

        GScore = starts.ToDictionary(s => s, s => 0);
        FScore = starts.ToDictionary(s => s, s => GScore[s] + hCalc(s));


        while (Open.Any())
        {
            var current = Open.MinByValue(p => FScore[p]);
            if (GScore[current] > int.MaxValue) return;

            if (isGoal(current))
            {
                Path = CalcPathTo(current);
                return;
            }

            Open.Remove(current);
            Closed.Add(current);

            foreach (var n in neighboorCalc(current).Where(n => !Closed.Contains(n)))
            {
                var g = GScore[current] + costCalc(current, n);
                if (!Open.Contains(n) || g < GScore[n])
                {
                    GScore[n] = g;
                    FScore[n] = g + hCalc(n);
                    From[n] = current;
                    Open.Add(n);
                }
            }
        }
    }

    public LinkedList<T> CalcPathTo(T end)
    {
        var path = new LinkedList<T>();

        var current = end;
        path.AddFirst(end);
        while (From.TryGetValue(current, out current))
        {
            path.AddFirst(current);
        }

        return path;
    }
}