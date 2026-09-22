using Microsoft.Extensions.DependencyInjection;

namespace Finite.App;

/// <summary>
/// WPF-UI IPageService implementation resolving page instances from the app's
/// DI container so pages receive services through constructor injection.
/// </summary>
public class NavigationPageProvider(IServiceProvider services) : Wpf.Ui.IPageService
{
    public TPage? GetPage<TPage>() where TPage : class
        => services.GetService<TPage>() as TPage;

    public System.Windows.FrameworkElement? GetPage(Type pageType)
        => services.GetRequiredService(pageType) as System.Windows.FrameworkElement;
}
