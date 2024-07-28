using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrollZoomPoc;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void OnPagedContentClicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new PagedContentPage());
    }
    
    private void OnPagedContent2Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new PagedContentPage2());
    }
    
    private void OnScrolledContentClicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new ScrolledContentPage());
    }
}