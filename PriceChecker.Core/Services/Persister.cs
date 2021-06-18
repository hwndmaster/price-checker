using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace Genius.PriceChecker.Core.Services
{
    public interface IPersister
    {
        T Load<T>(string filePath);
        T[] LoadCollection<T>(string filePath);
        void Store(string filePath, object data);
    }

    [ExcludeFromCodeCoverage]
    internal sealed class Persister : IPersister
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private static ReaderWriterLockSlim _locker = new();

        public Persister()
        {
            _jsonOptions = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        public T Load<T>(string filePath)
        {
            _locker.EnterReadLock();
            try
            {
                if (!File.Exists(filePath))
                {
                    return default(T);
                }
                var content = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        public T[] LoadCollection<T>(string filePath)
        {
            _locker.EnterReadLock();
            try
            {
                if (!File.Exists(filePath))
                {
                    return new T[0];
                }
                var content = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T[]>(content, _jsonOptions);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        public void Store(string filePath, object data)
        {
            _locker.EnterWriteLock();
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(filePath, json);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }
    }
}
