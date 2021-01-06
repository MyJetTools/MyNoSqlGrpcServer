using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyNoSqlGrpc.Engine
{
    
    public struct AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _src;


        public AsyncEnumerator(IEnumerable<T> src)
        {
            _src = src.GetEnumerator();
        }
            
        public ValueTask DisposeAsync()
        {
            return new ();
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return new (_src.MoveNext());
        }

        public T Current => _src.Current;
    }
    
    
    public struct AsyncEnumerableResult<T> : IAsyncEnumerable<T>
    {
        private readonly IEnumerable<T> _src;

        public AsyncEnumerableResult(IEnumerable<T> src)
        {
            _src = src;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new ())
        {
            return new AsyncEnumerator<T>(_src);
        }

        public static AsyncEnumerableResult<T> Empty()
        {
            return new (Array.Empty<T>());
        }
    }
}