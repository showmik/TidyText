using System.Windows;
using TextCleaner.ViewModel;

namespace TextCleaner.View
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