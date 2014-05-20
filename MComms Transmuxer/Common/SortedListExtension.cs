namespace MComms_Transmuxer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Extensions for SortedList class
    /// </summary>
    public static class SortedListExtension
    {
        /// <summary>
        /// Find first index of the key which is less or equal to the specified key
        /// </summary>
        /// <typeparam name="T">SortedList's key type</typeparam>
        /// <typeparam name="U">SortedList's value type</typeparam>
        /// <param name="sortedList">Sorted list to find data in</param>
        /// <param name="key">Key to find</param>
        /// <returns>
        /// Index of the found key. If key parameter is less than the list's first key then -1 will be returned
        /// </returns>
        public static int FindFirstIndexLessThanOrEqualTo<T, U>(this SortedList<T, U> sortedList, T key)
        {
            return BinarySearch(sortedList.Keys, key, true);
        }

        /// <summary>
        /// Find first index of the key which is greater than the specified value
        /// </summary>
        /// <typeparam name="T">SortedList's key type</typeparam>
        /// <typeparam name="U">SortedList's value type</typeparam>
        /// <param name="sortedList">Sorted list to find data in</param>
        /// <param name="key">Key to find</param>
        /// <returns>
        /// Index of the found key. If key parameter is more than the list's first key then sortedList.Count will be returned
        /// </returns>
        public static int FindFirstIndexGreaterThan<T, U>(this SortedList<T, U> sortedList, T key)
        {
            return BinarySearch(sortedList.Keys, key, false);
        }

        /// <summary>
        /// Performs binary search in the specified sorted list
        /// </summary>
        /// <typeparam name="T">List's value type</typeparam>
        /// <param name="list">Sorted list to find data in</param>
        /// <param name="value">Value to find</param>
        /// <param name="lessOrEqual">
        /// True if the found value has to be less or equal than value to find.
        /// False if the found value has to be greater than value to find.
        /// </param>
        /// <returns>Index of the found value.</returns>
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
