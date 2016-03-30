using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.Ocr;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI;
using Windows.ApplicationModel.DataTransfer;
using Dmx.Windows.MPC.InkToOcr;
using Windows.Graphics.Imaging;



// 1. use the control like this in XAML
// <dmx:OcrImageContainer Name="MyOIC" Width="300" Height="300" UseOnlineOcr="True"                                      
// SubscriptionKey="<yourkey>"></dmx:OcrImageContainer>
// 2. drag an image from your image library to the control
// 3. mark a text with the mouse
// 4. call ExtractFromLastStroke() and check the result


namespace Dmx.Win.MPC.InkToOcr
{
    public sealed partial class OcrImageContainer : UserControl
    {
        private bool useOnlineOcr = false;

        public event EventHandler TextFound;
        public InkCanvas GlobalInkCanvas { get; private set; }
        public OcrEngine OCREngine { get; set; }
        public CoreInputDeviceTypes InputTypesForMarking { get; set; }
        public bool ShowDebugRectangle { get; set; }
        public Image DebugCropImage { get; set; }
        public string MyImageSourcePath
        {
            get
            {
                return _myImageSourcePath;
            }
            set
            {
                _myImageSourcePath = value;

            }
        }

        /// <summary>
        /// Allow this control to go online and check for ocr results from Project Oxford
        /// </summary>
        public bool UseOnlineOcr
        {
            get
            {
                return useOnlineOcr;
            }

            set
            {
                useOnlineOcr = value;
            }
        }

        public string SubscriptionKey { get; set; }

        private string _myImageSourcePath;
        private RotateTransform _rotateTransform;
        private TranslateTransform _translateTransform;
        private ScaleTransform _scaleTransform;
        private StorageFile _croppedFile;
        private Canvas _debuggingCanvas;
        

        public OcrImageContainer()
        {
            this.InitializeComponent();
            this._rotateTransform = MyGridRT as RotateTransform;
            this._translateTransform = MyGridTT as TranslateTransform;
            this._scaleTransform = MyGridST as ScaleTransform;
            ManipulationGrid.ManipulationDelta += Grid_ManipulationDelta;
        }



        public async void Init(OcrEngine ocrEngine, StorageFile file, CoreInputDeviceTypes inputTypesForMarking = CoreInputDeviceTypes.Mouse, Image myDebugImage = null)
        {
            if (ocrEngine == null)
            {
                this.OCREngine = OcrEngine.TryCreateFromLanguage(new Language("en"));
            }
            else
            {
                this.OCREngine = ocrEngine;
            }

            if (inputTypesForMarking == CoreInputDeviceTypes.None)
            {
                InputTypesForMarking = CoreInputDeviceTypes.Mouse;
            }
            else
            {
                InputTypesForMarking = inputTypesForMarking;
            }
            MyImageSourcePath = file.Path;
            DebugCropImage = myDebugImage;

            BitmapImage bim = new BitmapImage(new Uri(MyImageSourcePath));
            bim.SetSource(await file.OpenReadAsync());
            MyImage.Source = bim;

            MyInkCanvas.InkPresenter.InputDeviceTypes = InputTypesForMarking;

        }

        public async Task<string> ExtractFromLastStroke()
        {
            var result = await ExtractTextFromMarking(MyInkCanvas, MyImageSourcePath);
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
            return result;
        }

        private void Grid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // makes sure it turns around the center
            _rotateTransform.CenterX = ManipulationGrid.ActualWidth / 2;
            _rotateTransform.CenterY = ManipulationGrid.ActualHeight / 2;
            _rotateTransform.Angle += e.Delta.Rotation;
            _translateTransform.X += e.Delta.Translation.X;
            _translateTransform.Y += e.Delta.Translation.Y;
            _scaleTransform.CenterX = ManipulationGrid.ActualWidth / 2;
            _scaleTransform.CenterY = ManipulationGrid.ActualHeight / 2;
            _scaleTransform.ScaleX *= e.Delta.Scale;
            _scaleTransform.ScaleY *= e.Delta.Scale;
        }

