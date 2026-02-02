using System;

namespace ForkTrack.Core
{
    /// <summary>
    /// Represents an action in the ForkTrack graph (e.g., "Interact", "Open", "Collect")
    /// </summary>
    [Serializable]
    public class ForkTrackAction
    {
        /// <summary>Unique identifier for this action</summary>
        public string id;

        /// <summary>Display name of the action</summary>
        public string name;

        public ForkTrackAction() { }

        public ForkTrackAction(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}
