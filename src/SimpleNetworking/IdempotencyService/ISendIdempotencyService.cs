using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleNetworking.IdempotencyService
{
    public interface ISendIdempotencyService<K, T> : IDisposable
    {
        Task<bool> Add(K token, T item);
        bool Remove(K token, out T item);
        IEnumerable<T> GetValues();
    }
}