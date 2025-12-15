using System.Windows;
using System.Windows.Controls;

namespace SmartDesktop.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Simulate refresh delay
            System.Threading.Thread.Sleep(100); 
            // In a real app, this would reload data. 
            // For test verification, we might change a status label or similar if we had one.
            MessageBox.Show("Refreshed");
        }
    }
}
