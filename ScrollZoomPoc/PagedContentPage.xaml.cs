namespace ScrollZoomPoc;

public partial class PagedContentPage : ContentPage
{
    public List<string> ContentPages { get; } = new List<string>()
    {
        "Page 1", "Page 2", "Page 3"
    };
    
    public PagedContentPage()
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
        if (view is ScrollView scrollView && scrollView.Children.First() is Grid grid)
        {
            grid.HeightRequest = grid.Height * scale;
            grid.WidthRequest = grid.Width * scale;
        }
    }

    private ScrollView _lastScrollView;
    private DateTime _lastPageEventTime;
    
    private void ScrollView_OnScrolled(object? sender, ScrolledEventArgs e)
    {
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
}