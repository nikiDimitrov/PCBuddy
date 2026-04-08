namespace PCBuddy.Models.Enums
{
    /// <summary>
    /// Enum for SMART status code.
    /// </summary>
    public enum SMARTStatusCode
    {
        UNKNOWN = 0,
        PERFECT = 1,
        COMM_PROBLEMS = 2,
        WEAK_SECTORS = 4,
        BAD_SECTORS = 8,
        SPIN_UP_PROBLEMS = 16,
        LOW_HEALTH = 32,
        FAILURE_PREDICTED = 64
    }
}
