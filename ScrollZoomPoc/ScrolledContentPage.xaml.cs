using System.ComponentModel;

namespace ScrollZoomPoc;

public partial class ScrolledContentPage : ContentPage
{
    public double DefaultWidth { get; } = 400;
    public double DefaultHeight { get; } = 518;

    public int MaxScale = 5;

    public List<string> ContentPages { get; } = new List<string>()
    {
        "pdf5535_0_612x792.png", "pdf5535_1_612x792.png", "pdf5535_2_612x792.png"
    };

    public ScrolledContentPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private Point _scaleOrigin;

    async void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        Console.WriteLine($"-- {e.Status} {e.Scale}");

        var visualElement = sender as VisualElement;

        switch (e.Status)
        {
            case GestureStatus.Started:
                Console.WriteLine($"--- e.ScaleOrigin {e.ScaleOrigin.X},{e.ScaleOrigin.Y}");
                _scaleOrigin = e.ScaleOrigin;
                await ExpandBackground(visualElement);
                break;

            case GestureStatus.Running:
                Console.WriteLine(
                    $"--- before _scrollView.ScrollX {_scrollView.ScrollX}, _scrollView.ScrollY {_scrollView.ScrollY}, _contentLayout.X {_contentLayout.X}, _contentLayout.Y {_contentLayout.Y}, visualElement.X {visualElement.X}, visualElement.Y {visualElement.Y}, visualElement.Width {visualElement.Width}, visualElement.Height {visualElement.Height}");
                ZoomContent(e.Scale);
                await Task.Delay(10);
                Console.WriteLine(
                    $"--- after _scrollView.ScrollX {_scrollView.ScrollX}, _scrollView.ScrollY {_scrollView.ScrollY}, _contentLayout.X {_contentLayout.X}, _contentLayout.Y {_contentLayout.Y}, visualElement.X {visualElement.X}, visualElement.Y {visualElement.Y}, visualElement.Width {visualElement.Width}, visualElement.Height {visualElement.Height}");
                await PositionAfterZoom(visualElement);
                break;

            case GestureStatus.Completed:
                SnapBack();
                await ResetBackground();
                break;
        }
    }

    private async Task PositionAfterZoom(VisualElement visualElement)
    {
        var scrollX = (_contentLayout.X + visualElement.X + (visualElement.Width * _scaleOrigin.X) -
                       (_scrollView.Width / 2));
        var scrollY = (_contentLayout.Y + visualElement.Y + (visualElement.Height * _scaleOrigin.Y) -
                       (_scrollView.Height / 2));
        Console.WriteLine($"--- scroll to {scrollX},{scrollY}");
        await _scrollView.ScrollToAsync(scrollX, scrollY, false);
    }

    private void SnapBack()
    {
        var parentAnimation = new Animation();
        foreach (var view in _contentLayout.Children)
        {
            if (view is VisualElement visualElement)
            {
                if (visualElement.Height < DefaultHeight || visualElement.Width < DefaultWidth ||
                    visualElement.Height > (DefaultHeight * MaxScale) ||
                    visualElement.Width > (DefaultWidth * MaxScale))
                {
                    Console.WriteLine($"---Resizing image to min/max...");
                    double targetWidth = 0;
                    double targetHeight = 0;

                    if (visualElement.Height < DefaultHeight || visualElement.Width < DefaultWidth)
                    {
                        targetWidth = DefaultWidth;
                        targetHeight = DefaultHeight;
                    }
                    else
                    {
                        targetWidth = DefaultWidth * MaxScale;
                        targetHeight = DefaultHeight * MaxScale;
                    }
                    
                    var heightAnimation =
                        new Animation(x => visualElement.HeightRequest = x, visualElement.Height, targetHeight);
                    var widthAnimation =
                        new Animation(x => visualElement.WidthRequest = x, visualElement.Width, targetWidth);

                    parentAnimation.Add(0, 1, heightAnimation);
                    parentAnimation.Add(0, 1, widthAnimation);
                }
            }
        }
        parentAnimation.Commit(this, "SnapBack", length: 100);
    }

    private async Task ExpandBackground(VisualElement visualElement)
    {
        Console.WriteLine($"--- before visualElement.Y {visualElement.Y}, _scrollView.ContentSize.Height {_scrollView.ContentSize.Height}, _scrollView.ScrollY {_scrollView.ScrollY}");
        var saveVisualElementY = visualElement.Y;
        var saveContentHeight = _scrollView.ContentSize.Height;
        var newContentHeight = (_contentLayout.Children.Count + 2) * DefaultHeight * MaxScale;
        var saveContentWidth = _scrollView.ContentSize.Width;
        var newContentWidth = 2 * DefaultWidth * MaxScale;
        var newScrollY = (newContentHeight - saveContentHeight) / 2 + _scrollView.ScrollY;
        var newScrollX = (newContentWidth - saveContentWidth) / 2 + _scrollView.ScrollX;
        _backgroundView.HeightRequest = newContentHeight;
        _backgroundView.WidthRequest = newContentWidth;
        await Task.Delay(10);
        Console.WriteLine($"--- after visualElement.Y {visualElement.Y}, _scrollView.ContentSize.Height {_scrollView.ContentSize.Height}, _scrollView.ScrollY {_scrollView.ScrollY}");
        await _scrollView.ScrollToAsync(newScrollX, newScrollY, false);
    }
    
    private async Task ResetBackground()
    {
        var saveContentHeight1 = _scrollView.ContentSize.Height;
        var saveContentWidth1 = _scrollView.ContentSize.Width;
        var newContentHeight1 = _contentLayout.Height;
        var newContentWidth1 = _contentLayout.Width;
        var newScrollY1 = _scrollView.ScrollY - ((saveContentHeight1-newContentHeight1)/2);
        var newScrollX1 = _scrollView.ScrollX - ((saveContentWidth1-newContentWidth1)/2);
        _backgroundView.HeightRequest = -1;
        _backgroundView.WidthRequest = -1;
        await _scrollView.ScrollToAsync(newScrollX1, newScrollY1, false);
    }
    
    async void OnDoubleTap(object sender, TappedEventArgs e)
    {
        if (sender is VisualElement visualElement)
        {
            var position = e.GetPosition(visualElement);
            if (visualElement.Width == DefaultWidth)// && visualElement.HeightRequest == DefaultHeight)
            {
                await ZoomAndScrollContent(5, visualElement, position.Value);
            }
            else
            {
                await ZoomAndScrollContent(1, visualElement, position.Value);
            }
        }
    }

    private void ZoomContent(double scale)
    {
        foreach (var view in _contentLayout.Children)
        {
            if (view is VisualElement visualElement)
            {
                visualElement.HeightRequest = visualElement.Height * scale;
                visualElement.WidthRequest = visualElement.Width * scale;
            }
        }
    }

    private bool _isAutoZooming = false;
    private double _x1 = 0;
    private double _x2 = 0;
    private double _y1 = 0;
    private double _y2 = 0;
    private double _padding = 40;
    private Point _tappedPosition;
    private VisualElement _tappedElement;
    private double _zoomScale;
    
    private async Task ZoomAndScrollContent(double scale, VisualElement tappedElement, Point position)
    {
        _x1 = _scrollView.ScrollX;
        _y1 = _scrollView.ScrollY;
        _x2 = (position.X-_padding) * MaxScale;
        if (_x2 > (DefaultWidth * MaxScale) / 2)
            _x2 = (DefaultWidth * MaxScale) / 2;
        
        _y2 = (position.Y-_padding) * MaxScale;
        Console.WriteLine($"--- _x1 {_x1}, _y1 {_y1}, _x2 {_x2}, _y2 {_y2}");

        _tappedElement = tappedElement;
        _tappedPosition = position;
        
        _isAutoZooming = true;
        _zoomScale = scale;
     
        var parentAnimation = new Animation();
       
        foreach (var view in _contentLayout.Children)
        {
            if (view is VisualElement visualElement)
            {
                var heightAnimation =
                    new Animation(x => visualElement.HeightRequest = x, visualElement.Height, DefaultHeight * scale);
                var widthAnimation =
                    new Animation(x => visualElement.WidthRequest = x, visualElement.Width, DefaultWidth * scale);
        
                parentAnimation.Add(0, 1, heightAnimation);
                parentAnimation.Add(0, 1, widthAnimation);
            }
        }
        
        parentAnimation.Commit(this, "AutoZoomIn", length: 400, finished: (d, b) =>
        {
            _isAutoZooming = false;
        });
    }

    private async void ScrollView_OnScrolled(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "ContentSize":
                Console.WriteLine($"--- ScrollView.ContentSize changed...");
                if (_isAutoZooming)
                {
                    if (_zoomScale > 1)
                    {
                        var percent = _tappedElement.Height / (MaxScale * DefaultHeight);
                        double x2 = _contentLayout.X + _tappedElement.X + ((_tappedPosition.X * MaxScale * percent) - _padding);
                        double y2 = _contentLayout.Y + _tappedElement.Y + ((_tappedPosition.Y * MaxScale * percent) - _padding);
                        Console.WriteLine($"--- percent {percent}, _contentLayout.X {_contentLayout.X}, _contentLayout.Y {_contentLayout.Y}, _tappedElement.X {_tappedElement.X}, _tappedElement.Y {_tappedElement.Y}");
                        Console.WriteLine($"--- scroll to {x2},{y2}");
                        await _scrollView.ScrollToAsync(x2, y2, false);
                    }
                    else
                    {
                        await _scrollView.ScrollToAsync(_tappedElement, ScrollToPosition.Center, false);
                    }
                }                    
                break;
        }
    }
}