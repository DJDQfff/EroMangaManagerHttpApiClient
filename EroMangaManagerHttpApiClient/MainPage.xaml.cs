
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using EroMangaManager.Core.DTOs;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.Extensions;

namespace EroMangaManagerHttpApiClient;

public sealed partial class MainPage : Page
{
    HttpClient client;
    ObservableCollection<MangasGroupDTO> groups;
    public MainPage()
    {
        this.InitializeComponent();
        client = new HttpClient
        {
            BaseAddress = new Uri("http://192.168.1.3:5000/")
        };
    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var folders = await client.GetFromJsonAsync<IEnumerable<MangasGroupDTO>>("/folders");
        groups = new ObservableCollection<MangasGroupDTO>(folders);
        navigationview.MenuItemsSource = groups;

        // var mangaList = await client.GetFromJsonAsync<ObservableCollection<MangaDTO>>("/mangas");
        //System.Diagnostics.Debug.WriteLine(mangaList.Select(x=>x.Guid).ToList());
        // gridview.ItemsSource = mangaList;
    }

    private void navigationview_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var item = args.InvokedItemContainer.DataContext as MangasGroupDTO;
        if (item != null)
        {
            gridview.ItemsSource = item.MangaDTOs;
        }
    }


    private void Image_Loaded(object sender, RoutedEventArgs e)
    {
        var image = sender as Image;
        var manga = image.DataContext as MangaDTO;
        var uri = new System.Uri($"{client.BaseAddress}covers/{manga.Guid}");
        image.Source = new BitmapImage(uri);
    }








    private async void gridview_ItemClick(object sender, ItemClickEventArgs e)
    {

        var manga = e.ClickedItem as MangaDTO;
        var container = gridview.ContainerFromItem(manga) as GridViewItem;

        var storagefile = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{manga.Guid}.zip", Windows.Storage.CreationCollisionOption.ReplaceExisting);

        // 此方法在安卓端错误
        //var root = container.ContentTemplateRootas StackPanel;
        //var progressbar = root?.FindName("progressbar") as ProgressBar;

        var progressbar = container?.FindFirstDescendant<ProgressBar>();

        // 直接下载到文件
        //var file = await client.GetStreamAsync($"/downloads/{manga.Guid}");
        //using (var stream = await storagefile.OpenStreamForWriteAsync())
        //{
        //    await file.CopyToAsync(stream);
        //}
        var count = 0;

        IProgress<float> progress = new Progress<float>(value =>
        {
            // 这里的代码会自动运行在 UI 线程上
            progressbar.Value = value;

        });
        // 1. 先获取响应头，不要直接读内容流
        var response = await client.GetAsync($"/downloads/{manga.Guid}", HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        progressbar.Visibility = Visibility.Visible;

        // 2. 获取总大小
        var totalBytes = response.Content.Headers.ContentLength;
        progressbar.Maximum = (double)totalBytes;

        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var fileStream = await storagefile.OpenStreamForWriteAsync())
        {
            var buffer = new byte[65536]; // 64KB 缓冲区，提高下载速度。可选1048576
            long bytesRead = 0;
            int read;

            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                bytesRead += read;

                // 3. 报告进度 (如果是 null 则跳过)
                if (totalBytes.HasValue && progress != null)
                {
                    //float percent =  (float)bytesRead / totalBytes.Value*100; // 计算百分比，这是默认progressbar是最大100的时候，不用了
                    progress.Report(bytesRead);
                }
            }
        }

        AndroidOperation.Open(storagefile);

    }
}
