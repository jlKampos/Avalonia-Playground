using OmniWatch.Core.Services;
using OmniWatch.Core.Settings;
using Xunit;

namespace OmniWatch.Core.Tests.Services
{
    public class SettingsServiceTests
    {
        private SettingsService CreateSut()
        {
            return new SettingsService();
        }

        private string GetFilePath()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniWatch");

            return Path.Combine(folder, "settings.json");
        }

        private void Cleanup()
        {
            var file = GetFilePath();

            if (File.Exists(file))
                File.Delete(file);
        }

        [Fact]
        public void Load_Should_Return_Default_When_File_Does_Not_Exist()
        {
            Cleanup();

            var sut = CreateSut();

            var result = sut.Load();

            Assert.NotNull(result);
        }

        [Fact]
        public void Save_And_Load_Should_Persist_Settings()
        {
            Cleanup();

            var sut = CreateSut();

            var settings = new AppSettings
            {
                UseOpenSkyCredentials = true,
                OpenSkyClientId = "client-123"
            };

            sut.Save(settings);

            var loaded = sut.Load();

            Assert.True(loaded.UseOpenSkyCredentials);
            Assert.Equal("client-123", loaded.OpenSkyClientId);
        }

        [Fact]
        public void Save_Should_Overwrite_Previous_Settings()
        {
            Cleanup();

            var sut = CreateSut();

            sut.Save(new AppSettings
            {
                UseOpenSkyCredentials = false
            });

            sut.Save(new AppSettings
            {
                UseOpenSkyCredentials = true
            });

            var loaded = sut.Load();

            Assert.True(loaded.UseOpenSkyCredentials);
        }

        [Fact]
        public void Load_Should_Return_Default_When_File_Is_Invalid()
        {
            Cleanup();

            var file = GetFilePath();

            Directory.CreateDirectory(Path.GetDirectoryName(file)!);

            File.WriteAllText(file, "INVALID_JSON");

            var sut = CreateSut();

            var result = sut.Load();

            Assert.NotNull(result);
        }
    }
}