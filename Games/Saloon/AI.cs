// This is where you build your AI for the Saloon game.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Joueur.cs.Games.Saloon
{
    /// <summary>
    /// This is where you build your AI for the Saloon game.
    /// </summary>
    class AI : BaseAI
    {
        public static Game _Game;
        public static Player _Player;
        public static Player _OtherPlayer;

        #region Properties
        #pragma warning disable 0169 // the never assigned warnings between here are incorrect. We set it for you via reflection. So these will remove it from the Error List.
        #pragma warning disable 0649
        /// <summary>
        /// This is the Game object itself, it contains all the information about the current game
        /// </summary>
        public readonly Saloon.Game Game;
        /// <summary>
        /// This is your AI's player. This AI class is not a player, but it should command this Player.
        /// </summary>
        public readonly Saloon.Player Player;
        public Saloon.Player Opponent;
        #pragma warning restore 0169
        #pragma warning restore 0649

        #endregion


        #region Methods
        /// <summary>
        /// This returns your AI's name to the game server. Just replace the string.
        /// </summary>
        /// <returns>string of you AI's name.</returns>
        public override string GetName()
        {
            return "Aimbot"; // REPLACE THIS WITH YOUR TEAM NAME!
        }

        /// <summary>
        /// This is automatically called when the game first starts, once the Game object and all GameObjects have been initialized, but before any players do anything.
        /// </summary>
        /// <remarks>
        /// This is a good place to initialize any variables you add to your AI, or start tracking game objects.
        /// </remarks>
        public override void Start()
        {
            this.Opponent = this.Game.Players.MinByValue(p => p == this.Player );
    
            base.Start();
            AI._Game = this.Game;
            AI._Player = this.Player;
            AI._OtherPlayer = this.Game.Players.First(p => p.Id != this.Player.Id);
        }

        /// <summary>
        /// This is automatically called every time the game (or anything in it) updates.
        /// </summary>
        /// <remarks>
        /// If a function you call triggers an update this will be called before that function returns.
        /// </remarks>
        public override void GameUpdated()
        {
            base.GameUpdated();
        }

        /// <summary>
        /// This is automatically called when the game ends.
        /// </summary>
        /// <remarks>
        /// You can do any cleanup of you AI here, or do custom logging. After this function returns the application will close.
        /// </remarks>
        /// <param name="won">true if your player won, false otherwise</param>
        /// <param name="reason">a string explaining why you won or lost</param>
        public override void Ended(bool won, string reason)
        {
            base.Ended(won, reason);
        }


        /// <summary>
        /// This is called every time it is this AI.player's turn.
        /// </summary>
        /// <returns>Represents if you want to end your turn. True means end your turn, False means to keep your turn going and re-call this function.</returns>
        public bool RunTurn()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Spawn();
            PlayPianos();
            Spawn();
            
            Console.WriteLine("{0} - {1}", this.Game.CurrentTurn, stopwatch.ElapsedMilliseconds);

            return true;
        }

        void PlayPianos()
        {
            while (true)
            {
                var pianos = this.Game.Furnishings.Where(f => f.IsPiano && !f.IsDestroyed && !f.IsPlaying).Select(t => t.ToPoint()).ToHashSet();
                var cowboys = this.Player.Cowboys.Where(c => !c.IsDead && !c.IsDrunk && c.CanMove && c.TurnsBusy == 0).Select(c => c.ToPoint());

                if (pianos.Count == 0 || cowboys.Count() == 0)
                {
                    break;
                }

                var path = Solver.PathSafely(cowboys, p => pianos.Contains(p)).ToList();
                if (path.Count == 0)
                {
                    Console.WriteLine("Nope");
                    break;
                }

                var cowboy = path[0].Cowboy;
                if (path.Count > 2)
                {
                    Console.WriteLine("Move [{0}]", String.Join(",", path.Select(t => t.Stringify()).ToArray()));
                    cowboy.Move(path[1]);
                }
                if (path.Count <= 3)
                {
                    Console.WriteLine("Play [{0}]", String.Join(",", path.Select(t => t.Stringify()).ToArray()));
                    cowboy.Play(path.Last().Furnishing);
                }
            }
        }

        void Spawn()
        {
            var target = this.Player.YoungGun.CallInTile;
            if (target.Cowboy != null && target.Cowboy.Owner == this.Player)
            {
                return;
            }
            var jobPriority = new [] { "Bartender", "Sharpshooter", "Brawler" };
            foreach(var job in jobPriority)
            {
                if (this.Player.Cowboys.Count(c => c.Job == job) < 2)
                {
                    this.Player.YoungGun.CallIn(job);
                    break;
                }
            }
        }

        #endregion
    }
}
