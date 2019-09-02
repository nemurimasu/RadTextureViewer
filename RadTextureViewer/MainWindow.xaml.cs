using RadTextureViewer.Core;
using System.Windows;

namespace RadTextureViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            host.ViewModel = new ViewerWindow();
        }
    }
}
