using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServerApp
{
    internal class SocketAsyncEventArgsPool
    {
        private readonly ConcurrentBag<SocketAsyncEventArgs> pool;
        private readonly int maxPoolSize;

        public SocketAsyncEventArgsPool(int maxPoolSize)
        {
            this.maxPoolSize = maxPoolSize;
            pool = new ConcurrentBag<SocketAsyncEventArgs>();

            for (int i = 0; i < maxPoolSize; i++)
            {
                this.pool.Add(new SocketAsyncEventArgs());
            }
        }
        public SocketAsyncEventArgs Rent()
        {
            if (this.pool.TryTake(out var item))
                return item;

            return new SocketAsyncEventArgs();
        }
        public void Return(SocketAsyncEventArgs item)
        {
            this.pool.Add(item);
        }

        public void ClearPool()
        {
            while (this.pool.TryTake(out var item))
            {
                item.Dispose();
            }
        }
    }
}
