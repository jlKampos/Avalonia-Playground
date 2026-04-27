using Microsoft.AspNetCore.DataProtection;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Models;
using System.Text;
using System.Text.Json;

namespace OmniWatch.Core.Services
{
    public class SecretService : ISecretService
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private readonly IDataProtector _protector;

        public SecretService(IDataProtectionProvider provider)
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniWatch");

            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "secrets.dat");

            _protector = provider.CreateProtector("OmniWatch.SecretService.v1");
        }

        internal SecretService(string filePath, IDataProtectionProvider provider) : this(provider)
        {
            _filePath = filePath;
        }

        internal SecretService(string filePath, IDataProtectionProvider provider) : this(provider)
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
                    all.TryGetValue(key.ToStorageKey(), out var value) ? value : null
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

        private Dictionary<string, string> LoadAll()
        {
            if (!File.Exists(_filePath))
                return new();

            try
            {
                var encryptedData = File.ReadAllText(_filePath);
                var decryptedJson = _protector.Unprotect(encryptedData);

                return JsonSerializer.Deserialize<Dictionary<string, string>>(decryptedJson) ?? new();
            }
            catch (Exception)
            {
                return new();
            }
        }

        private void SaveAll(Dictionary<string, string> data)
        {
            var json = JsonSerializer.Serialize(data);

            var encrypted = _protector.Protect(json);

            var tempFile = _filePath + ".tmp";
            File.WriteAllText(tempFile, encrypted);
            File.Move(tempFile, _filePath, true);
        }
    }
}