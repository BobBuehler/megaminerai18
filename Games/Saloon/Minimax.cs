using System;
using System.Collections.Generic;
using System.Linq;

class Minimax<T>
{
    private Func<T, bool> isTerminal;
    private Func<T, int> hCalc;
    private Func<T, IEnumerable<T>> childCalc;

    public Minimax(Func<T, bool> isTerminal, Func<T, int> hCalc, Func<T, IEnumerable<T>> childCalc)
    {
        this.isTerminal = isTerminal;
        this.hCalc = hCalc;
        this.childCalc = childCalc;
    }

    public T Search(T node, int depth)
    {
        return childCalc(node).MaxByValue(child => Step(child, depth - 1, false));
    }

    private int Step(T node, int depth, bool maximizingPlayer)
    {
        if (depth == 0 || isTerminal(node))
        {
            return hCalc(node);
        }

        if (maximizingPlayer)
        {
            return childCalc(node).Max(child => Step(child, depth - 1, false));
        }
        else
        {
            return childCalc(node).Min(child => Step(child, depth - 1, true));
        }
    }
}

class ABMinimax<T>
{
    private Func<T, bool> isTerminal;
    private Func<T, int> hCalc;
    private Func<T, IEnumerable<T>> childCalc;

    public ABMinimax(Func<T, bool> isTerminal, Func<T, int> hCalc, Func<T, IEnumerable<T>> childCalc)
    {
        this.isTerminal = isTerminal;
        this.hCalc = hCalc;
        this.childCalc = childCalc;
    }

    public T Search(T node, int depth)
    {
        return childCalc(node).MaxByValue(child => Step(child, depth - 1, false, int.MinValue, int.MaxValue));
    }

    private int Step(T node, int depth, bool maximizingPlayer, int a, int b)
    {
        if (depth == 0 || isTerminal(node))
        {
            return hCalc(node);
        }

        if (maximizingPlayer)
        {
            return childCalc(node)
                .Select(child => Step(child, depth - 1, false, a, b))
                .While(h => { a = Math.Max(a, h); return a < b; })
                .Max();
        }
        else
        {
            return childCalc(node)
                .Select(child => Step(child, depth - 1, true, a, b))
                .While(h => { b = Math.Min(b, h); return a < b; })
                .Min();
        }
    }
}