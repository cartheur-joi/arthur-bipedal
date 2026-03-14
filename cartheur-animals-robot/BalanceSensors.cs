namespace Cartheur.Animals.Robot
{
    public enum SupportFoot
    {
        Unknown,
        Left,
        Right,
        Both
    }

    public struct ImuSample
    {
        public double PitchDegrees { get; set; }
        public double RollDegrees { get; set; }
        public double YawDegrees { get; set; }
        public bool IsValid { get; set; }
    }

    public struct FootContactSample
    {
        public bool LeftInContact { get; set; }
        public bool RightInContact { get; set; }
        public bool IsValid { get; set; }
    }

    public interface IImuProvider
    {
        ImuSample GetSample();
    }

    public interface IFootContactProvider
    {
        FootContactSample GetSample();
    }

    /// <summary>
    /// Default provider used when no IMU integration is configured.
    /// </summary>
    public sealed class NullImuProvider : IImuProvider
    {
        public ImuSample GetSample()
        {
            return new ImuSample { IsValid = false };
        }
    }

    /// <summary>
    /// Default provider used when no foot-contact sensors are configured.
    /// </summary>
    public sealed class NullFootContactProvider : IFootContactProvider
    {
        public FootContactSample GetSample()
        {
            return new FootContactSample { IsValid = false };
        }
    }
}
