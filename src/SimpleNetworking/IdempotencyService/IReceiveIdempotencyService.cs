using System;

namespace SimpleNetworking.IdempotencyService
{
    public interface IReceiveIdempotencyService<T> : IDisposable
    {
        bool Add(T value);
        bool Remove(T value);
        bool Find(T value);
    }
}