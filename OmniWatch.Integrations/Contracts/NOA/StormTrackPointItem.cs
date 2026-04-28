namespace OmniWatch.Integrations.Contracts.NOA
{
    public class StormTrackPointItem
    {
        public DateTime Time { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int Wind { get; set; }
        public int Pressure { get; set; }
        public int Category { get; set; }

        public string Basin { get; set; }
        public string Nature { get; set; }

        public double DistanceToLand { get; set; }
    }
}
