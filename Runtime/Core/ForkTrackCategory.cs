using System;

namespace ForkTrack.Core
{
    /// <summary>
    /// Represents a category for organizing nodes (e.g., "Main Quest", "Side Quest")
    /// </summary>
    [Serializable]
    public class ForkTrackCategory
    {
        /// <summary>Unique identifier for this category</summary>
        public string id;

        /// <summary>Display name of the category</summary>
        public string name;

        /// <summary>Color hex code for visual identification (e.g., "#FF5733")</summary>
        public string color;

        public ForkTrackCategory() { }

        public ForkTrackCategory(string id, string name, string color)
        {
            this.id = id;
            this.name = name;
            this.color = color;
        }
    }
}
