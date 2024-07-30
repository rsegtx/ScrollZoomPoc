namespace ScrollZoomPoc;

public partial class ScrolledContentPage : ContentPage
{
    public List<string> ContentPages { get; } = new List<string>()
    {
        "pdf5535_0_612x792.png", "pdf5535_1_612x792.png", "pdf5535_2_612x792.png"
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

    async void OnDoubleTap(object sender, TappedEventArgs e)
    {
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
            if (view is VisualElement visualElement)
            {
                visualElement.HeightRequest = visualElement.Height * scale;
                visualElement.WidthRequest = visualElement.Width * scale;
            }
        }
    }
}