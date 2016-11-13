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
        public static Random _Random;
        public static HashSet<Point> _IsAPlayer;

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
            this.Opponent = this.Game.Players.First(p => p.Id != this.Player.Id);
    
            base.Start();
            AI._Game = this.Game;
            AI._Player = this.Player;
            AI._OtherPlayer = this.Opponent;
            AI._Random = new Random();
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
            Console.WriteLine("Turn #{0}", this.Game.CurrentTurn);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            AI._IsAPlayer = new HashSet<Point>();


            Spawn();
            
            Solver.GreedySwarmAndPlay();
            CauseTrouble();
            SharpshooterAttack();

            Spawn();
            this.Player.Cowboys.ForEach(c => Solver.BeSafe(c));

            Console.WriteLine("Turn #{0}. Score={1}-{2}. Time={3}ms.",
                this.Game.CurrentTurn,
                this.Player.Score,
                this.Opponent.Score,
                stopwatch.ElapsedMilliseconds);

            return true;
        }

        void Spawn()
        {
            var youngGun = this.Player.YoungGun;
            var spawnTile = youngGun.CallInTile;
            
            var cowboys = this.Player.Cowboys;
            var opponentCowboys = this.Opponent.Cowboys;
            
            if (spawnTile.Cowboy != null && spawnTile.Cowboy.Owner == this.Player)
            {
                return;
            }
            
            if (spawnTile.Furnishing != null && spawnTile.Furnishing.IsPiano)
            {
                // Should we spawn on piano?
                var friendlyPath = Solver.PathSafely(new [] { spawnTile.ToPoint() }, cowboys.Select(c => c.ToPoint()));
                var opponentPath = Solver.PathSafely(new [] { spawnTile.ToPoint() }, opponentCowboys.Select(c => c.ToPoint()));
                // Maybe spawn if we can play on it
                if (friendlyPath.Count() <= 3)
                {
                    if (opponentPath.Count() > 3)
                    {
                        return;
                    }
                }
                else if (opponentPath.Count() > 3)
                {
                    return;
                }
            }

            var jobPriority = new [] { "Sharpshooter", "Bartender", "Brawler" };
            foreach(var job in jobPriority)
            {
                if (cowboys.Count(c => c.Job == job) < 2)
                {
                    youngGun.CallIn(job);
                    break;
                }
            }
        }
        
        void GreedyBartenders()
        {
            var bartenders = this.Player.Cowboys.Where(c => c.CanMove && !c.IsDead && !c.IsDrunk && c.TurnsBusy == 0 && c.Job == "Bartender");
            var opponentCowboys = this.Opponent.Cowboys.Where(c => !c.IsDead).Select(c => c.ToPoint()).ToHashSet();
            foreach(var bartender in bartenders)
            {
                this.GreedyBartender(bartender, opponentCowboys);
            }
        }
        
        void GreedyBartender(Saloon.Cowboy bartender, IEnumerable<Point> opponentCowboys)
        {
            var startPoint = bartender.ToPoint();
            foreach (var direction in new string[] {"North", "East", "South", "West"})
            {
                Func<Point, Point> nextPoint = p => Solver.NextPoint(p, direction);
                var stepPoint = nextPoint(startPoint);
                
                while(stepPoint.ManhattanDistance(startPoint) <= 2)
                {
                    if (opponentCowboys.Contains(stepPoint))
                    {
                        bartender.Act(nextPoint(startPoint).ToTile(), direction);
                        return;
                    }
                    if (!Solver.IsBottlePathable(stepPoint))
                    {
                        break;
                    }
                    stepPoint = nextPoint(stepPoint);
                }
            }
        }
        
        void BartenderMadness()
        {
            var youngGun = this.Player.YoungGun;
            var spawnTile = youngGun.CallInTile;
            var spawnPoint = spawnTile.ToPoint();
            var nextSpawnPoint = Solver.AutoStates(3).Last().OurCallIn;
            var cowboys = this.Player.Cowboys.Where(c => !c.IsDead);
            var bartenders = cowboys.Where(c => c.Job == "Bartender");
            var moved = false;
            
            if (bartenders.Count() == 0)
            {
                youngGun.CallIn("Bartender");
            }
            else if (bartenders.Count() == 1)
            {
                var bartender = bartenders.First();
                
                Console.WriteLine("Throw Bottle");
                if (nextSpawnPoint.y == 1 && nextSpawnPoint.x != 20)
                {
                    Console.WriteLine("South");
                    bartender.Act(bartender.Tile.TileSouth, "North");
                }
                else if (nextSpawnPoint.x == 20 && nextSpawnPoint.y != 10)
                {
                    Console.WriteLine("West");
                    bartender.Act(bartender.Tile.TileWest, "East");
                }
                else if (nextSpawnPoint.y == 10 && nextSpawnPoint.x != 1)
                {
                    Console.WriteLine("North");
                    bartender.Act(bartender.Tile.TileNorth, "South");
                } else
                {
                    Console.WriteLine("East");
                    bartender.Act(bartender.Tile.TileEast, "West");
                }
                
                Console.WriteLine("Move");
                if ( !bartender.ToPoint().Equals(spawnPoint) && Solver.IsWalkable(spawnPoint) )
                {
                    bartender.Move(spawnPoint.ToTile());
                    moved = true;
                }
                
                Console.WriteLine("Spawn");
                if ( moved )
                {
                    if (!Solver.IsWalkable(nextSpawnPoint))
                    {
                        Console.WriteLine("Spawn Brawler");
                        youngGun.CallIn("Brawler");
                    }
                    else
                    {
                        Console.WriteLine("Spawn Move Bartender");
                        youngGun.CallIn("Bartender");
                    }
                }
                else
                {
                    // Not Moving
                    if (!Solver.IsWalkable(nextSpawnPoint) && !spawnPoint.Equals(nextSpawnPoint))
                    {
                        Console.WriteLine("Spawn Sharpshooter");
                        youngGun.CallIn("Sharpshooter");
                    }
                    else
                    {
                        Console.WriteLine("Spawn NoMove Bartender");
                        youngGun.CallIn("Bartender");
                    }
                }
                Console.WriteLine("End Bar Fest");
            }
        }


        void CauseTrouble()
        {
            var cowboys = this.Player.Cowboys.Where(c => (!c.IsDead && !c.IsDrunk && c.CanMove && c.TurnsBusy == 0) && (c.Job == "Brawler")).ToList();
            var targets = AI._OtherPlayer.Cowboys.Select(t => t.ToPoint());
            var targets = AI._OtherPlayer.Cowboys.Select(t => t.ToPoint()).ToHashSet();
            
            if(cowboys.Count() == 0 || targets.Count() == 0)
            {
                return;
            }
            foreach(var brawler in cowboys)
            {
                var path = Solver.PathSafely(new [] { brawler.ToPoint() }, targets);
                if(path.Count() > 2)
                {
                    brawler.Move(path.ElementAt(1));  
                }
            }
        }
        
        void SharpshooterAttack()
        {
            var cowboys = this.Player.Cowboys.Where(c => (!c.IsDead && !c.IsDrunk && c.CanMove && c.TurnsBusy == 0) && (c.Job == "Sharpshooter")).ToList();
            var targets = AI._OtherPlayer.Cowboys.Select(t => t.ToPoint());
            var targetsNearPianos = targets.Where(t => Solver.Neighboors(t).Select(n => n.ToTile()).Any(n => n.Furnishing != null && n.Furnishing.IsPiano));

            if(cowboys.Count() == 0 || !targetsNearPianos.Any())
            {
                return;
            }

            foreach(var sharpshooter in cowboys)
            {
                var path = Solver.PathSafely(new [] { sharpshooter.ToPoint() }, targetsNearPianos);

                if (path.Count() > 2)
                {
                    sharpshooter.Move(path.ElementAt(1));
                }
                
                if (sharpshooter.Focus > 0 && path.Count() == 2)
                {
                    sharpshooter.Act(path.Last());
                }
            }
        }
        
        #endregion
    }
}
