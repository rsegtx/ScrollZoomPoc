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

    async void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        Console.WriteLine($"--- {e.Status} {e.Scale} Origin x,y - {e.ScaleOrigin.X},{e.ScaleOrigin.Y} ");

        switch (e.Status)
        {
            case GestureStatus.Started:
                break;
            
            case GestureStatus.Running:
                ZoomContent(e.Scale);
                if (_imageView.Width > _scrollView.Width || _imageView.Height > _scrollView.Height)
                    // TODO: rather than scrolling to center of _imageView, we should take into account
                    // e.ScaleOrigin.X, e.ScaleOrigin.Y.
                    await _scrollView.ScrollToAsync(_imageView, ScrollToPosition.Center, false);
                break;
            
            case GestureStatus.Completed:
                
                // when zoom in/out is complete adjust the size of _imageView if the size is smaller than
                // the minimum or larger than the maximum.
                if (_imageView.Height < DefaultHeight || _imageView.Width < DefaultWidth ||
                    _imageView.Height > (DefaultHeight*MaxScale) || _imageView.Width > (DefaultWidth*MaxScale))
                {
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

                break;
        }
        
        Console.WriteLine($"--- _image - ({_imageView.Width},{_imageView.Height}) _scrollView.ContentSize - ({_scrollView.ContentSize.Width},{_scrollView.ContentSize.Height})");
    }
    
    async  void OnDoubleTap(object sender, TappedEventArgs e)
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
    private double _scrollXFactor = 0;
    private double _scrollYFactor = 0;
    
    private async Task ZoomAndScrollContent(double scale, Point position)
    {
        _isAutoZooming = true;
        
        _scrollXFactor = position.X / _imageView.Width;
        _scrollYFactor = position.Y / _imageView.Height;

        var parentAnimation = new Animation();
        var heightAnimation =
            new Animation(x => _imageView.HeightRequest = x, _imageView.Height, _imageView.Height * scale);
        var widthAnimation =
            new Animation(x => _imageView.WidthRequest = x, _imageView.Width, _imageView.Width * scale);
        parentAnimation.Add(0, 1, heightAnimation);
        parentAnimation.Add(0, 1, widthAnimation);
        parentAnimation.Commit(this, "ZoomAndScroll", length: 250, finished: (d, b) => { _isAutoZooming = false;});
    }

    private void ResetZoom()
    {
        var parentAnimation = new Animation();
        var heightAnimation = new Animation(x => _imageView.HeightRequest = x, _imageView.Height, DefaultHeight);
        var widthAnimation = new Animation(x => _imageView.WidthRequest = x, _imageView.Width, DefaultWidth);

        parentAnimation.Add(0,1, heightAnimation);
        parentAnimation.Add(0, 1, widthAnimation);
        parentAnimation.Commit(this, "ResetZoom", length:250);
    }    
    
    DateTime _lastScrollToEndRight = DateTime.MinValue;
    DateTime _lastPageRight = DateTime.MinValue;
    DateTime _lastScrollToEndLeft = DateTime.MinValue;
    DateTime _lastPageLeft = DateTime.MinValue;
    
    private void ScrollView_OnScrolled(object? sender, ScrolledEventArgs e)
    {
        if (_isAutoZooming)
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
    
    private async void _scrollView_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "ContentSize":
                Console.WriteLine($"--- _scrollView.ContentSize changed...");
                if (_isAutoZooming && (_imageView.Width > _scrollView.Width || _imageView.Height > _scrollView.Height))
                {
                    var x = _imageView.Width * _scrollXFactor;
                    var y = _imageView.Height * _scrollYFactor;
                    // TODO: should try and scroll to place double tapped but the animation is pretty rough
                    // right now, need to work on a way to smooth it out.
                    //await _scrollView.ScrollToAsync(x, y, false);
                    await _scrollView.ScrollToAsync(_imageView, ScrollToPosition.Center, false);
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