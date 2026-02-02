namespace ForkTrack.Core
{
    /// <summary>
    /// Logging verbosity level for ForkTrack debug output
    /// </summary>
    public enum LogLevel
    {
        /// <summary>No logging</summary>
        None = 0,
        /// <summary>Only errors</summary>
        Error = 1,
        /// <summary>Errors and warnings</summary>
        Warning = 2,
        /// <summary>Errors, warnings, and info messages</summary>
        Info = 3,
        /// <summary>All messages including debug details</summary>
        Debug = 4,
        /// <summary>Most verbose, includes trace information</summary>
        Verbose = 5
    }
}
