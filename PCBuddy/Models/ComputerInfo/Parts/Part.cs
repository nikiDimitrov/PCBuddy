namespace PCBuddy.Models.ComputerInfo
{
    public abstract class Part
    {
        public string Manufacturer { get; set; }

        public string ModelName { get; set; }

        public int ReleaseYear { get; set; }
    }
}
