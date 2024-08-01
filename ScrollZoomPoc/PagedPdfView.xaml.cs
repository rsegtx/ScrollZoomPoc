using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrollZoomPoc;

public partial class PagedPdfView : ContentView
{
    public event EventHandler<EventArgs> PageNext;
    public event EventHandler<EventArgs> PagePrevious;
    
    public double DefaultWidth { get; } = 400;
    public double DefaultHeight { get; } = 518;
    
    public int MaxScale = 5;

    public PagedPdfView()
    {
        InitializeComponent();
    }

    private Point _scaleOrigin;
    private bool _pinchZooming = false;
    
    async void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        Console.WriteLine($"--- {e.Status} {e.Scale} Origin x,y - {e.ScaleOrigin.X},{e.ScaleOrigin.Y} ");

        switch (e.Status)
        {
            case GestureStatus.Started:
                _scaleOrigin = e.ScaleOrigin;
                _pinchZooming = true;
                break;
            
            case GestureStatus.Running:
                Console.WriteLine($"before _imageView.Width {_imageView.Width}, _imageView.Height {_imageView.Height}");
                _imageView.HeightRequest = _imageView.Height * e.Scale;
                _imageView.WidthRequest = _imageView.Width * e.Scale;
                Console.WriteLine($"after _imageView.Width {_imageView.Width}, _imageView.Height {_imageView.Height}");
                Console.WriteLine($"--- _scrollView.Width {_scrollView.Width}; ContentSize.Width {_scrollView.ContentSize.Width}");
                if (_imageView.Width > _scrollView.Width || _imageView.Height > _scrollView.Height)
                {
                    var scrollX = (_imageView.Width > _scrollView.Width)
                            ? ((_imageView.Width * _scaleOrigin.X)) - (_scrollView.Width / 2)
                            : 0;
                    
                    var scrollY = (_imageView.Height > _scrollView.Height)
                            ? ((_imageView.Height * _scaleOrigin.Y)) - (_scrollView.Height/2)
                            : 0;
                    
                    Console.WriteLine($"--- scroll to {scrollX},{scrollY}");
                    await _scrollView.ScrollToAsync(scrollX, scrollY, false);
                }
                break;
            
            case GestureStatus.Completed:
                // when zoom in/out is complete adjust the size of _imageView if the size is smaller than
                // the minimum or larger than the maximum.
                if (_imageView.Height < DefaultHeight || _imageView.Width < DefaultWidth ||
                    _imageView.Height > (DefaultHeight*MaxScale) || _imageView.Width > (DefaultWidth*MaxScale))
                {
                    Console.WriteLine($"---Resizing image to min/max...");
                    double targetWidth = 0;
                    double targetHeight = 0;

                    if (_imageView.Height < DefaultHeight || _imageView.Width < DefaultWidth)
                    {
                        targetWidth = DefaultWidth;
                        targetHeight = DefaultHeight;
                    }
                    else
                    {
                        targetWidth = DefaultWidth*MaxScale;
                        targetHeight = DefaultHeight*MaxScale;
                    }
                    
                    var parentAnimation = new Animation();
                    var heightAnimation =
                        new Animation(x => _imageView.HeightRequest = x, _imageView.Height, targetHeight);
                    var widthAnimation =
                        new Animation(x => _imageView.WidthRequest = x, _imageView.Width, targetWidth);

                    parentAnimation.Add(0, 1, heightAnimation);
                    parentAnimation.Add(0, 1, widthAnimation);
                    parentAnimation.Commit(this, "ZoomAndScroll", length: 100);
                }
                _pinchZooming = false;

                break;
        }
        
