using System;
using System.Threading.Tasks;

namespace Hercules
{
    public static class AsyncValue
    {
        public static Lazy<T> Create<T>(Func<T> initializer)
        {
            var task = Task.Run(initializer);
            return new(() => task.Result);
        }
    }
}
