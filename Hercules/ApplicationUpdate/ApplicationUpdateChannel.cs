using System;

namespace Hercules.ApplicationUpdate
{
    public enum ApplicationUpdateChannel
    {
        Stable = 0,
        Beta = 1,
    }

    public static class ApplicationUpdateChannelHelper 
    {
        extension(ApplicationUpdateChannel channel)
        {
            public string ToTag()
            {
                return channel switch
                {
                    ApplicationUpdateChannel.Stable => "stable",
                    ApplicationUpdateChannel.Beta => "beta",
                    _ => throw new ArgumentOutOfRangeException(nameof(channel))
                };
            }
        }
    }
}