        Console.WriteLine($"--- _image - ({_imageView.Width},{_imageView.Height}) _scrollView.ContentSize - ({_scrollView.ContentSize.Width},{_scrollView.ContentSize.Height})");
    }
    
    async void OnDoubleTap(object sender, TappedEventArgs e)
    {
        if (_imageView.Width == DefaultWidth && _imageView.HeightRequest == DefaultHeight)
        {
            var position = e.GetPosition(_imageView);
            await ZoomAndScrollContent(5, position.Value);
        }
        else
        {
            ResetZoom();
        }
    }

    private void ZoomContent(double scale)
    {
        _imageView.HeightRequest = _imageView.Height * scale;
        _imageView.WidthRequest = _imageView.Width * scale;
    }

    private bool _isAutoZooming = false;
    private double _x1 = 0;
    private double _x2 = 0;
    private double _y1 = 0;
    private double _y2 = 0;
    private double _padding = 40;
    
    private async Task ZoomAndScrollContent(double scale, Point position)
    {
        _backgroundView.HeightRequest = DefaultHeight * MaxScale;
        _backgroundView.WidthRequest = DefaultWidth * MaxScale;
        await Task.Delay(10);
        await _scrollView.ScrollToAsync(_imageView, ScrollToPosition.Center, false);

        _x1 = _scrollView.ScrollX;
        _y1 = _scrollView.ScrollY;
        _x2 = (position.X-_padding) * MaxScale;
        if (_x2 > (DefaultWidth * MaxScale) / 2)
            _x2 = (DefaultWidth * MaxScale) / 2;
        
        _y2 = (position.Y-_padding) * MaxScale;
        Console.WriteLine($"--- _x1 {_x1}, _y1 {_y1}, _x2 {_x2}, _y2 {_y2}");
        
        _isAutoZooming = true;
        
        var parentAnimation = new Animation();
        var heightAnimation =
            new Animation(x => _imageView.HeightRequest = x, _imageView.Height, _imageView.Height * scale);
        var widthAnimation =
            new Animation(x => _imageView.WidthRequest = x, _imageView.Width, _imageView.Width * scale);
        
        parentAnimation.Add(0, 1, heightAnimation);
        parentAnimation.Add(0, 1, widthAnimation);
        parentAnimation.Commit(this, "AutoZoomIn", length: 400, finished: (d, b) =>
        {
            _isAutoZooming = false;
            Console.WriteLine($"--- _scrollView.ScrollX {_scrollView.ScrollX}. _scrollView.ScrollY {_scrollView.ScrollY}");
        });
    }

    private void ResetZoom()
    {
        _backgroundView.HeightRequest = DefaultHeight;
        _backgroundView.WidthRequest = DefaultWidth;
        
        var parentAnimation = new Animation();
        var heightAnimation = new Animation(x => _imageView.HeightRequest = x, _imageView.Height, DefaultHeight);
        var widthAnimation = new Animation(x => _imageView.WidthRequest = x, _imageView.Width, DefaultWidth);

        parentAnimation.Add(0,1, heightAnimation);
        parentAnimation.Add(0, 1, widthAnimation);
        parentAnimation.Commit(this, "ResetZoom", length:400);
    }    
    
    DateTime _lastScrollToEndRight = DateTime.MinValue;
    DateTime _lastPageRight = DateTime.MinValue;
    DateTime _lastScrollToEndLeft = DateTime.MinValue;
    DateTime _lastPageLeft = DateTime.MinValue;
    
    private void ScrollView_OnScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isAutoZooming || _pinchZooming)
            return;

        Console.WriteLine($"Scrolled ScrollView.Width {_scrollView.Width}; ContentSize.Width {_scrollView.ContentSize.Width}; e.ScrollX {e.ScrollX}; e.ScrollY {e.ScrollY}");
        if (CanScrollVertically())
        {
            if ((_scrollView.Width + e.ScrollX) >= (_scrollView.ContentSize.Width - 3))
            {
                if (_lastScrollToEndRight != DateTime.MinValue)
                {
                    if (_lastScrollToEndRight < DateTime.Now.AddSeconds(-.25) &&
                        _lastPageRight < DateTime.Now.AddSeconds(-1))
                    {
                        Console.WriteLine("--- Page Right...");
                        _lastScrollToEndRight = DateTime.MinValue;
                        _lastPageRight = DateTime.Now;
                        PageNext?.Invoke(this, new EventArgs());
                    }
                    else
                    {
                        Console.WriteLine("--- Scrolled to end right...");
                        _lastScrollToEndRight = DateTime.Now;
                    }
                }
                else
                    _lastScrollToEndRight = DateTime.Now;
            }
            else if (e.ScrollX <= 3)
            {
                if (_lastScrollToEndLeft != DateTime.MinValue)
                {
                    if (_lastScrollToEndLeft < DateTime.Now.AddSeconds(-.25) &&
                        _lastPageLeft < DateTime.Now.AddSeconds(-1))
                    {
                        Console.WriteLine("--- Page Left...");
                        _lastScrollToEndLeft = DateTime.MinValue;
                        _lastPageLeft = DateTime.Now;
                        PagePrevious?.Invoke(this, new EventArgs());
                    }
                    else
                    {
                        Console.WriteLine("--- Scrolled to end left...");
                        _lastScrollToEndLeft = DateTime.Now;
                    }
                }
                else
                    _lastScrollToEndLeft = DateTime.Now;
            }
            else
            {
                _lastScrollToEndRight = DateTime.MinValue;
                _lastScrollToEndLeft = DateTime.MinValue;
            }
        }
    }

    private bool CanScrollVertically()
    {
        return _scrollView.ContentSize.Width > _scrollView.Width;
    }
    
    private async void _imageView_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "Width":
                Console.WriteLine($"--- _imageView.Width changed...{_imageView.Width}");
                if (_isAutoZooming) // && (_imageView.Width > _scrollView.Width || _imageView.Height > _scrollView.Height))
                {
                    var percent = _imageView.Height / (MaxScale * DefaultHeight);
                    var x = _x1 + ((_x2 - _x1) * percent);
                    var y = _y1 + ((_y2 - _y1) * percent);
                    Console.WriteLine($"--- scroll to {x},{y}");
                    await _scrollView.ScrollToAsync(x, y, false);
                }
                break;
        }
    }    
    
    private void Right_OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Completed)
            PageNext?.Invoke(this, new EventArgs());
    }
    
    private void Left_OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (e.StatusType == GestureStatus.Completed)
            PagePrevious?.Invoke(this, new EventArgs());
    }
}