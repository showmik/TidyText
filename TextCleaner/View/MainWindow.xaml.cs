using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xceed.Wpf.Toolkit;

namespace TextCleaner.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var thumb = (Thumb)sender;
            var richTextBox = (Xceed.Wpf.Toolkit.RichTextBox)thumb.Parent;

            richTextBox.Width = Math.Max(richTextBox.ActualWidth + e.HorizontalChange, thumb.DesiredSize.Width);
            richTextBox.Height = Math.Max(richTextBox.ActualHeight + e.VerticalChange, thumb.DesiredSize.Height);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double maxRowHeight = windowHeight - 380; // Adjust the division value as needed

            // Update the MaxHeight property of the row containing the GridSplitter
            if(maxRowHeight > 0)
            {
                grid.RowDefinitions[0].MaxHeight = maxRowHeight;
            }
            
        }
    }
}