// An object in the game. The most basic class that all game classes should inherit from automatically.

// DO NOT MODIFY THIS FILE
// Never try to directly create an instance of this class, or modify its member variables.
// Instead, you should only be reading its variables and calling its functions.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// you can add addtional using(s) here


namespace Joueur.cs.Games.Saloon
{
    /// <summary>
    /// An object in the game. The most basic class that all game classes should inherit from automatically.
    /// </summary>
    class GameObject : BaseGameObject
    {
        #region Properties
        /// <summary>
        /// String representing the top level Class that this game object is an instance of. Used for reflection to create new instances on clients, but exposed for convenience should AIs want this data.
        /// </summary>
        public string GameObjectName { get; protected set; }

        /// <summary>
        /// Any strings logged will be stored here. Intended for debugging.
        /// </summary>
        public IList<string> Logs { get; protected set; }


        // you can add addtional properties(s) here. None of them will be tracked or updated by the server.

        #endregion


        #region Methods
        /// <summary>
        /// Creates a new instance of GameObject. Used during game initialization, do not call directly.
        /// </summary>
        protected GameObject() : base()
        {
            this.Logs = new List<string>();
        }

        /// <summary>
        /// Adds a message to this GameObject's logs. Intended for your own debugging purposes, as strings stored here are saved in the gamelog.
        /// </summary>
        /// <param name="message">A string to add to this GameObject's log. Intended for debugging.</param>
        public void Log(string message)
        {
            this.RunOnServer<object>("log", new Dictionary<string, object> {
                {"message", message}
            });
        }


        // you can add addtional method(s) here.

        #endregion
    }
}
