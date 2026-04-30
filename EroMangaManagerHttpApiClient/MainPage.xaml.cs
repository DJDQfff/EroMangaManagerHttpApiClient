
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
    int currentpageindex;
    public MainPage()
    {
        this.InitializeComponent();
        client = new HttpClient
        {
            BaseAddress = new Uri("http://192.168.1.2:5000/")
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



    private async void gridview_ItemClick(object sender, ItemClickEventArgs e)
    {

        var manga = e.ClickedItem as MangaDTO;
        var container = gridview.ContainerFromItem(manga) as GridViewItem;

        var storagefile = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync($"{manga.Name}.zip", Windows.Storage.CreationCollisionOption.ReplaceExisting);



        // 直接下载到文件
        var file = await client.GetStreamAsync($"/downloads/{manga.Guid}");
        var progressring = container?.FindFirstDescendant<ProgressRing>();
        progressring.Visibility = Visibility.Visible;

        using (var stream = await storagefile.OpenStreamForWriteAsync())
        {
            await file.CopyToAsync(stream);
        }
        progressring.Visibility = Visibility.Collapsed;
        AndroidOperation.Open(storagefile);

        return;
        //TODO 下面可以显示下载进度，但安卓端有问题，暂时先直接下载后打开
        // 1.下载速度跑不满
        //2. progress.Report()调用会影响下载速度。，即便不使用progress.Report()，下载速度也只有2M多，怀疑是因为每次读写都要调用UI线程的关系。
        // 3.频繁调用性能网速更低，怀疑是因为每次读写都要调用UI线程的关系。

        // 此方法在安卓端错误
        //var root = container.ContentTemplateRootas StackPanel;
        //var progressbar = root?.FindName("progressbar") as ProgressBar;

        var progressbar = container?.FindFirstDescendant<ProgressBar>();

        IProgress<float> progress = new Progress<float>(value =>
        {
            // 这里的代码会自动运行在 UI 线程上
            progressbar.Value = value;

        });
        // 1. 先获取响应头，不要直接读内容流
        var response = await client.GetAsync($"/downloads/{manga.Guid}", HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        // 2. 获取总/近似大小(文件夹则按近似大小，单文件则按实际大小)，以之为进度条最大值。
        long estimatedSize = 0;
        if (response.Content.Headers.ContentLength.HasValue)
        {
            estimatedSize = response.Content.Headers.ContentLength.Value;
        }
        else if (response.Headers.TryGetValues("X-Estimated-Size", out var values))
        {
            estimatedSize = long.Parse(values.FirstOrDefault());
        }
        // 若大小为0，则不启用进度条
        if (estimatedSize != 0)
        {
            progressbar.Visibility = Visibility.Visible;
            progressbar.Maximum = (double)estimatedSize;

        }


        using var networkstream = await response.Content.ReadAsStreamAsync();
        using (var fileStream = await storagefile.OpenStreamForWriteAsync())
        {
            var buffer = new byte[1048576]; // 64KB 缓冲区，提高下载速度。可选 1048576  65536
            long bytesRead = 0;
            int read;

            while ((read = await networkstream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);

                bytesRead += read;

                // 3. 报告进度 (如果是 null 则跳过)
                //if (estimatedSize > 0 && progress != null)
                //{
                //    // 计算百分比，这是默认progressbar是最大100的时候，不用了
                //    //float percent =  (float)bytesRead / totalBytes.Value*100; 

                //    progress.Report(bytesRead);
                //}
            }
            //progress.Report(estimatedSize); // 确保最后报告完成

        }

        AndroidOperation.Open(storagefile);

    }

    private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuFlyoutItem;

        var manga = menuItem.DataContext as MangaDTO;
        var group = navigationview.SelectedItem as MangasGroupDTO;
        if (group != null)
        {
            group.MangaDTOs.Remove(manga);
            numberbox.Maximum = (group.MangaDTOs.Count + 19) / 20;

        }
        var response = await client.DeleteAsync($"/mangas/{manga.Guid}");
        response.EnsureSuccessStatusCode();

    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedGroup = navigationview.SelectedItem as MangasGroupDTO;
        if (selectedGroup != null)
        {
            var current = (int)numberbox.Value;
            var mangadtos = selectedGroup.MangaDTOs.Skip((current - 1) * 20).Take(20).ToList();
            gridview.ItemsSource = mangadtos;
        }
    }



    private void Image_Loaded(object sender, RoutedEventArgs e)
    {
        var image = sender as Image;
        var manga = image.DataContext as MangaDTO;
        var uri = new System.Uri($"{client.BaseAddress}covers/{manga.Guid}");
        image.Source = new BitmapImage(uri);
    }

    private void navigationview_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var groups = navigationview.SelectedItem as MangasGroupDTO;
       
        numberbox.Maximum = (groups.MangaDTOs.Count + 19) / 20;
        numberbox.Value = 1;
        var mangadtos = groups.MangaDTOs.Take(20).ToList();
        gridview.ItemsSource = mangadtos;
    }
}
