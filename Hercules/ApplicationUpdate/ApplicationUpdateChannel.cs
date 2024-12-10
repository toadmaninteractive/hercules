using System;
using System.ComponentModel;

namespace Hercules.ApplicationUpdate
{
    public enum ApplicationUpdateChannel
    {
        Stable = 0,
        Beta = 1,
        [Description("Development")]
        Dev = 2,
    }

    public static class ApplicationUpdateChannelHelper
    {
        public static string ToTag(this ApplicationUpdateChannel channel)
        {
            return channel switch
            {
                ApplicationUpdateChannel.Stable => "stable",
                ApplicationUpdateChannel.Beta => "beta",
                ApplicationUpdateChannel.Dev => "dev",
                _ => throw new ArgumentOutOfRangeException(nameof(channel))
            };
        }
    }
}
