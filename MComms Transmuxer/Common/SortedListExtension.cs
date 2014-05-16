namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class SortedListExtension
    {
        public static int FindFirstIndexLessThanOrEqualTo<T, U>(this SortedList<T, U> sortedList, T key)
        {
            return BinarySearch(sortedList.Keys, key, true);
        }

        public static int FindFirstIndexGreaterThan<T, U>(this SortedList<T, U> sortedList, T key)
        {
            return BinarySearch(sortedList.Keys, key, false);
        }

        private static int BinarySearch<T>(IList<T> list, T value, bool lessOrEqual)
        {
            if (list == null) throw new ArgumentNullException("list");

            if (list.Count == 0)
            {
                if (lessOrEqual)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }

            Comparer<T> comp = Comparer<T>.Default;

            int lo = 0;
            int hi = list.Count - 1;
            while (lo < hi)
            {
                int m = (hi + lo) / 2;
                if (comp.Compare(list[m], value) < 0)
                {
                    lo = m + 1;
                }
                else
                {
                    hi = m - 1;
                }
            }

            int res = 0;
            if (lessOrEqual)
            {
                if (lo > list.Count - 1)
                {
                    lo = list.Count - 1;
                }

                if (comp.Compare(list[lo], value) > 0)
                {
                    res = lo - 1;
                }
                else
                {
                    res = lo;
                }
            }
            else
            {
                if (hi < 0)
                {
                    hi = 0;
                }

                if (comp.Compare(list[hi], value) <= 0)
                {
                    res = hi + 1;
                }
                else
                {
                    res = hi;
                }
            }

            return res;
        }
    }
}
