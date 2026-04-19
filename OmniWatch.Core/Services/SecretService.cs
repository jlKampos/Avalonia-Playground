using OmniWatch.Core.Interfaces;
using System.Text;
using System.Security.Cryptography;
namespace OmniWatch.Core.Services
{
    public class SecretService : ISecretService
    {
        private readonly string _filePath;

        public SecretService()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniWatch");

            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "secret.dat");
        }

        public void Save(string value)
        {
            var data = Encoding.UTF8.GetBytes(value);
            var encrypted = ProtectedData.Protect(
                data, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(_filePath, encrypted);
        }


        public string Load()
        {
            if (!File.Exists(_filePath))
                return null;

            var data = File.ReadAllBytes(_filePath);
            var decrypted = ProtectedData.Unprotect(
                data, null, DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decrypted);
        }

    }
}
