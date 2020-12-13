using System;
using System.Collections.Generic;

namespace MyNoSqlGrpc.Reader.GrpcConnection
{
    public static class DictionaryExtensions
    {

        public static void AddRange<TKey, TValue>(this SortedDictionary<TKey, TValue> dict, IEnumerable<TValue> values,
            Func<TValue, TKey> getKey)
        {
            if (values == null)
                return;

            foreach (var value in values)
            {
                var key = getKey(value);
                dict.Add(key, value);
            }
        }
        
    }
}