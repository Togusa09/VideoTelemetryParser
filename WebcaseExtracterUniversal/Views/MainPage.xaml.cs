using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Media.Editing;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WebcaseExtracterUniversal.Views
{
    struct FlightPoint
    {
        public TimeSpan Time;
        public decimal Altitude;
        public decimal Velocity;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        public async Task DoStuff()
        {
            ProgressBar.Visibility = Visibility.Visible;

            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            var startTime = new TimeSpan(0, 19, 30);
            var endTime = new TimeSpan(0, 28, 00);

            //00070 at 1566,215,98,25
            //00.8 at 1770,213,69,25
            var speedRect = new Rect(1550, 210, 120, 35);
            //var speedRect = new Rect( 1566, 215, 97, 25 );
            var altitudeRect = new Rect(1750, 210, 90, 35);


            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".mp4");

            var flight = new List<FlightPoint>();

            StorageFile file = await openPicker.PickSingleFileAsync();

            var fileSavePicker = new FileSavePicker
            {
                SuggestedFileName = file.DisplayName,
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            fileSavePicker.FileTypeChoices.Add("CSV File", new List<string> { ".csv" });

            StorageFile csvFile = await fileSavePicker.PickSaveFileAsync();

            var mediaClip = await MediaClip.CreateFromFileAsync(file);
            var mediaComposition = new MediaComposition();
            mediaComposition.Clips.Add(mediaClip);

            var ocrTime = endTime - startTime;
            var ocrSeconds = (int) ocrTime.TotalSeconds;

            var speed = 0m;
            var altitude = 0m;
            this.ProgressBar.Maximum = ocrSeconds;

            var width = PreviewImage.Width;
            var height = PreviewImage.Height;

            var encodingProperties = mediaClip.GetVideoEncodingProperties();
            var videoWidth = encodingProperties.Width;
            var videoHeight = encodingProperties.Height;

            var widthFactor = (width / videoWidth);
            var heightFactor = height / videoHeight;

            OutlineOcrSection(speedRect, widthFactor, heightFactor);
            OutlineOcrSection(altitudeRect, widthFactor, heightFactor);


            for (var i = 0; i < ocrSeconds; i++)
            {
                var currentTime = startTime + TimeSpan.FromSeconds(i);
                var thumbnail = await mediaComposition.GetThumbnailAsync(currentTime, 0, 0, VideoFramePrecision.NearestFrame);
                var decoder = await BitmapDecoder.CreateAsync(thumbnail);
                var bitmap = await decoder.GetSoftwareBitmapAsync();

                await SetPreviewImage(bitmap);

                var result = await ocrEngine.RecognizeAsync(bitmap);
                
                bool velPassed = false;
                bool speedPassed = false;

                foreach (var line in result.Lines)
                {
                    // Iterate over words in line.
                    foreach (var word in line.Words)
                    {

                        //if (speedRect.Contains(new Point(word.BoundingRect.X + word.BoundingRect.Width / 2, word.BoundingRect.Y + word.BoundingRect.Height / 2)))
                        //    //&& speedRect.Contains(new Point(word.BoundingRect.X + word.BoundingRect.Width, word.BoundingRect.Y + word.BoundingRect.Height)))
                        //{
                        //    decimal tempSpeed;
                        //    if (decimal.TryParse(word.Text, out tempSpeed))
                        //    {
                        //        speed = Math.Abs(tempSpeed);
                        //        this.Velocity.Text = speed.ToString(CultureInfo.InvariantCulture);
                        //        velPassed = true;
                        //    }
                            
                        //}

                        //if (altitudeRect.Contains(new Point(word.BoundingRect.X, word.BoundingRect.Y)))
                        //    //&& speedRect.Contains(new Point(word.BoundingRect.X + word.BoundingRect.Width, word.BoundingRect.Y + word.BoundingRect.Height)))
                        //{
                        //    decimal tempAltitude;
                        //    if (decimal.TryParse(word.Text, out tempAltitude))
                        //    {
                        //        altitude = Math.Abs(tempAltitude);
                        //        Altitude.Text = altitude.ToString(CultureInfo.InvariantCulture);
                        //        speedPassed = true;
                        //    }
                        //}
                    }
                }

                if (!speedPassed || !velPassed)
                {
                    Debug.WriteLine(result.Text);
                }

                var time = TimeSpan.FromSeconds(i);
                this.Time.Text = time.ToString();

                var point = new FlightPoint { Velocity = speed, Altitude = altitude, Time = time };
                Debug.WriteLine($"{point.Time}, {point.Altitude}, {point.Velocity}");
                flight.Add(point);

                ProgressBar.Value = i;
                PreviewImage.RenderTransform = null;
            }

            // Get information about the preview
            //var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

            using (IRandomAccessStream stream = await csvFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var dataWriter = new Windows.Storage.Streams.DataWriter(stream))
                {
                    dataWriter.WriteString("time,altitude,velocity,acceleration");
                    decimal prevVelocity = 0;
                    var prevTime = TimeSpan.Zero;
                    foreach (var entry in flight)
                    {
                        var timeDelta = (decimal) (entry.Time - prevTime).TotalSeconds;
                        var ac = 0m;
                        if (timeDelta > 0)
                        {
                            ac = (entry.Velocity - prevVelocity) / timeDelta;
                        }

                        prevVelocity = entry.Velocity;
                        prevTime = entry.Time;
                        
                        dataWriter.WriteString($"{entry.Time}, {entry.Altitude}, {entry.Velocity}, {ac}{Environment.NewLine}");
                    }
                    await dataWriter.StoreAsync();
                    await stream.FlushAsync();
                }
            }

            this.ProgressBar.Visibility = Visibility.Collapsed;
        }

        private void OutlineOcrSection(Rect speedRect, double widthFactor, double heightFactor)
        {
            var rectangle1 = new Rectangle();
            rectangle1.Fill = new SolidColorBrush(Windows.UI.Colors.Transparent);
            rectangle1.Width = speedRect.Width * widthFactor;
            rectangle1.Height = speedRect.Height * heightFactor;
            rectangle1.Stroke = new SolidColorBrush(Windows.UI.Colors.Red);
            rectangle1.StrokeThickness = 3;

            Canvas1.Children.Add(rectangle1);
            Canvas.SetLeft(rectangle1, speedRect.Left * widthFactor);
            Canvas.SetTop(rectangle1, speedRect.Top * heightFactor);
            Canvas.SetZIndex(rectangle1, 1);
        }

        private async Task SetPreviewImage(SoftwareBitmap bitmap)
        {
            if (bitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
                bitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
            {
                bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }
            

            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(bitmap);

            // Set the source of the Image control
            PreviewImage.Source = source;
        }

        internal class FileExtensions
        {
            public static readonly string[] Video = new string[] { ".mp4", ".wmv" };
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await DoStuff();
        }
    }
}
