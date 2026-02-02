namespace ForkTrack.Core
{
    /// <summary>
    /// Logical condition for combining edge dependencies
    /// </summary>
    public enum DependencyCondition
    {
        /// <summary>All dependencies with this condition must be satisfied</summary>
        AND,
        /// <summary>At least one dependency with this condition must be satisfied</summary>
        OR,
        /// <summary>The dependency must NOT be satisfied (inverts the check)</summary>
        NOT
    }
}
