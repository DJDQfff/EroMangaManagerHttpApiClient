
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using EroMangaManager.Core.DTOs;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Extensions;
using Windows.System;

namespace EroMangaManagerHttpApiClient;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }
    override protected void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (App.MangaClient == null)
        {
            mainFrame.Navigate(typeof(ConnectPage));
        }
        else
        {
            mainFrame.Navigate(typeof(NavigationPage));
        }
    }
}
