namespace ScrollZoomPoc;

public partial class PagedContentPage2 : ContentPage
{
    public List<string> ContentPages { get; } = new List<string>()
    {
        "pdf5535_0_612x792.png", "pdf5535_1_612x792.png", "pdf5535_2_612x792.png"
    };

    public double DefaultWidth { get; } = 400;
    public double DefaultHeight { get; } = 518;
    
    public PagedContentPage2()
    {
        InitializeComponent();
        BindingContext = this;
    }

    void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        Console.WriteLine($"-- {e.Status} {e.Scale}");
        
        if (e.Status == GestureStatus.Running)
        {
            ZoomContent(e.Scale);
        }
    }
    
    async  void OnDoubleTap(object sender, TappedEventArgs e)
    {
        var view = _carouselView.CurrentView;
        if (view is ScrollView scrollView && scrollView.Children.First() is VisualElement visualElement)
        {
            if (visualElement.Width == DefaultWidth && visualElement.HeightRequest == DefaultHeight)
            {
                var position = e.GetPosition(visualElement);
                await ZoomAndScrollContent(5, position.Value);
                Console.WriteLine($"--- scrollView.ContentSize.Width={scrollView.ContentSize.Width}, Height={scrollView.ContentSize.Height}");
                Console.WriteLine($"--- visualElement.Width={visualElement.Width}, Height={visualElement.Height}");
                Console.WriteLine($"--- TappedEventArgs.Position.X={position.Value.X}, Y={position.Value.Y}");
            }
            else
            {
                ResetZoom();
                Console.WriteLine($"--- scrollView.ContentSize.Width={scrollView.ContentSize.Width}, Height={scrollView.ContentSize.Height}");
                Console.WriteLine($"--- visualElement.Width={visualElement.Width}, Height={visualElement.Height}");
            }
        }
    }
    
    private void OnZoomIn(object sender, EventArgs e)
    {
        ZoomContent(1.5);
    }
    
    private void OnZoomOut(object sender, EventArgs e)
    {
        ZoomContent(.5);
    }
    
    private void ZoomContent(double scale)
    {
        var view = _carouselView.CurrentView;
        if (view is ScrollView scrollView && scrollView.Children.First() is VisualElement visualElement)
        {
            visualElement.HeightRequest = visualElement.Height * scale;
            visualElement.WidthRequest = visualElement.Width * scale;
        }
    }

    private async Task ZoomAndScrollContent(double scale, Point position)
    {
        var view = _carouselView.CurrentView;
        if (view is ScrollView scrollView && scrollView.Children.First() is VisualElement visualElement)
        {
            var scrollXStart = position.X;
            var scrollYStart = position.Y;
            var scrollXEnd = position.X * scale;
            var scrollYEnd = position.Y * scale;
            var ratio = position.Y / position.X;
            
            var parentAnimation = new Animation();
            var heightAnimation = new Animation(x => visualElement.HeightRequest = x, visualElement.Height, visualElement.Height * scale);
            var widthAnimation = new Animation(x => visualElement.WidthRequest = x, visualElement.Width, visualElement.Width * scale);
            var scrollAnimation = new Animation(x => scrollView.ScrollToAsync(x,x*ratio,false), scrollXStart, scrollXEnd);

            parentAnimation.Add(0,1, heightAnimation);
            parentAnimation.Add(0, 1, widthAnimation);
            parentAnimation.Add(0, 1, scrollAnimation);
            parentAnimation.Commit(this, "ZoomAndScroll", length:1000);
            //await Task.Delay(1001);
            //scrollView.ScrollToAsync(scrollXEnd, scrollYEnd, true);

            //visualElement.Animate("Expand", animation: new Animation(x => visualElement.HeightRequest = x, visualElement.Height, visualElement.Height * scale), length: 1000);
            //visualElement.Animate("Expand2", animation: new Animation(x => visualElement.WidthRequest = x, visualElement.Width, visualElement.Width * scale), length: 1000);
            //visualElement.HeightRequest = visualElement.Height * scale;
            //visualElement.WidthRequest = visualElement.Width * scale;
        }
    }

    private void ResetZoom()
    {
        var view = _carouselView.CurrentView;
        if (view is ScrollView scrollView && scrollView.Children.First() is VisualElement visualElement)
        {
            visualElement.HeightRequest = DefaultHeight;
            visualElement.WidthRequest = DefaultWidth;
        }
    }
    
    private ScrollView _lastScrollView;
    private DateTime _lastPageEventTime;
    
    private void ScrollView_OnScrolled(object? sender, ScrolledEventArgs e)
    {
        return;
        
        if (sender is ScrollView scrollView)
        {
            // hack to keep from paging more than once...
            if (_lastScrollView == scrollView && _lastPageEventTime.AddSeconds(1) > DateTime.Now)
                return;
            
            Console.WriteLine($"Scrolled ScrollView.Width {scrollView.Width}; ContentSize.Width {scrollView.ContentSize.Width}; e.ScrollX {e.ScrollX}; e.ScrollY {e.ScrollY}");
            if ((scrollView.ContentSize.Width > scrollView.Width) && (scrollView.Width + e.ScrollX) > scrollView.ContentSize.Width)
            {
                if (_carouselView.SelectedIndex < (_carouselView.ItemsCount - 1))
                {
                    Console.WriteLine("Scrolled Page Right...");
                    _lastScrollView = scrollView;
                    _lastPageEventTime = DateTime.Now;
                    _carouselView.SelectedIndex++;
                }
            }
            else if (e.ScrollX < 0)
            {
                if (_carouselView.SelectedIndex > 0)
                {
                    Console.WriteLine("Scrolled Page Left...");
                    _lastScrollView = scrollView;
                    _lastPageEventTime = DateTime.Now;
                    _carouselView.SelectedIndex--;
                }
            }
        }
    }

    private void PagedPdfView_OnPageNext(object? sender, EventArgs e)
    {
        // hack to keep from paging more than once...
        if (_lastScrollView == sender && _lastPageEventTime.AddSeconds(1) > DateTime.Now)
            return;
        
        if (_carouselView.SelectedIndex < (_carouselView.ItemsCount - 1))
        {
            _lastScrollView = sender as ScrollView;
            _lastPageEventTime = DateTime.Now;
            _carouselView.SelectedIndex++;
        }
    }

    private void PagedPdfView_OnPagePrevious(object? sender, EventArgs e)
    {
        // hack to keep from paging more than once...
        if (_lastScrollView == sender && _lastPageEventTime.AddSeconds(1) > DateTime.Now)
            return;
        
        if (_carouselView.SelectedIndex > 0)
        {
            _lastScrollView = sender as ScrollView;
            _lastPageEventTime = DateTime.Now;
            _carouselView.SelectedIndex--;
        }
    }
}