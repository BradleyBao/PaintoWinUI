using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Painto.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Painto
{
    public sealed partial class PenControl : UserControl
    {
        public static CustomizePenWindow penWindow;
        public delegate void MyEventHandler(object sender, EventArgs e);
        public event MyEventHandler DisableWindowControl;

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
            Init();
        }

        private void Init()
        {
            PenItemList.SelectedIndex = 0;
            var selectedItem = (GridViewItem)PenItemList.ContainerFromIndex(0);
            selectedItem?.Focus(FocusState.Programmatic);
        }

        private void PenItemList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var clickedItem = e.ClickedItem as PenData;

            if (clickedItem != null)
            {
                ToolBarWindow.penColor = clickedItem.PenColor;
                ToolBarWindow.penThickness = clickedItem.Thickness;
            }
        }

        private void PenItemList_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            var item = (sender as GridView).SelectedItem as PenData;
            DisableWindowControl?.Invoke(this, EventArgs.Empty);
            penWindow = new CustomizePenWindow(item.Thickness, item.PenColor);
            penWindow.Activate(); // Activate the window and ensure it gets focus
        }
    }
}
