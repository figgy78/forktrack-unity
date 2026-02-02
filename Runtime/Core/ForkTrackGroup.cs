using System;
using UnityEngine;

namespace ForkTrack.Core
{
    /// <summary>
    /// Represents a visual group container for organizing nodes in the editor
    /// </summary>
    [Serializable]
    public class ForkTrackGroup
    {
        /// <summary>Unique identifier for this group</summary>
        public string id;

        /// <summary>Display name of the group</summary>
        public string name;

        /// <summary>Color hex code for visual identification</summary>
        public string color;

        /// <summary>Position in the editor</summary>
        public Vector2 position;

        /// <summary>Size of the group container</summary>
        public Vector2 size;

        public ForkTrackGroup() { }

        public ForkTrackGroup(string id, string name, string color = null)
        {
            this.id = id;
            this.name = name;
            this.color = color;
        }
    }
}
