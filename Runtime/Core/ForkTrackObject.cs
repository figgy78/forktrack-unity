using System;

namespace ForkTrack.Core
{
    /// <summary>
    /// Represents an object in the ForkTrack graph (e.g., "Fred", "Door", "Chest")
    /// </summary>
    [Serializable]
    public class ForkTrackObject
    {
        /// <summary>Unique identifier for this object</summary>
        public string id;

        /// <summary>Display name of the object</summary>
        public string name;

        /// <summary>Optional category ID this object belongs to</summary>
        public string category;

        public ForkTrackObject() { }

        public ForkTrackObject(string id, string name, string category = null)
        {
            this.id = id;
            this.name = name;
            this.category = category;
        }
    }
}
