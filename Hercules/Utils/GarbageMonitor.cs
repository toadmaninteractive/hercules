using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Hercules
{
    public static class GarbageMonitor
    {
        abstract class GarbageTypeMonitorBase
        {
            public abstract int Count { get; }
        }

        class GarbageTypeMonitor<T> : GarbageTypeMonitorBase where T : class
        {
            private class Handle
            {
                public static int Count;

                public Handle()
                {
                    Interlocked.Increment(ref Count);
                }

                ~Handle()
                {
                    Interlocked.Decrement(ref Count);
                }
            }

            private static readonly GarbageTypeMonitor<T> Instance;

            public override int Count => Handle.Count;

            static GarbageTypeMonitor()
            {
                if (Instance == null)
                {
                    Instance = new GarbageTypeMonitor<T>();
                    Monitors[typeof(T)] = Instance;
                }
            }

            static readonly ConditionalWeakTable<T, Handle> Table = new();

            public static void Register(T obj)
            {
                Table.Add(obj, new Handle());
            }
        }

        static readonly Dictionary<Type, GarbageTypeMonitorBase> Monitors = new();

        public static void Register<T>(T instance) where T : class
        {
            GarbageTypeMonitor<T>.Register(instance);
        }

        public static void Report()
        {
            var reported = false;
            foreach (var pair in Monitors)
            {
                if (pair.Value.Count > 0)
                {
                    Logger.LogDebug($"{pair.Key}: {pair.Value.Count}");
                    reported = true;
                }
            }
            if (!reported)
                Logger.LogDebug("No registered instances");
        }

        public static void GarbageCollect()
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
        }
    }
}