        private async Task<string> ExtractTextFromMarking(InkCanvas inkCanvas, String filepath)
        {
            var file = await StorageFile.GetFileFromPathAsync(filepath);
            var newImgFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(file.Name + Guid.NewGuid(), CreationCollisionOption.ReplaceExisting);
            var strokes = inkCanvas.InkPresenter.StrokeContainer.GetStrokes();
            var stroke = strokes[strokes.Count - 1];

            // find the outside bounderies of the last stroke
            double strokeSmallestX = Double.MaxValue;
            double strokeSmallestY = Double.MaxValue;
            double strokeLargestX = Double.MinValue;
            double strokeLargestY = Double.MinValue;
            ImageCropper.FindBounderiesOfStroke(stroke, ref strokeSmallestX, ref strokeSmallestY, ref strokeLargestX, ref strokeLargestY);

            // find crop size
            var cropWidth = (strokeLargestX - strokeSmallestX);
            var cropHeight = (strokeLargestY - strokeSmallestY);

            var cropPointX = strokeSmallestX;
            var cropPointY = strokeSmallestY;
            var cropSize = new Size(cropWidth, cropHeight);

            if (ShowDebugRectangle)
            {
                DebugHelper.ShowNewDebuggingCanvas(strokeSmallestX, strokeSmallestY, cropHeight, cropWidth, ManipulationGrid, ref _debuggingCanvas);
            }

            // normalize here regarding grid position
            if (inkCanvas.ActualHeight > MyImage.ActualHeight)
            {
                cropPointY = cropPointY - ((ManipulationGrid.ActualHeight / 2) - (MyImage.ActualHeight / 2));
            }

            if (inkCanvas.ActualWidth > MyImage.ActualWidth)
            {
                cropPointX = cropPointX - ((ManipulationGrid.ActualWidth / 2) - (MyImage.ActualWidth / 2));
            }

            var startPoint = new Point(cropPointX, cropPointY);

            Size wis = new Size(MyImage.ActualWidth, MyImage.ActualHeight);
            _croppedFile = await ImageCropper.SaveCroppedBitmapAsync(file, newImgFile, startPoint, cropSize, wis);

            if (DebugCropImage != null)
            {
                BitmapImage bim = new BitmapImage(new Uri(_croppedFile.Path));
                DebugCropImage.Source = bim;
            }


            if (UseOnlineOcr)
            {
                var res = await ProjOxWrapper.PostToOnlineOcrRecoAsync(_croppedFile, SubscriptionKey);
                if (res != string.Empty)
                {
                    if (TextFound != null)
                    {
                        TextFound.Invoke(this, new TextFoundEventArgs(res));
                    }
                }
                Debug.WriteLine("From Online OCR:" + res);
                return res;
            }
            else
            {
                // extract Text offline
                var result = await OfflineImageTextExtractor.ExtractTextFromImageAsync(_croppedFile, OCREngine);
                if (result != string.Empty)
                {
                    if (TextFound != null)
                    {
                        TextFound.Invoke(this, new TextFoundEventArgs(result));
                    }
                }
                Debug.WriteLine("Offline OCR: " + result);

                return result;
            }
        }


        private async void OnDropped(object sender, DragEventArgs e)
        {
            ResetAll();

            var file = (StorageFile)(await e.DataView.GetStorageItemsAsync())[0];

            // There might be a rotation hint in the exif of the image. That's why we have to rotate the inkcanvas accoringly
            var decoder = await DecodeImage(file);
            var orientation = await GetExifMetadataForOrientation(decoder);
            Debug.WriteLine(orientation);

            MyImageRenderTransform.CenterX = MyImage.ActualWidth / 2;
            MyImageRenderTransform.CenterY = MyImage.ActualHeight / 2;

            MyInkCanvasRenderTransform.CenterX = MyInkCanvas.ActualWidth / 2;
            MyInkCanvasRenderTransform.CenterY = MyInkCanvas.ActualHeight / 2;
            
            
            int inkAngle = 0;
            switch (orientation)
            {
                case 1: // Exif says normal
                    inkAngle = 0;
                    break;
                case 3: // Exif says 180
                    inkAngle = 180;
                    break;
                case 8: // Exif says 270
                    inkAngle = 270;
                    break;
                case 6: // exif says 90
                    inkAngle = 90;
                    break;
                default:
                    inkAngle = 0;
                    break;
            }

            MyInkCanvasRenderTransform.Angle = inkAngle;
            
            Init(this.OCREngine, file, this.InputTypesForMarking, DebugCropImage);            
        }
        
        async Task<BitmapDecoder> DecodeImage(StorageFile file)
        {
            var stream = await file.OpenAsync(FileAccessMode.Read);
            return await BitmapDecoder.CreateAsync(stream);
        }

        async Task<ushort?> GetExifMetadataForOrientation(BitmapDecoder decoder)
        {
            var requests = new System.Collections.Generic.List<string>();
            requests.Add("System.Photo.Orientation"); // Windows property key for EXIF orientation          

            try
            {
                var retrievedProps = await decoder.BitmapProperties.GetPropertiesAsync(requests);
                ushort? orientation = null;
                if (retrievedProps.ContainsKey("System.Photo.Orientation"))
                {
                    orientation = (ushort)retrievedProps["System.Photo.Orientation"].Value;
                }
                return orientation;
            }

            catch (Exception err)
            {
                //todo: Handle this
            }
            return null;
        }
        
        private void ResetAll()
        {
            MyImage.SetValue(Image.StretchProperty, Stretch.Uniform);
            MyGridRT.SetValue(RotateTransform.AngleProperty, 0);
            MyGridST.SetValue(ScaleTransform.ScaleXProperty, 1);
            MyGridST.SetValue(ScaleTransform.ScaleYProperty, 1);
            MyGridTT.SetValue(TranslateTransform.XProperty, 0);
            MyGridTT.SetValue(TranslateTransform.YProperty, 0);
            MyInkCanvas.InkPresenter.StrokeContainer.Clear();
            if (_debuggingCanvas != null)
            {
                _debuggingCanvas.Children.Clear();
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {            
            e.AcceptedOperation = DataPackageOperation.Copy;
        }


        private void OnDragLeave(object sender, DragEventArgs e)
        {
            // todo 
        }

    }
}
