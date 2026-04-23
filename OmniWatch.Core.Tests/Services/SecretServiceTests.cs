using OmniWatch.Core.Enums;
using OmniWatch.Core.Models;
using OmniWatch.Core.Services;
using System.Reflection;

namespace OmniWatch.Core.Tests.Services
{
    public class SecretServiceTests
    {
        private string CreateTempPath()
        {
            var dir = Path.Combine(Path.GetTempPath(), "OmniWatchTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "secrets.dat");
        }

        private SecretService CreateSut(string path)
        {
            var sut = new SecretService();

            var field = typeof(SecretService)
                .GetField("_filePath", BindingFlags.NonPublic | BindingFlags.Instance);

            field!.SetValue(sut, path);

            return sut;
        }

        // ------------------------
        // SET + GET
        // ------------------------
        [Fact]
        public async Task SetAsync_Should_Persist_And_GetAsync_Should_Return_Value()
        {
            var path = CreateTempPath();
            var sut = CreateSut(path);

            var key = new SecretKey(SecretType.ApiKey, "test");

            await sut.SetAsync(key, "abc123");

            var result = await sut.GetAsync(key);

            Assert.Equal("abc123", result);
        }

        // ------------------------
        // REMOVE
        // ------------------------
        [Fact]
        public async Task RemoveAsync_Should_Delete_Value()
        {
            var path = CreateTempPath();
            var sut = CreateSut(path);

            var key = new SecretKey(SecretType.ApiKey, "test");

            await sut.SetAsync(key, "abc123");
            await sut.RemoveAsync(key);

            var result = await sut.GetAsync(key);

            Assert.Null(result);
        }

        // ------------------------
        // GET NON EXISTENT
        // ------------------------
        [Fact]
        public async Task GetAsync_Should_Return_Null_When_Key_Does_Not_Exist()
        {
            var path = CreateTempPath();
            var sut = CreateSut(path);

            var key = new SecretKey(SecretType.ApiKey, "missing");

            var result = await sut.GetAsync(key);

            Assert.Null(result);
        }
    }
}