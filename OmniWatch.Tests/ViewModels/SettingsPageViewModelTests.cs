using Moq;
using OmniWatch.Core.Enums;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Models;
using OmniWatch.Core.Settings;
using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels.MessageDialog;
using OmniWatch.ViewModels.ProgressControl;
using OmniWatch.ViewModels.Settings;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.Tests.ViewModels;

public class SettingsPageViewModelTests
{
    private readonly Mock<ISettingsService> _settingsService = new();
    private readonly Mock<ISecretService> _secretService = new();
    private readonly Mock<ISecretResetService> _secretResetService = new();
    private readonly Mock<IMessageService> _messageService = new();
    private readonly Mock<IOpenSkyTokenManager> _tokenManager = new();
    private readonly Mock<IGlobalProgressService> _globalProgress = new();
    private readonly Mock<ILocalizationService> _localizationService = new();

    private SettingsPageViewModel CreateVM()
    {
        _settingsService.Setup(x => x.Load()).Returns(new AppSettings
        {
            OpenSkyClientId = "client-id",
            Language = "en-US",
            RefreshInterval = 15,
            UseOpenSkyCredentials = true
        });

        _secretService
            .Setup(x => x.GetAsync(It.IsAny<SecretKey>()))
            .ReturnsAsync("secret");

        return new SettingsPageViewModel(
            _settingsService.Object,
            _secretService.Object,
            _secretResetService.Object,
            new ProgressControlViewModel(_globalProgress.Object),
            _messageService.Object,
            _tokenManager.Object,
            _localizationService.Object
        );
    }

    [Fact]
    public async Task Load_Should_Populate_Properties()
    {
        var vm = CreateVM();

        await vm.Load();

        Assert.Equal("client-id", vm.OpenSkyClientId);
        Assert.Equal("secret", vm.OpenSkyClientSecret);
        Assert.Equal("en-US", vm.Language);
        Assert.Equal(15, vm.RefreshInterval);
        Assert.True(vm.UseOpenSkyCredentials);
        Assert.NotNull(vm.SelectedLanguage);
    }

    [Fact]
    public void ToggleSecretVisibility_Should_Toggle_Value()
    {
        var vm = CreateVM();

        vm.IsSecretVisible = false;
        vm.ToggleSecretVisibilityCommand.Execute(null);

        Assert.True(vm.IsSecretVisible);
    }

    [Fact]
    public void SecretVisibilityIcon_Should_Change_With_State()
    {
        var vm = CreateVM();

        vm.IsSecretVisible = true;
        Assert.Equal("\uE224", vm.SecretVisibilityIcon);

        vm.IsSecretVisible = false;
        Assert.Equal("\uE220", vm.SecretVisibilityIcon);
    }

    [Fact]
    public void UseOpenSkyCredentials_False_And_Low_Interval_Should_Reset_To_10()
    {
        var vm = CreateVM();

        vm.RefreshInterval = 5;
        vm.UseOpenSkyCredentials = false;

        Assert.Equal(10, vm.RefreshInterval);

        _messageService.Verify(x => x.ShowAsync(
            It.IsAny<string>(),
            MessageDialogType.Warning), Times.Once);
    }

    [Fact]
    public async Task TestCredentials_Should_Show_Success_Message()
    {
        var vm = CreateVM();

        _tokenManager.Setup(x => x.TestCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new OpenSkyAuthResult { Status = OpenSkyAuthStatus.Success });

        await vm.TestOpenSkyCredentialsCommand.ExecuteAsync(null);

        _messageService.Verify(x => x.ShowAsync(
            It.Is<string>(s => s.Contains("valid")),
            MessageDialogType.Success), Times.Once);
    }

    [Fact]
    public async Task Save_Should_Not_Save_When_Invalid_Interval_Without_Credentials()
    {
        var vm = CreateVM();

        vm.UseOpenSkyCredentials = false;
        vm.RefreshInterval = 5;

        vm.Save();
        await Task.Delay(50); // workaround devido ao fire-and-forget

        _settingsService.Verify(x => x.Save(It.IsAny<AppSettings>()), Times.Never);
        Assert.Equal(10, vm.RefreshInterval);
    }

    [Fact]
    public async Task Save_Should_Not_Save_When_Credentials_Missing()
    {
        var vm = CreateVM();

        vm.UseOpenSkyCredentials = true;
        vm.OpenSkyClientId = "";
        vm.OpenSkyClientSecret = "";

        vm.Save();
        await Task.Delay(50);

        _settingsService.Verify(x => x.Save(It.IsAny<AppSettings>()), Times.Never);
    }

    [Fact]
    public async Task Save_Should_Save_When_Valid()
    {
        var vm = CreateVM();

        vm.UseOpenSkyCredentials = true;
        vm.OpenSkyClientId = "id";
        vm.OpenSkyClientSecret = "secret";
        vm.RefreshInterval = 15;

        vm.Save();
        await Task.Delay(50);

        _settingsService.Verify(x => x.Save(It.Is<AppSettings>(s =>
            s.UseOpenSkyCredentials == true &&
            s.OpenSkyClientId == "id" &&
            s.RefreshInterval == 15
        )), Times.Once);

        _secretService.Verify(x => x.SetAsync(
            It.Is<SecretKey>(k => k.Type == SecretType.ApiKey),
            "secret"),
            Times.Once);
    }

    [Fact]
    public async Task Reset_Should_Not_Proceed_When_Cancelled()
    {
        _messageService.Setup(x => x.ShowAsync(It.IsAny<string>(), It.IsAny<MessageDialogType>()))
            .ReturnsAsync(MessageDialogResult.Cancel);

        var vm = CreateVM();

        await vm.ResetSettingsAsync();

        _settingsService.Verify(x => x.Save(It.IsAny<AppSettings>()), Times.Never);
    }

    [Fact]
    public async Task Reset_Should_Reset_When_Confirmed()
    {
        _messageService.Setup(x => x.ShowAsync(It.IsAny<string>(), It.IsAny<MessageDialogType>()))
            .ReturnsAsync(MessageDialogResult.Ok);

        var vm = CreateVM();

        await vm.ResetSettingsAsync();

        _settingsService.Verify(x => x.Save(It.Is<AppSettings>(s =>
            s.RefreshInterval == 10 &&
            s.UseOpenSkyCredentials == false &&
            s.Language == "en-US"
        )), Times.Once);

        _secretResetService.Verify(x => x.ResetAsync(ApiProvider.OpenSky), Times.Once);
    }

    [Fact]
    public void SelectedLanguage_Should_Update_Language()
    {
        var vm = CreateVM();

        vm.SelectedLanguage = new SettingsPageViewModel.LanguageItem
        {
            Code = "pt-PT",
            Name = "Português"
        };

        Assert.Equal("pt-PT", vm.Language);
    }
}