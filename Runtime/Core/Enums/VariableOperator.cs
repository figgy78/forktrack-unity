namespace ForkTrack.Core
{
    /// <summary>
    /// Comparison operator for variable conditions on edges
    /// </summary>
    public enum VariableOperator
    {
        /// <summary>Equal to (==)</summary>
        EQ,
        /// <summary>Not equal to (!=)</summary>
        NE,
        /// <summary>Greater than (>)</summary>
        GT,
        /// <summary>Less than (&lt;)</summary>
        LT,
        /// <summary>Greater than or equal to (>=)</summary>
        GTE,
        /// <summary>Less than or equal to (&lt;=)</summary>
        LTE
    }

    /// <summary>
    /// Extension methods for VariableOperator
    /// </summary>
    public static class VariableOperatorExtensions
    {
        /// <summary>
        /// Converts VariableOperator to its symbol representation
        /// </summary>
        public static string ToSymbol(this VariableOperator op)
        {
            return op switch
            {
                VariableOperator.EQ => "==",
                VariableOperator.NE => "!=",
                VariableOperator.GT => ">",
                VariableOperator.LT => "<",
                VariableOperator.GTE => ">=",
                VariableOperator.LTE => "<=",
                _ => "=="
            };
        }
    }
}
