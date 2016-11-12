using System;
using System.Collections.Generic;
using System.Linq;

interface ISharpMove
{
}

interface ISharpState<TMove> where TMove : ISharpMove
{
}

class SharpCacheValue<TMove, TState>
    where TMove : ISharpMove
    where TState : ISharpState<TMove>
{
    public TState State;
    public IEnumerable<Tuple<TMove, TState>> Nexts;

    public bool IsNew;
    public int SelfFitness;
    public int ExpectedFitness;
    public int ExpectedFitnessDepth;

    public SharpCacheValue(TState state)
    {
        State = state;
        Nexts = null;
        IsNew = true;
        SelfFitness = 0;
        ExpectedFitness = 0;
        ExpectedFitnessDepth = 0;
    }
}

class SharpMinimax<TMove, TState>
    where TMove : ISharpMove
    where TState : ISharpState<TMove>
{
    private Func<TState, bool> isTerminal;
    private Func<TState, int> calcFitness;
    private Func<TState, IEnumerable<Tuple<TMove, TState>>> moveCalc;
    private LRUCache<TState, SharpCacheValue<TMove, TState>> stateCache;

    public SharpMinimax(
        Func<TState, bool> isTerminal,
        Func<TState, int> calcFitness,
        Func<TState, IEnumerable<Tuple<TMove, TState>>> moveCalc,
        int cacheCapacity)
    {
        this.isTerminal = isTerminal;
        this.calcFitness = calcFitness;
        this.moveCalc = moveCalc;

        this.stateCache = new LRUCache<TState, SharpCacheValue<TMove, TState>>(cacheCapacity);
        this.stateCache.OnMiss = delegate (TState state, out SharpCacheValue<TMove, TState> value)
        {
            value = new SharpCacheValue<TMove, TState>(state);
            return true;
        };
    }

    public TMove Search(TState state, int depth)
    {
        return Search(state, depth, true, int.MinValue, int.MaxValue);
    }

    public TMove Search(TState state, int depth, bool maximizingPlayer, int a, int b)
    {
        var cacheValue = stateCache[state];
        SearchInto(cacheValue, depth, maximizingPlayer, a, b);
        return cacheValue.Nexts.MaxByValue(next => stateCache[next.Item2].ExpectedFitness).Item1;
    }

    // Need to document what this is and isn't doing
    private void SearchInto(SharpCacheValue<TMove, TState> cacheValue, int depth, bool maximizingPlayer, int a, int b)
    {
        if (!cacheValue.IsNew && cacheValue.ExpectedFitnessDepth >= depth)
        {
            // We've totally done this search before and maybe even went deeper
            return;
        }

        if (depth == 0 || isTerminal(cacheValue.State))
        {
            if (cacheValue.IsNew)
            {
                cacheValue.IsNew = false;
                cacheValue.SelfFitness = calcFitness(cacheValue.State);
                cacheValue.ExpectedFitness = cacheValue.SelfFitness;
                cacheValue.ExpectedFitnessDepth = depth;
            }
            return;
        }

        if (cacheValue.Nexts == null)
        {
            cacheValue.Nexts = moveCalc(cacheValue.State);
        }

        var nextValues = OrderBestFirst(cacheValue.Nexts.Select(next => stateCache[next.Item2]), maximizingPlayer)
                .While(value =>
                {
                    SearchInto(value, depth - 1, !maximizingPlayer, a, b);
                    return !ShouldABPrune(value.ExpectedFitness, maximizingPlayer, ref a, ref b);
                });
        var bestNextValue = GetBest(nextValues, maximizingPlayer);

        cacheValue.ExpectedFitness = bestNextValue.ExpectedFitness;
        cacheValue.ExpectedFitnessDepth = depth;
    }

    // Putting expected best choices first we can AB prune more frequently
    private IEnumerable<SharpCacheValue<TMove, TState>> OrderBestFirst(IEnumerable<SharpCacheValue<TMove, TState>> values, bool max)
    {
        if (max)
        {
            return values.OrderByDescending(value => value.IsNew ? int.MinValue : value.ExpectedFitness);
        }
        else
        {
            return values.OrderBy(value => value.IsNew ? int.MaxValue : value.ExpectedFitness);
        }
    }

    // Update alpha or beta and return true if there is no need to expore more options
    private bool ShouldABPrune(int fitness, bool max, ref int a, ref int b)
    {
        if (max)
        {
            a = Math.Max(a, fitness);
        }
        else
        {
            b = Math.Min(b, fitness);
        }
        return a > b;
    }

    private SharpCacheValue<TMove, TState> GetBest(IEnumerable<SharpCacheValue<TMove, TState>> values, bool max)
    {
        if (max)
        {
            return values.MaxByValue(v => v.ExpectedFitness);
        }
        else
        {
            return values.MinByValue(v => v.ExpectedFitness);
        }
    }
}