using System;
using System.Threading.Tasks;

namespace Hercules
{
    public static class TaskUtils
    {
        public static void Track(this Task task)
        {
            ArgumentNullException.ThrowIfNull(task);
            task.ContinueWith(HandleException, TaskScheduler.Default);
        }

        private static void HandleException(Task task)
        {
            if (task.Exception != null)
            {
                Logger.LogException(task.Exception.GetInnerException());
            }
        }

        public static Exception GetInnerException(this Exception exception)
        {
            if (exception is AggregateException aggregateException)
                return aggregateException.InnerException!;
            else
                return exception;
        }

        public static string ExceptionMessage(Exception exception)
        {
            if (exception is AggregateException aggregateException)
                return aggregateException.InnerException!.Message;
            else
                return exception.Message;
        }
    }
}
