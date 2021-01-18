namespace TorchAlert.Core
{
    public readonly struct Proximity
    {
        public readonly DefenderGridInfo Defender;
        public readonly OffenderGridInfo Offender;
        public readonly double Distance;

        public Proximity(DefenderGridInfo defender, OffenderGridInfo offender, double distance)
        {
            Defender = defender;
            Offender = offender;
            Distance = distance;
        }

        public void Deconstruct(out DefenderGridInfo defender, out OffenderGridInfo offender, out double distance)
        {
            defender = Defender;
            offender = Offender;
            distance = Distance;
        }

        public override string ToString()
        {
            return $"{nameof(Defender)}: ({Defender}), {nameof(Offender)}: ({Offender}), {nameof(Distance)}: {Distance}";
        }
    }
}