using System;
using System.Windows;
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


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double maxRowHeight = windowHeight - 380;

            if (maxRowHeight > 0)
            {
                MainGrid.RowDefinitions[0].MaxHeight = maxRowHeight;
            }
        }
    }
}