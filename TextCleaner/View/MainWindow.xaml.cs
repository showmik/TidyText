using System.Windows;
using TidyText.ViewModel;

namespace TidyText.View
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
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