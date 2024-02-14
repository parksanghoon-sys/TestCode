using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace wpfMVVM.Popup.Controls
{
    public class IconButton : Button
    {
        public ImageSource? IconImage
        {
            get { return (ImageSource)GetValue(IconImageProperty); }
            set { SetValue(IconImageProperty, value); }
        }
        public string? Text
        {
            get { return (string?)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(IconButton));


        public static readonly DependencyProperty IconImageProperty =
            DependencyProperty.Register("IconImage", typeof(ImageSource), typeof(IconButton), new PropertyMetadata(null));



        public IconButton()
        {
            DefaultStyleKey = typeof(IconButton);
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
