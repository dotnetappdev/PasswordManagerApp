namespace VaultGuard.Mobile.Presentation;

public partial record MainModel
{
    private INavigator _navigator;

    public MainModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        INavigator navigator)
    {
        _navigator = navigator;
        Title = "Main";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";
    }

    public string? Title { get; }

    public string Name { get; set; } = string.Empty;

    public async Task GoToSecond()
    {
        await _navigator.NavigateViewModelAsync<SecondModel>(this, data: new Entity(Name!));
    }

}
