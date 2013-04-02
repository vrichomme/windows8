﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media.Animation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace LibrelioApplication
{
    public class ImageData : Common.BindableBase
    {
        //public string Url { get; set; }
        private ImageSource _image = null;
        private double _width = 0;
        private double _height = 0;
        private bool _notDownloaded = false;
        private bool _hidden = false;
        private Stretch _stretch = Stretch.UniformToFill;

        public ImageSource Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged("Image");
            }
        }

        public Stretch ImgStretch
        {
            get { return _stretch; }
            set
            {
                _stretch = value;
                OnPropertyChanged("ImgStretch");
            }
        }

        public bool NotDownloaded
        {
            get { return _notDownloaded; }
            set
            {
                _notDownloaded = value;
                OnPropertyChanged("NotDownloaded");
            }
        }

        public bool Hidden
        {
            get { return _hidden; }
            set
            {
                _hidden = value;
                OnPropertyChanged("Hidden");
            }
        }

        public double Width
        {
            get { return _width; }
            set
            {
                _width = value;
                OnPropertyChanged("Width");
            }
        }

        public double Height
        {
            get { return _height; }
            set
            {
                _height = value;
                OnPropertyChanged("Height");
            }
        }
    }

    public sealed partial class SlideShow : UserControl
    {
        private Point initialPoint;
        bool isSwiping = false;

        int length = 0;
        int currentImage = 0;

        bool autoSlide = false;
        int interval = 0;

        bool noTranstions = false;

        ObservableCollection<ImageData> images = new ObservableCollection<ImageData>();

        ScrollViewer scrollViewer;

        public SlideShow()
        {
            this.InitializeComponent();
        }

        public async Task SetRect(Rect rect, string folderUrl, string url, float offset)
        {
            if (!DownloadManager.IsFullScreenAsset(url))
            {
                this.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
                this.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Top;
                this.Margin = new Thickness(rect.Left * offset, (rect.Top + 1.5) * offset, 0, 0);
                this.Width = rect.Width * offset;
                this.Height = rect.Height * offset;
            }
            else
            {
                this.Width = rect.Width;
                this.Height = rect.Height;
                this.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                this.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center;
            }

            string startMame = null;
            string endName = null;

            if (!DownloadManager.IsFullScreenAsset(url))
            {
                images.Add(new ImageData() { Image = null, Width = this.Width, Height = this.Height });
            }
            StorageFolder folder = null;
            try
            {
                folder = KnownFolders.DocumentsLibrary;
                foreach (var fld in folderUrl.Split('\\'))
                {
                    if (fld != "")
                    {
                        folder = await folder.GetFolderAsync(fld);
                    }
                }

                int start = 0;
                int end = 0;
                if (url.Contains(".jpg"))
                {
                    start = url.IndexOf('_');
                    end = url.IndexOf(".jpg");

                    startMame = url.Substring(0, start + 1);
                    startMame = startMame.Replace("http://localhost/", "");
                    endName = ".jpg";
                }
                else if (url.Contains(".png"))
                {
                    start = url.IndexOf('_');
                    end = url.IndexOf(".png");

                    startMame = url.Substring(0, start + 1);
                    endName = ".png";
                }

                if (start == -1 || end == -1)
                {
                    throw new Exception();
                }

                var test = url.Substring(start + 1, end - start);
                length = Convert.ToInt32(url.Substring(start + 1, end - start - 1));

            }
            catch
            {
                if (!DownloadManager.IsFullScreenAsset(url))
                {
                    images.Add(new ImageData() { Image = null, Width = this.Width, Height = this.Height });
                }

                for (int p = 0; p < 1; p++)
                {
                    images.Add(new ImageData() { Image = null, NotDownloaded = true, Width = this.Width, Height = this.Height });
                }
                return;
            }

            var maxWidth = this.Width;
            var maxHeight = this.Height;
            for (int i = 1; i <= length; i++)
            {
                StorageFile file = null;
                try
                {
                    var str = folder.Path + "\\" + startMame + i + endName;
                    file = await StorageFile.GetFileFromPathAsync(folder.Path + "\\" + startMame + i + endName);
                    using (var stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        var unprotected = await DownloadManager.UnprotectPDFStream(stream);
                        var bitmap = new BitmapImage();
                        await bitmap.SetSourceAsync(unprotected);
                        if (DownloadManager.IsFullScreenAsset(url))
                        {
                            maxWidth = maxWidth < bitmap.PixelWidth ? bitmap.PixelWidth : maxWidth;
                            maxHeight = maxHeight < bitmap.PixelHeight ? bitmap.PixelHeight : maxHeight;
                            images.Add(new ImageData() { Image = bitmap, ImgStretch = Stretch.Uniform, Hidden = true, Width = this.Width, Height = this.Height });
                        }
                        else
                        {
                            images.Add(new ImageData() { Image = bitmap, Hidden = true, Width = this.Width, Height = this.Height });
                        }
                    }
                }
                catch
                {
                    images.Add(new ImageData() { Image = null, NotDownloaded = true, Width = this.Width, Height = this.Height });
                }
            }

            if (!DownloadManager.IsFullScreenAsset(url))
            {
                images.Add(new ImageData() { Image = null, Width = this.Width, Height = this.Height });
            }
            else
            {
                if (maxHeight > Window.Current.Bounds.Height - 100)
                {
                    maxWidth = maxWidth * (Window.Current.Bounds.Height - 100) / maxHeight;
                    maxHeight = Window.Current.Bounds.Height - 100;
                }
                this.Width = maxWidth;
                this.Height = maxHeight;
                foreach (var image in images)
                {
                    image.Width = maxWidth;
                    image.Height = maxHeight;
                }
            }
            itemListView.ItemsSource = images;

            //autoSlide = true;
            //interval = 4000;

            //noTranstions = true;
        }

        public async void Start(int interval)
        {
            while (true)
            {
                await Task.Delay(interval);

                var width = scrollViewer.ExtentWidth / (length + 2);
                var offset = (int)(scrollViewer.HorizontalOffset / width);
                var start = scrollViewer.HorizontalOffset;
                if (offset == length + 1)
                {
                    scrollViewer.ScrollToHorizontalOffset(0);
                    start = 0;
                    offset = 0;
                }

                if (!noTranstions)
                {
                    var ee = new ExponentialEase();
                    ee.EasingMode = EasingMode.EaseInOut;
                    var sb = new Storyboard();
                    var da = new DoubleAnimation
                    {
                        From = start,
                        To = (width * offset) + width,
                        Duration = new Duration(TimeSpan.FromSeconds(0.5d)),
                        EasingFunction = ee,
                        EnableDependentAnimation = true
                    };

                    sb.Children.Add(da);
                    Storyboard.SetTargetProperty(da, "HorizontalOffset");
                    Storyboard.SetTarget(sb, Mediator);
                    sb.Begin();
                }
                else
                {
                    scrollViewer.ScrollToHorizontalOffset((width * offset) + width);
                }
            }
        }

        private static T findFirstInVisualTree<T>(DependencyObject parent) where T : class
        {
            if (parent == null)
            {
                return null;
            }

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                var childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = findFirstInVisualTree<T>(child);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null)
                    {
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = child as T;
                    break;
                }
            }

            return foundChild;
        }

        private void itemListView_Loaded(object sender, RoutedEventArgs e)
        {
            scrollViewer = findFirstInVisualTree<ScrollViewer>(itemListView);
            if (scrollViewer != null)
            {
                scrollViewer.HorizontalSnapPointsType = SnapPointsType.MandatorySingle;
                scrollViewer.HorizontalSnapPointsAlignment = SnapPointsAlignment.Near;

                if (autoSlide)
                {
                    Binding b = new Binding();
                    b.Source = scrollViewer;
                    b.Mode = BindingMode.TwoWay;
                    Mediator.SetBinding(Common.ScrollViewerOffsetMediator.ScrollViewerProperty, b);
                    Start(interval);
                }

                if (noTranstions)
                {
                    scrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
                    scrollViewer.ManipulationMode = ManipulationModes.All;
                    scrollViewer.ManipulationStarted += scrollViewer_ManipulationStarted;
                    scrollViewer.ManipulationDelta += scrollViewer_ManipulationDelta;
                }
            }
        }

        void scrollViewer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.IsInertial && isSwiping)
            {
                Point currentpoint = e.Position;
                if (currentpoint.X - initialPoint.X >= 85)
                {
                    isSwiping = false;
                    SwipeRight();
                    e.Complete();
                }
                else if (initialPoint.X - currentpoint.X >= 85)
                {
                    isSwiping = false;
                    SwipeLeft();
                    e.Complete();
                }
            }
        }

        void scrollViewer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            initialPoint = e.Position;
            isSwiping = true;
        }

        void SwipeLeft()
        {
            var width = scrollViewer.ExtentWidth / (length + 2);
            if (scrollViewer.HorizontalOffset < (scrollViewer.ExtentWidth - width))
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + width);
            }
        }

        void SwipeRight()
        {
            var width = scrollViewer.ExtentWidth / (length + 2);
            if (scrollViewer.HorizontalOffset >= width)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - width);
            }
        }
    }
}
