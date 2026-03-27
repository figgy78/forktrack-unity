using System;

namespace ForkTrack.Core
{
    /// <summary>
    /// Represents a location definition in the narrative graph
    /// </summary>
    [Serializable]
    public class ForkTrackLocation
    {
        /// <summary>Unique identifier for this location (e.g., "loc_1704067200000")</summary>
        public string id;

        /// <summary>Display name of the location (e.g., "Village Square")</summary>
        public string name;

        public ForkTrackLocation() { }

        public ForkTrackLocation(string id, string name)
        {
            this.id = id;
            this.name = name;
        }
    }
}
