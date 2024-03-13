using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace avaloniaNavigationBar.Styles
{
    public class MagicBar : ListBox
    {
        private Grid _circle;
        static MagicBar()
        {

        }
        public MagicBar()
        {
            this.SelectionChanged += (sender, args) =>
            {
                int index = SelectedIndex;
                Canvas.SetLeft(_circle, index * 80);
            };
        }
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _circle = e.NameScope.Get<Grid>("PART_Circle");
            SelectedIndex = 0;
        }
    }
}
