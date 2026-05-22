namespace LiveCaptionsRain.Core.Timing;

public static class FrameCadence
{
    public static bool ShouldRun(TimeSpan now, TimeSpan lastRun, TimeSpan interval)
    {
        if (lastRun <= TimeSpan.Zero || now < lastRun)
        {
            return true;
        }

        return now - lastRun >= interval;
    }
}
