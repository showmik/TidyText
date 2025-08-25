using System.Windows;
using TidyText.ViewModel;

namespace TidyText.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isCtrlKeyPressed;

        public MainWindow()
        {
            InitializeComponent();
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