using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrollZoomPoc;

public partial class PagedPdfView2 : ContentView
{
    public event EventHandler<EventArgs> PageNext;
    public event EventHandler<EventArgs> PagePrevious;
    
    public double DefaultWidth { get; } = 400;
    public double DefaultHeight { get; } = 518;

    public PagedPdfView2()
    {
        InitializeComponent();
    }

    private double startScale = 0;
    private double currentScale = 0;
    void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        Console.WriteLine($"--- {e.Status} {e.Scale} Origin x,y - {e.ScaleOrigin.X},{e.ScaleOrigin.Y} ");
        
        //if (e.Status == GestureStatus.Running)
        //{
        //    ZoomContent(e.Scale);
        //}
        
        switch (e.Status)
        {
            case GestureStatus.Started:
                // Store the current scale factor applied to the wrapped user interface element,
                // and zero the components for the center point of the translate transform.
                startScale = _imageView.Scale;
                _imageView.AnchorX = e.ScaleOrigin.X;
                _imageView.AnchorY = e.ScaleOrigin.Y;
                break;
            case GestureStatus.Running:
                // Calculate the scale factor to be applied.
                currentScale += (e.Scale - 1) * startScale;
                currentScale = Math.Max(1, currentScale);
                _imageView.Scale = currentScale;
                break;
            case GestureStatus.Completed:
                // Store the final scale factor applied to the wrapped user interface element.
                startScale = currentScale;
                break;
        }
        
        //Console.WriteLine($"--- _image - ({_imageView.Width},{_imageView.Height}) _scrollView.ContentSize - ({_scrollView.ContentSize.Width},{_scrollView.ContentSize.Height})");
    }
    
    async  void OnDoubleTap(object sender, TappedEventArgs e)
    {
        if (_imageView.Scale == 1)
        {
            var position = e.GetPosition(_imageView);
            _imageView.AnchorX = position.Value.X;
            _imageView.AnchorY = position.Value.Y;
            _imageView.ScaleTo(5, 500);
        }
        else
        {
            _imageView.ScaleTo(1, 500);
        }
    }

    private void ZoomContent(double scale)
    {
        _imageView.HeightRequest = _imageView.Height * scale;
        _imageView.WidthRequest = _imageView.Width * scale;
        //_scrollView.ScrollToAsync(_imageView, ScrollToPosition.Center, false);
    }
    
    private async Task ZoomAndScrollContent(double scale, Point position)
    {
        var scrollXStart = position.X;
        var scrollYStart = position.Y;
        var scrollXEnd = position.X * scale;
        var scrollYEnd = position.Y * scale;
        var ratio = position.Y / position.X;

        var parentAnimation = new Animation();
        var heightAnimation =
            new Animation(x => _imageView.HeightRequest = x, _imageView.Height, _imageView.Height * scale);
        var widthAnimation =
            new Animation(x => _imageView.WidthRequest = x, _imageView.Width, _imageView.Width * scale);
        //var scrollAnimation =
        //    new Animation(x => _scrollView.ScrollToAsync(x, x * ratio, false), scrollXStart, scrollXEnd);

        parentAnimation.Add(0, 1, heightAnimation);
        parentAnimation.Add(0, 1, widthAnimation);
        //parentAnimation.Add(0, 1, scrollAnimation);
        parentAnimation.Commit(this, "ZoomAndScroll", length: 1000);
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
    
    private void ScrollView_OnScrolled(object? sender, ScrolledEventArgs e)
    {
        if (sender is ScrollView scrollView)
        {
            Console.WriteLine($"Scrolled ScrollView.Width {scrollView.Width}; ContentSize.Width {scrollView.ContentSize.Width}; e.ScrollX {e.ScrollX}; e.ScrollY {e.ScrollY}");
            if ((scrollView.ContentSize.Width > scrollView.Width) && (scrollView.Width + e.ScrollX) > scrollView.ContentSize.Width)
            {
                Console.WriteLine("Scrolled Page Right...");
                PageNext?.Invoke(this, new EventArgs());
            }
            else if (e.ScrollX < 0)
            {
                Console.WriteLine("Scrolled Page Left...");
                PagePrevious?.Invoke(this, new EventArgs());
            }
        }
    }

    private double startingTranslateX = 0;
    private double startingTranslateY = 0;
    
    private void PanGestureRecognizer_OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        Console.WriteLine($"---- PanGestureRecognizer_OnPanUpdated()");
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                startingTranslateX = _imageView.TranslationX;
                startingTranslateY = _imageView.TranslationY;
                break;
                
            case GestureStatus.Running:
                _imageView.TranslationX = startingTranslateX + e.TotalX;
                _imageView.TranslationY = startingTranslateY + e.TotalY;
                break;
                
            case GestureStatus.Completed:
                break;
        }
    }
}