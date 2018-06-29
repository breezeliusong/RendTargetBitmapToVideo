using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RendTargetBitmapToVideo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            List<WriteableBitmap> list = new List<WriteableBitmap>();

            WriteableBitmap bitmap1 = await GetWriteableBitmapAsync(new Uri("ms-appx:///Assets/image.jpg"));
            WriteableBitmap bitmap2 = await GetWriteableBitmapAsync(new Uri("ms-appx:///Assets/image1.jpg"));
            list.Add(bitmap1);
            list.Add(bitmap2);
            CreateVideoFromWritableBitmapAsync(list);

        }
        private async Task<WriteableBitmap> GetWriteableBitmapAsync(Uri uri)
        {
            var file1 = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var stream = await file1.OpenReadAsync();
            WriteableBitmap writeableBitmap = new WriteableBitmap(800, 600);
            writeableBitmap.SetSource(stream);
            return writeableBitmap;
        }

        private async void CreateVideoFromWritableBitmapAsync(List<WriteableBitmap> WBs)
        {
            var mediaComposition = new MediaComposition();
            foreach (WriteableBitmap WB in WBs)
            {
                var pixels = WB.PixelBuffer.ToArray();
                CanvasBitmap bitmap = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixels,
                        800, 600, DirectXPixelFormat.B8G8R8A8UIntNormalized);
                MediaClip mediaClip = MediaClip.CreateFromSurface(bitmap, TimeSpan.FromSeconds(1));
                mediaComposition.Clips.Add(mediaClip);
            }
            //Save the video
            var file =await ApplicationData.Current.LocalFolder.CreateFileAsync("WBVideo.mp4");
            await mediaComposition.SaveAsync(file);
            mediaComposition=await MediaComposition.LoadAsync(file);
            await mediaComposition.RenderToFileAsync(file);
        }



        //This works
        private async void CreateVideoFromIDX3D()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var folder = ApplicationData.Current.LocalFolder;
                    RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                    await renderTargetBitmap.RenderAsync(RenderGrid);

                    IBuffer pixels = await renderTargetBitmap.GetPixelsAsync();
                    CanvasBitmap bitmap = null;
                    var videoClip = new MediaComposition();
                    //SoftwareBitmap softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, renderTargetBitmap.PixelWidth,
                    //    renderTargetBitmap.PixelHeight);
                    //bitmap = CanvasBitmap.CreateFromSoftwareBitmap(CanvasDevice.GetSharedDevice(), softwareBitmap);

                    bitmap = CanvasBitmap.CreateFromBytes(CanvasDevice.GetSharedDevice(), pixels,
                    renderTargetBitmap.PixelWidth, renderTargetBitmap.PixelHeight, DirectXPixelFormat.B8G8R8A8UIntNormalized);
                    StorageFile video2 = await folder.CreateFileAsync("video2" + ".mp4", CreationCollisionOption.ReplaceExisting);
                    MediaClip d = MediaClip.CreateFromSurface(bitmap, TimeSpan.FromSeconds(3));
                    videoClip.Clips.Add(d);

                    //Use these code to work
                    await videoClip.SaveAsync(video2);
                    videoClip = await MediaComposition.LoadAsync(video2);
                    var result = await videoClip.RenderToFileAsync(video2);
                    Debug.WriteLine(result.ToString());
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }


        //This can work

        private async void CreateVideoByConvertRenderBitmapToFile()
        {
            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Test",
                CreationCollisionOption.ReplaceExisting);
            var composition = new MediaComposition();
            for (int i = 0; i < 5; i++)
            {
                RenderTargetBitmap render = new RenderTargetBitmap();
                await render.RenderAsync(RenderGrid);
                MyImage.Source = render;
                var pixel = await render.GetPixelsAsync();
                var file = await folder.CreateFileAsync("test.png", CreationCollisionOption.GenerateUniqueName);
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                        (uint)render.PixelWidth,
                        (uint)render.PixelHeight,
                        logicalDpi,
                        logicalDpi,
                        pixel.ToArray());
                    await encoder.FlushAsync();
                    stream.Dispose();
                    MediaClip clip = await MediaClip.CreateFromImageFileAsync(file, TimeSpan.FromSeconds(3));
                    composition.Clips.Add(clip);
                    MyText.Text = "First frame >>>" + i;
                }
            }
            var video = await ApplicationData.Current.LocalFolder.CreateFileAsync("test.mp4",
                CreationCollisionOption.ReplaceExisting);
            var action = await composition.RenderToFileAsync(video, MediaTrimmingPreference.Precise);
            await folder.DeleteAsync();
        }
    }
}
