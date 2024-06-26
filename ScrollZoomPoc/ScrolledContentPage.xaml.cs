namespace ScrollZoomPoc;

public partial class ScrolledContentPage : ContentPage
{
    public List<string> ContentPages { get; } = new List<string>()
    {
        "Page 1", "Page 2", "Page 3"
    };
    
    public ScrolledContentPage()
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
        foreach (var view in _contentLayout.Children)
        {
            var grid = view as Grid;
            if (grid != null)
            {
                grid.HeightRequest = grid.Height * scale;
                grid.WidthRequest = grid.Width * scale;
            }
        }
    }
}