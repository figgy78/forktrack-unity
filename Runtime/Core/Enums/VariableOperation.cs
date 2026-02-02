namespace ForkTrack.Core
{
    /// <summary>
    /// Operation to perform on a variable when a node trigger fires
    /// </summary>
    public enum VariableOperation
    {
        /// <summary>Set the variable to the specified value</summary>
        SET,
        /// <summary>Add the value to the current NUMBER variable</summary>
        ADD,
        /// <summary>Subtract the value from the current NUMBER variable</summary>
        SUBTRACT,
        /// <summary>Toggle the BOOLEAN variable (true becomes false, false becomes true)</summary>
        TOGGLE
    }
}
