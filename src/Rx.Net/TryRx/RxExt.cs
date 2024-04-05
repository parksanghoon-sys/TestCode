using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TryRx
{
    static class RxExt
    {
        public static IObservable<IList<T>> Quiescent<T>(this IObservable<T> source, TimeSpan minimumPeriod, IScheduler scheduler)
        {
            IObservable<int> onOffs = from _ in source
                                      from delta in
                                          Observable.Return(1, scheduler)
                                          .Concat(Observable.Return(-1, scheduler)
                                               .Delay(minimumPeriod, scheduler))

                                      select delta;

            IObservable<int> outstanding = onOffs.Scan(0,(total, deltl) => total + deltl);
            IObservable<int> zeroCrossings = outstanding.Where(Total => Total == 0);

            return source.Buffer(zeroCrossings);
        }
    }
}
