using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Genius.PriceChecker.Core.Services
{
    public interface IPersister
    {
        T[] Load<T>(string filePath);
        void Store<T>(string filePath, IEnumerable<T> data);
    }

    internal sealed class Persister : IPersister
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public Persister()
        {
            _jsonOptions = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        public T[] Load<T>(string filePath)
        {
            var content = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T[]>(content, _jsonOptions);
        }

        public void Store<T>(string filePath, IEnumerable<T> data)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
    }
}
