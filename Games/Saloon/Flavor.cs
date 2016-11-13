using Joueur.cs.Games.Saloon;
using System;

static class Flavor
{
    public static void LogScared(this Cowboy cowboy)
    {
        var responses = new []
        {
            "YIKES!",
            "BOY HOWDY!",
            "WHAT IN TARNATION?",
            "HOW RUDE!"
        };

        if (AI._Random.Next(5) == 0)
        {
            cowboy.Log(responses[AI._Random.Next(responses.Length)]);
        }
    }
}