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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace uwp_qr_demo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public sealed partial class MainPage : Page
    {
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

            OpenCvSharp.Mat mat;
            string qrResult = "";
            Clipboard.ContentChanged += async (s, e) =>
            {
                try
                {
                    DataPackageView dataPackageView = Clipboard.GetContent();
                    string copiedFormat = dataPackageView.AvailableFormats.FirstOrDefault();
                    if (copiedFormat == "Bitmap")
                    {
                        // Get QR code from clipboard using OpenCV
                        RandomAccessStreamReference bitmap = await dataPackageView.GetBitmapAsync();
                        IRandomAccessStream stream = await bitmap.OpenReadAsync();
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                        SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                        mat = SoftwareBitmap2Mat(softwareBitmap);

                        OpenCvSharp.QRCodeDetector qRCodeDetector = new OpenCvSharp.QRCodeDetector();
                        OpenCvSharp.Point2f[] points;
                        qrResult = qRCodeDetector.DetectAndDecode(mat, out points);

                        // Display QR code result
                        if (qrResult != "")
                        {
                            QrResultTextBlock.Text = qrResult;
                        }
                    }
                }
                catch (Exception ex)
                {
                    qrResult = "";
                }
            };
        }
        // https://github.com/shimat/opencvsharp_samples/blob/master/CameraOpenCV/OcvOp.cs
        public static unsafe OpenCvSharp.Mat SoftwareBitmap2Mat(SoftwareBitmap softwareBitmap)
        {
            using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            {
                using (var reference = buffer.CreateReference())
                {
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out var dataInBytes, out var capacity);

                    OpenCvSharp.Mat outputMat = new OpenCvSharp.Mat(softwareBitmap.PixelHeight, softwareBitmap.PixelWidth, OpenCvSharp.MatType.CV_8UC4, (IntPtr)dataInBytes);
                    return outputMat;
                }
            }
        }
    }
}
