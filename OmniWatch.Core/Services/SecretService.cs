using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace OmniWatch.Core.Services
{
    public class SecretService : ISecretService
    {

        private readonly string _filePath;
        private readonly object _lock = new();

        public SecretService()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniWatch");

            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "secrets.dat");
        }

        internal SecretService(string filePath)
        {
            _filePath = filePath;
        }

        public Task SetAsync(SecretKey key, string value)
        {
            lock (_lock)
            {
                var all = LoadAll();
                all[key.ToStorageKey()] = value;
                SaveAll(all);
            }

            return Task.CompletedTask;
        }

        public Task<string?> GetAsync(SecretKey key)
        {
            lock (_lock)
            {
                var all = LoadAll();
                return Task.FromResult(
                    all.TryGetValue(key.ToStorageKey(), out var value)
                        ? value
                        : null
                );
            }
        }

        public Task RemoveAsync(SecretKey key)
        {
            lock (_lock)
            {
                var all = LoadAll();
                if (all.Remove(key.ToStorageKey()))
                    SaveAll(all);
            }

            return Task.CompletedTask;
        }


        // -------------------------

        private Dictionary<string, string> LoadAll()
        {
            if (!File.Exists(_filePath))
                return new();

            var encrypted = File.ReadAllBytes(_filePath);

            var decrypted = ProtectedData.Unprotect(
                encrypted, null, DataProtectionScope.CurrentUser);

            var json = Encoding.UTF8.GetString(decrypted);

            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new();
        }

        private void SaveAll(Dictionary<string, string> data)
        {
            var json = JsonSerializer.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(json);

            var encrypted = ProtectedData.Protect(
                bytes, null, DataProtectionScope.CurrentUser);

            var tempFile = _filePath + ".tmp";
            File.WriteAllBytes(tempFile, encrypted);
            File.Move(tempFile, _filePath, true);
        }
    }
}

