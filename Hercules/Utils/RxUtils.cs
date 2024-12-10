using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Hercules
{
    public static class RxUtils
    {
        public static IObservable<IList<TSource>> BufferWhenAvailable<TSource>(this IObservable<TSource> source, TimeSpan threshold)
        {
            return source.GroupByUntil(_ => true, _ => Observable.Timer(threshold)).SelectMany(i => i.ToList());
        }
    }
}
