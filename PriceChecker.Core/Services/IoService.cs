using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Genius.PriceChecker.Core.Services
{
    public interface IIoService
    {
        bool FileExists(string path);
        string ReadTextFromFile(string path);
        void WriteTextToFile(string path, string content);
    }

    [ExcludeFromCodeCoverage]
    public class IoService : IIoService
    {
        public bool FileExists(string path)
            => File.Exists(path);

        public string ReadTextFromFile(string path)
            => File.ReadAllText(path);

        public void WriteTextToFile(string path, string content)
            => File.WriteAllText(path, content, Encoding.UTF8);
    }
}
