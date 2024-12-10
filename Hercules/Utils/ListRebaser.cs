using System;
using System.Collections.Generic;
using System.Linq;

namespace Hercules
{
    public static class ListRebaser
    {
        public static IReadOnlyList<T> Rebase<T>(IReadOnlyList<T> myItems, IReadOnlyList<T> baseItems, IReadOnlyList<T> theirItems, IEqualityComparer<T>? equalityComparer = null, Func<T, T, T, T>? rebaseFun = null)
        {
            T DefaultRebase(T myItem, T baseItem, T theirItem) => myItem;
            return RebaseImpl(myItems.ToList(), baseItems.ToList(), theirItems.ToList(), equalityComparer ?? EqualityComparer<T>.Default, rebaseFun ?? DefaultRebase);
        }

        private static IReadOnlyList<T> RebaseImpl<T>(List<T> myItems, List<T> baseItems, List<T> theirItems, IEqualityComparer<T> equalityComparer, Func<T, T, T, T> rebaseFun)
        {
            var result = new List<T>();

            while (myItems.Count > 0 && baseItems.Count > 0 && theirItems.Count > 0)
            {
                var myIndexOfBaseItem = myItems.FindIndex(item => equalityComparer.Equals(item, baseItems[0]));
                var theirIndexOfBaseItem = theirItems.FindIndex(item => equalityComparer.Equals(item, baseItems[0]));

                if (myIndexOfBaseItem == 0 && theirIndexOfBaseItem == 0)
                {
                    // no changes
                    result.Add(rebaseFun(myItems[0], baseItems[0], theirItems[0]));
                    myItems.RemoveAt(0);
                    theirItems.RemoveAt(0);
                    baseItems.RemoveAt(0);
                    continue;
                }

                if (theirIndexOfBaseItem < 0 || myIndexOfBaseItem < 0)
                {
                    // someone removed base element
                    baseItems.RemoveAt(0);
                    if (myIndexOfBaseItem >= 0)
                        myItems.RemoveAt(myIndexOfBaseItem);
                    if (theirIndexOfBaseItem >= 0)
                        theirItems.RemoveAt(theirIndexOfBaseItem);
                    continue;
                }

                var myIndexOfTheirItem = myItems.FindIndex(item => equalityComparer.Equals(item, theirItems[0]));
                var baseIndexOfTheirItem = baseItems.FindIndex(item => equalityComparer.Equals(item, theirItems[0]));

                if (myIndexOfTheirItem < 0 && baseIndexOfTheirItem >= 0)
                {
                    // we removed base element
                    theirItems.RemoveAt(0);
                    baseItems.RemoveAt(baseIndexOfTheirItem);
                    continue;
                }

                var theirIndexOfMyItem = theirItems.FindIndex(item => equalityComparer.Equals(item, myItems[0]));
                var baseIndexOfMyItem = baseItems.FindIndex(item => equalityComparer.Equals(item, myItems[0]));

                if (theirIndexOfMyItem < 0 && baseIndexOfMyItem >= 0)
                {
                    // they removed base element
                    myItems.RemoveAt(0);
                    baseItems.RemoveAt(baseIndexOfMyItem);
                    continue;
                }

                if (baseIndexOfTheirItem < 0 && myIndexOfTheirItem < 0)
                {
                    // they added an element here
                    result.Add(theirItems[0]);
                    theirItems.RemoveAt(0);
                    continue;
                }

                if (baseIndexOfMyItem < 0 && theirIndexOfMyItem < 0)
                {
                    // we added an element here
                    result.Add(myItems[0]);
                    myItems.RemoveAt(0);
                    continue;
                }

                if (baseIndexOfTheirItem < 0)
                {
                    // we and they added an element
                    result.Add(myItems[myIndexOfTheirItem]);
                    theirItems.RemoveAt(0);
                    myItems.RemoveAt(myIndexOfTheirItem);
                    continue;
                }

                if (baseIndexOfMyItem < 0)
                {
                    // we and they added an element
                    result.Add(myItems[0]);
                    myItems.RemoveAt(0);
                    theirItems.RemoveAt(theirIndexOfMyItem);
                    continue;
                }

                if (theirIndexOfBaseItem == 0)
                {
                    // they and base are on the same page
                    // we moved the element here explicitly
                    result.Add(rebaseFun(myItems[0], baseItems[baseIndexOfMyItem], theirItems[theirIndexOfMyItem]));
                    myItems.RemoveAt(0);
                    baseItems.RemoveAt(baseIndexOfMyItem);
                    theirItems.RemoveAt(theirIndexOfMyItem);
                    continue;
                }

                result.Add(rebaseFun(myItems[myIndexOfTheirItem], baseItems[baseIndexOfTheirItem], theirItems[0]));
                myItems.RemoveAt(myIndexOfTheirItem);
                theirItems.RemoveAt(0);
                baseItems.RemoveAt(baseIndexOfTheirItem);
            }

            while (baseItems.Count > 0 && theirItems.Count > 0)
            {
                var baseIndexOfTheirItem = baseItems.FindIndex(item => equalityComparer.Equals(item, theirItems[0]));
                if (baseIndexOfTheirItem < 0)
                {
                    // they added an element here
                    result.Add(theirItems[0]);
                }
                else
                {
                    baseItems.RemoveAt(baseIndexOfTheirItem);
                }
                theirItems.RemoveAt(0);
            }

            while (myItems.Count > 0 && theirItems.Count > 0)
            {
                var myIndexOfTheirItem = myItems.FindIndex(item => equalityComparer.Equals(item, theirItems[0]));
                if (myIndexOfTheirItem < 0)
                {
                    // they added an element here
                    result.Add(theirItems[0]);
                    theirItems.RemoveAt(0);
                    continue;
                }

                var theirIndexOfMyItem = theirItems.FindIndex(item => equalityComparer.Equals(item, myItems[0]));
                if (theirIndexOfMyItem < 0)
                {
                    // we added an element here
                    result.Add(myItems[0]);
                    myItems.RemoveAt(0);
                    continue;
                }

                // we and they added an element
                result.Add(myItems[myIndexOfTheirItem]);
                theirItems.RemoveAt(0);
                myItems.RemoveAt(myIndexOfTheirItem);
            }

            result.AddRange(theirItems);
            result.AddRange(myItems);

            return result;
        }
    }
}
