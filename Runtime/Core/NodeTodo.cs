using System;

namespace ForkTrack.Core
{
    /// <summary>
    /// A development todo item attached to a node
    /// </summary>
    [Serializable]
    public class NodeTodo
    {
        /// <summary>Title of the todo item</summary>
        public string title;

        /// <summary>Detailed description (supports markdown)</summary>
        public string description;

        /// <summary>Person responsible for this todo</summary>
        public string responsible;

        /// <summary>Whether this todo has been resolved</summary>
        public bool resolved;

        public NodeTodo() { }

        public NodeTodo(string title, string description = null, string responsible = null, bool resolved = false)
        {
            this.title = title;
            this.description = description;
            this.responsible = responsible;
            this.resolved = resolved;
        }
    }
}
