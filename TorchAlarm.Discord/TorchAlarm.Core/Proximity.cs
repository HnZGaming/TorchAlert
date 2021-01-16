namespace TorchAlarm.Core
{
    public readonly struct Proximity
    {
        public readonly GridInfo Grid0;
        public readonly GridInfo Grid1;
        public readonly double Distance;

        public Proximity(GridInfo g0, GridInfo g1, double distance)
        {
            Grid0 = g0;
            Grid1 = g1;
            Distance = distance;
        }

        public void Deconstruct(out GridInfo g0, out GridInfo g1, out double distance)
        {
            g0 = Grid0;
            g1 = Grid1;
            distance = Distance;
        }
    }
}