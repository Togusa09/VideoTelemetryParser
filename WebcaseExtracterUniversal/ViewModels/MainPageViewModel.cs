using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Template10.Mvvm;
using WebcaseExtracterUniversal.Models;

namespace WebcaseExtracterUniversal.ViewModels
{
    public class MainPageViewModel : Template10.Mvvm.ViewModelBase
    {
        public MainPageViewModel()
        {
            Test = "Test Text";
            RectsItems = new ObservableCollection<Rect>();

            _ocrAreas.Add(new OcrArea { Name = "Velocity", Area = new Rect(1550, 210, 120, 35) });
            _ocrAreas.Add(new OcrArea { Name = "Altitude", Area = new Rect(1750, 210, 90, 35) });

            OutlineOcrSection(_ocrAreas[0].Area, 0.4, 0.4);
            OutlineOcrSection(_ocrAreas[1].Area, 0.4, 0.4);

            ProgressBarVisibility = Visibility.Collapsed;
            VideoLength = 1;
            CurrentVideoPosition = 0;
            
        }

        private string _test;
        public string Test
        {
            get { return _test; }
            set { Set(ref _test, value); }
        }

        private readonly ObservableCollection<OcrArea> _ocrAreas = new ObservableCollection<OcrArea>();
        public ObservableCollection<OcrArea> OcrAreas => _ocrAreas;

        private Visibility _progressBarVisibility;
        public Visibility ProgressBarVisibility {
            get { return _progressBarVisibility; }
            set { Set(ref _progressBarVisibility, value); }
        }

        private int _videoLength;
        public int VideoLength
        {
            get { return _videoLength; }
            set { Set(ref _videoLength, value); }
        }

        private int _currentVideoPosition;
        public int CurrentVideoPosition
        {
            get { return _currentVideoPosition; }
            set { Set(ref _currentVideoPosition, value); }
        }

        private TimeSpan _currentTime;

        public TimeSpan CurrentTime
        {
            get { return _currentTime;}
            set { Set(ref _currentTime, value); }
        }

        private StorageFile _videoFile = null;
        public StorageFile VideoFile
        {
            get { return _videoFile; }
            set
            {
                Set(ref _videoFile, value);
                // Need to define length boundaries
                // Need to adjust video length and screenshot
            }
        }

        private StorageFile _csvFile = null;

        public StorageFile CsvFile
        {
            get { return _csvFile;}
            set { Set(ref _videoFile, value); }
        }


        private Dictionary<string, string> _parsedValues;
        public Dictionary<string, string> ParsedValues
        {
            get { return _parsedValues; }
            set { Set(ref _parsedValues, value); }
        }

        private AwaitableDelegateCommand _selectVideoCommand;
        public AwaitableDelegateCommand SelectVideoCommand => _selectVideoCommand ?? (_selectVideoCommand = new AwaitableDelegateCommand(SelectVideo));

        public async Task SelectVideo(AwaitableDelegateCommandParameter arg)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".mp4");
            VideoFile = await openPicker.PickSingleFileAsync();

