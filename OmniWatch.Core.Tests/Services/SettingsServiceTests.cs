using OmniWatch.Core.Services;
using OmniWatch.Core.Settings;
using Xunit;

namespace OmniWatch.Core.Tests.Services
{
    public class SettingsServiceTests
    {
        private string CreateTempPath()
        {
            var dir = Path.Combine(Path.GetTempPath(), "OmniWatchTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }

        private SettingsService CreateSut(string path)
        {
            return new SettingsService(path); // usa o construtor interno
        }

        [Fact]
        public void Load_Should_Return_Default_When_File_Does_Not_Exist()
        {
            var path = CreateTempPath();
            var sut = CreateSut(path);

            var result = sut.Load();

            Assert.NotNull(result);
        }

        [Fact]
        public void Save_And_Load_Should_Persist_Settings()
        {
            var path = CreateTempPath();
            var sut = CreateSut(path);

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
            var path = CreateTempPath();
            var sut = CreateSut(path);

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
            var path = CreateTempPath();

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "INVALID_JSON");

            var sut = CreateSut(path);

            var result = sut.Load();

            Assert.NotNull(result);
        }
    }
}
