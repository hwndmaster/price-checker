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

    internal sealed class Persister : IPersister
    {
        private readonly IIoService _io;
        private readonly JsonSerializerOptions _jsonOptions;
        private static ReaderWriterLockSlim _locker = new();

        public Persister(IIoService io)
        {
            _io = io;
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
                if (!_io.FileExists(filePath))
                {
                    return default(T);
                }
                var content = _io.ReadTextFromFile(filePath);
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
                if (!_io.FileExists(filePath))
                {
                    return new T[0];
                }
                var content = _io.ReadTextFromFile(filePath);
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
                _io.WriteTextToFile(filePath, json);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }
    }
}