            SelectOutputFileCommand.RaiseCanExecuteChanged();
        }

        private AwaitableDelegateCommand _selectOutputFileCommand;
        public AwaitableDelegateCommand SelectOutputFileCommand => _selectOutputFileCommand ?? (_selectOutputFileCommand = new AwaitableDelegateCommand(SelectOutputFile, CanSelectOutputFile));

        public async Task SelectOutputFile(AwaitableDelegateCommandParameter arg)
        {
            var fileSavePicker = new FileSavePicker
            {
                SuggestedFileName = VideoFile.DisplayName,
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            fileSavePicker.FileTypeChoices.Add("CSV File", new List<string> { ".csv" });

            _csvFile = await fileSavePicker.PickSaveFileAsync();
            //ProcessVideoCommand.RaiseCanExecuteChanged();
            RaisePropertyChanged(nameof(CanProcessVideo));
        }

        public bool CanSelectOutputFile(AwaitableDelegateCommandParameter arg)
        {
            return VideoFile != null;
        }

        public bool CanProcessVideo =>  VideoFile != null && _csvFile != null;
        

        public async Task ExecuteProcessVideo()
        {
            ProgressBarVisibility = Visibility.Visible;

            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            var startTime = new TimeSpan(0, 19, 30);
            var endTime = new TimeSpan(0, 28, 00);

            var flight = new List<Dictionary<string, object>>();

            var mediaClip = await MediaClip.CreateFromFileAsync(VideoFile);
            var mediaComposition = new MediaComposition();
            mediaComposition.Clips.Add(mediaClip);

            var ocrTime = endTime - startTime;
            var ocrSeconds = (int)ocrTime.TotalSeconds;

            var speed = 0m;
            var altitude = 0m;
            VideoLength = ocrSeconds;

            //var width = PreviewImage.Width;
            //var height = PreviewImage.Height;

            //var encodingProperties = mediaClip.GetVideoEncodingProperties();
            //var videoWidth = encodingProperties.Width;
            //var videoHeight = encodingProperties.Height;

            //var widthFactor = (width / videoWidth);
            //var heightFactor = height / videoHeight;


            for (var i = 0; i < ocrSeconds; i++)
            {
                var currentTime = startTime + TimeSpan.FromSeconds(i);
                var thumbnail = await mediaComposition.GetThumbnailAsync(currentTime, 0, 0, VideoFramePrecision.NearestFrame);
                var decoder = await BitmapDecoder.CreateAsync(thumbnail);
                var bitmap = await decoder.GetSoftwareBitmapAsync();

                await SetPreviewImage(bitmap);

                var result = await ocrEngine.RecognizeAsync(bitmap);

                var values = OcrAreas.ToDictionary(x => x.Name, x => new object());
                values["Time"] = currentTime;
                var succeeded = OcrAreas.ToDictionary(x => x.Name, x => false);

                foreach (var line in result.Lines)
                {
                    // Iterate over words in line.
                    foreach (var word in line.Words)
                    {
                        foreach (var ocrArea in OcrAreas)
                        {
                            var rect = ocrArea.Area;
                            if (
                                rect.Contains(new Point(word.BoundingRect.X + word.BoundingRect.Width / 2,
                                    word.BoundingRect.Y + word.BoundingRect.Height / 2)))
                            {
                                decimal tempVal;
                                if (decimal.TryParse(word.Text, out tempVal))
                                {
                                    values[ocrArea.Name] = Math.Abs(tempVal);
                                    // Need to display the read value somehow...
                                    succeeded[ocrArea.Name] = true;
                                }
                            }
                        }
                    }
                }

                if (succeeded.Any(x => !x.Value))
                {
                    Debug.WriteLine(result.Text);
                }

                var time = TimeSpan.FromSeconds(i);
                CurrentTime = time;

                ParsedValues = values.ToDictionary(x => x.Key, x => x.Value.ToString());

                //var point = new FlightPoint { Velocity = (decimal)values["Velocity"], Altitude = (decimal)values["Altitude"], Time = time };
                Debug.WriteLine($"{values["Time"]}, {values["Altitude"]}, {values["Velocity"]}");
                flight.Add(values);

                CurrentVideoPosition = i;
            }

            // Get information about the preview
            //var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

            using (IRandomAccessStream stream = await _csvFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                using (var dataWriter = new DataWriter(stream))
                {
                    //dataWriter.WriteString($"time,altitude,velocity,acceleration{Environment.NewLine}");
                    var commaSeperatedHeadings = string.Join(",", flight.First().Where(x => x.Key != "Time").Select(x => x.Key));
                    dataWriter.WriteString($"Time,{commaSeperatedHeadings}");
                    
                    //decimal prevVelocity = 0;
                    //var prevTime = TimeSpan.Zero;
                    foreach (var entry in flight)
                    {
                        var time = (TimeSpan) entry["Time"];
                        //var timeDelta = (decimal)(time - prevTime).TotalSeconds;
                        //var ac = 0m;
                        //if (timeDelta > 0)
                        //{
                        //    ac = (entry.Velocity - prevVelocity) / timeDelta;
                        //}

                        //prevVelocity = entry.Velocity;
                        //prevTime = time;

                        //dataWriter.WriteString($"{time}, {entry.Altitude}, {entry.Velocity}, {ac}{Environment.NewLine}");
                        //var sb = new StringBuilder();

                        var commaSeperatedValues = string.Join(",", entry.Where(x => x.Key != "Time").Select(x => x.Value.ToString()));

                        dataWriter.WriteString($"{time},");
                        dataWriter.WriteString(commaSeperatedValues);
                    }
                    await dataWriter.StoreAsync();
                    await stream.FlushAsync();
                }
            }

            ProgressBarVisibility = Visibility.Collapsed;
        }

        public ObservableCollection<Rect> RectsItems { get; }

        private void OutlineOcrSection(Rect speedRect, double widthFactor, double heightFactor)
        {
            RectsItems.Add(new Rect(speedRect.X * widthFactor, speedRect.Y * heightFactor, speedRect.Width * widthFactor, speedRect.Height * heightFactor));
        }


        private int areaCount;
        public void AddParameter(object sender, RoutedEventArgs e)
        {
            OcrAreas.Add(new OcrArea { Name = $"Property {areaCount}" });
            areaCount++;
        }

        private SoftwareBitmapSource _previewImageSource;
        public SoftwareBitmapSource PreviewImageSource
        {
            get { return _previewImageSource;}
            set { Set(ref _previewImageSource, value); }
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
            PreviewImageSource = source;
        }


        DelegateCommand _navigateCommand;
        public DelegateCommand NavigateCommand => _navigateCommand ?? (_navigateCommand = new DelegateCommand(ExecuteNavigate));
        private void ExecuteNavigate()
        {
            NavigationService.Navigate(typeof(Views.SecondView));
        }
    }
}
