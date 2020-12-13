using System.Collections.Generic;
using MyNoSqlGrpc.Reader.Cache;

namespace MyNoSqlGrpc.Reader
{
    public static class DbRowChangesUtils
    {

        //ToDo - UnitTestIt
        public static IEnumerable<(RowOperationResult, T)> FindTheDifference<T>(this ReaderPartition<T> before, ReaderPartition<T> now)
        {
            if (before.Count == 0)
            {
                foreach (var nowRow in now.Get())
                    yield return (RowOperationResult.Insert, nowRow.PayLoad);
            }
            else if (now.Count == 0)
            {
                foreach (var beforeRow in before.Get())
                    yield return (RowOperationResult.Delete, beforeRow.PayLoad);
            }
            else
            {

                foreach (var nowRow in now.Get())
                {
                    var beforeRow = before.TryGet(nowRow.RowKey);
                    
                    if (beforeRow == null)
                        yield return (RowOperationResult.Insert, nowRow.PayLoad);
                    else
                    {
                        if (nowRow.TimeStamp != beforeRow.TimeStamp)
                            yield return (RowOperationResult.Update, nowRow.PayLoad);
                    }
                }

                foreach (var beforeRow in before.Get())
                {
                    var nowRow = now.TryGet(beforeRow.RowKey);
                    if (nowRow == null)
                        yield return (RowOperationResult.Delete, beforeRow.PayLoad);
                }
            }
            

        }
        
    }
}