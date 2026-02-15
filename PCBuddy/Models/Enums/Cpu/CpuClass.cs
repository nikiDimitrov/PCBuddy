namespace PCBuddy.Models.Enums
{
    /// <summary>
    /// PCBuddy uses this enum to determine the CPU's class after
    /// looking at its characteristics. 
    /// </summary>
    public enum CpuClass
    {
        LOWPOWER = 0,
        MAINSTREAM_DESKTOP = 1,
        HIGH_PERFORMANCE = 2,
        WORKSTATION = 3
    }
}
