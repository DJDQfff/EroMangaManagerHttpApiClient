using System;
using System.Collections.Generic;
using System.Text;


using Windows.Storage;
namespace EroMangaManagerHttpApiClient;

public static class ServerStorage
{
    private const string Key = "last_server_url";

    public static string LoadLastServer()
    {
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(Key, out object value))
        {
            return value?.ToString() ?? string.Empty;
        }
        return string.Empty;
    }

    public static void SaveServer(string url)
    {
        ApplicationData.Current.LocalSettings.Values[Key] = url.TrimEnd('/');
    }
}
