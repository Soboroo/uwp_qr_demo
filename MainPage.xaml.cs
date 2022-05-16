using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.ApplicationModel.DataTransfer;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.UI.Core;
using System.Diagnostics;
using System.Collections.Generic;
using Windows.Storage;
using uwp_qr_demo.Utils;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace uwp_qr_demo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DataPackage data;

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Windows.Foundation.Size(480, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            // Set XAML element as a drag region.
            Window.Current.SetTitleBar(AppTitleBar);
            AppTitleTextBlock.Text = AppInfo.Current.DisplayInfo.DisplayName;
        }
        
        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            DataPackageView currentClipboard = Clipboard.GetContent();
            if (currentClipboard != null)
            {
                data = await DataPackageViewToDataPackage(currentClipboard);
            }
            Clipboard.Clear();
            bool result = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-screenclip:snip?source=qr_demo"));
            if (result)
            {
                Window.Current.Activated += SnippingToolOpened;
            }
            else
            {
                // Failed to launch screen clip (different from didn't take a screenshot)
            }
        }

        private void SnippingToolOpened(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState != CoreWindowActivationState.Deactivated)
            {
                DataPackageView dataPackageView = Clipboard.GetContent();
                if (dataPackageView.Contains(StandardDataFormats.Bitmap))
                {
                    Debug.WriteLine("Screenshot copied to clipboard");
                    GetQRInfo(Clipboard.GetContent());
                }
                Window.Current.Activated -= SnippingToolOpened;
            }
        }

        private async void GetQRInfo(DataPackageView clipboard)
        {
            if (clipboard.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference bitmap = await clipboard.GetBitmapAsync();
                string result = await QRDecodeHelper.GetQRInfoFromBitmap(bitmap);
                Clipboard.SetContent(data);
                if (result != null && result.Length > 0)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "QR Code",
                        Content = result,
                        PrimaryButtonText = "Copy",
                        CloseButtonText = "Close"
                    };

                    dialog.PrimaryButtonClick += (s, e) =>
                    {
                        data.SetText(result);
                        Clipboard.SetContent(data);
                    };

                    await dialog.ShowAsync();
                }
                else
                {
                    var dialog = new ContentDialog
                    {
                        Title = "QR Code",
                        Content = "No QR Code found",
                        CloseButtonText = "Close"
                    };
                    await dialog.ShowAsync();
                }
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "QR Code",
                    Content = "Given Data is not a bitmap",
                    CloseButtonText = "Close"
                };
                await dialog.ShowAsync();
            }
            
        }

        private async Task<DataPackage> DataPackageViewToDataPackage(DataPackageView dataPackageView)
        {
            DataPackage dataPackage = new DataPackage();
            if (dataPackageView.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference bitmap = await dataPackageView.GetBitmapAsync();
                dataPackage.SetBitmap(bitmap);
            }
            else if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string text = await dataPackageView.GetTextAsync();
                dataPackage.SetText(text);
            }
            else if (dataPackageView.Contains(StandardDataFormats.Html))
            {
                string html = await dataPackageView.GetHtmlFormatAsync();
                dataPackage.SetHtmlFormat(html);
            }
            else if (dataPackageView.Contains(StandardDataFormats.Rtf))
            {
                string rtf = await dataPackageView.GetRtfAsync();
                dataPackage.SetRtf(rtf);
            }
            else if (dataPackageView.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> storageItems = await dataPackageView.GetStorageItemsAsync();
                dataPackage.SetStorageItems(storageItems);
            }
            else if (dataPackageView.Contains(StandardDataFormats.WebLink))
            {
                Uri webLink = await dataPackageView.GetWebLinkAsync();
                dataPackage.SetWebLink(webLink);
            }
            return dataPackage;
        }
    }
}
