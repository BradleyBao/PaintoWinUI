using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Painto.Modules;
using System.Collections.ObjectModel;

namespace Painto
{
    public sealed partial class PenControl : UserControl
    {
        public ObservableCollection<PenData> ItemsSource
        {
            get { return (ObservableCollection<PenData>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<PenData>), typeof(PenControl), new PropertyMetadata(null));

        public PenControl()
        {
            this.InitializeComponent();
        }
    }
}
