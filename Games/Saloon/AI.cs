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

            var pianos = this.Game.Furnishings.Where(f => f.IsPiano && !f.IsDestroyed);

            foreach(var cowboy in this.Player.Cowboys.Where(c => !c.IsDead && !c.IsDrunk))
            {
                var targetPianos = pianos.Where(f => !f.IsPlaying);
                if (!targetPianos.Any())
                {
                    return true;
                }
                var piano = targetPianos.MinByValue(p => p.ToPoint().ManhattanDistance(cowboy.ToPoint()));
                
                List<Tile> path = this.FindPath(cowboy.Tile, piano.Tile);
                if (path.Count > 1)
                {
                    cowboy.Move(path.First());
                }

                if (piano.ToPoint().ManhattanDistance(cowboy.ToPoint()) == 1)
                {
                    cowboy.Play(piano);
                }
            }
            
            Console.WriteLine("{0} - {1}", this.Game.CurrentTurn, stopwatch.ElapsedMilliseconds);

            return true;
        }

        void Spawn()
        {
            if (this.Player.YoungGun.CallInTile.Cowboy != null)
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

        /// <summary>
        /// A very basic path finding algorithm (Breadth First Search) that when given a starting Tile, will return a valid path to the goal Tile.
        /// </summary>
        /// <remarks>
        /// This is NOT an optimal pathfinding algorithm. It is intended as a stepping stone if you want to improve it.
        /// </remarks>
        /// <param name="start">the starting Tile</param>
        /// <param name="goal">the goal Tile</param>
        /// <returns>A List of Tiles representing the path, the the first element being a valid adjacent Tile to the start, and the last element being the goal. Or an empty list if no path found.</returns>
        List<Tile> FindPath(Tile start, Tile goal)
        {
            // no need to make a path to here...
            if (start == goal)
            {
                return new List<Tile>();
            }

            // the tiles that will have their neighbors searched for 'goal'
            Queue<Tile> fringe = new Queue<Tile>();

            // How we got to each tile that went into the fringe.
            Dictionary<Tile, Tile> cameFrom = new Dictionary<Tile, Tile>();

            // Enqueue start as the first tile to have its neighbors searched.
            fringe.Enqueue(start);

            // keep exploring neighbors of neighbors... until there are no more.
            while (fringe.Count > 0)
            {
                // the tile we are currently exploring.
                Tile inspect = fringe.Dequeue();

                // cycle through the tile's neighbors.
                foreach (Tile neighbor in inspect.GetNeighbors())
                {
                    if (neighbor == goal)
                    {
                        // Follow the path backward starting at the goal and return it.
                        List<Tile> path = new List<Tile>();
                        path.Add(goal);

                        // Starting at the tile we are currently at, insert them retracing our steps till we get to the starting tile
                        for (Tile step = inspect; step != start; step = cameFrom[step])
                        {
                            path.Insert(0, step);
                        }

                        return path;
                    }

                    // if the tile exists, has not been explored or added to the fringe yet, and it is pathable
                    if (neighbor != null && !cameFrom.ContainsKey(neighbor) && neighbor.IsPathable())
                    {
                        // add it to the tiles to be explored and add where it came from.
                        fringe.Enqueue(neighbor);
                        cameFrom.Add(neighbor, inspect);
                    }

                } // foreach(neighbor)

            } // while(fringe not empty)

            // if you're here, that means that there was not a path to get to where you want to go.
            //   in that case, we'll just return an empty path.
            return new List<Tile>();

        }
        #endregion
    }
}
