using Microsoft.Win32;
using RadTextureViewer.Core;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive.Disposables;

namespace RadTextureViewer
{
    /// <summary>
    /// Interaction logic for CacheView.xaml
    /// </summary>
    public partial class CacheView : ReactiveUserControl<ViewerWindow>
    {
        public CacheView()
        {
            InitializeComponent();

            this.WhenActivated(disposableRegistration =>
            {
                this.Bind(ViewModel,
                        viewModel => viewModel.Location,
                        view => view.locationTextBox.Text)
                    .DisposeWith(disposableRegistration);

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.CacheContents,
                        view => view.contentListView.ItemsSource)
                    .DisposeWith(disposableRegistration);

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.IsLoading,
                        view => view.loadingProgress.Visibility)
                    .DisposeWith(disposableRegistration);
            });
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var path = Environment.ExpandEnvironmentVariables(locationTextBox.Text);
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CustomPlaces =
                {
                    new FileDialogCustomPlace(new Guid(0xf1b32785, 0x6fba, 0x4fcf, 0x9d, 0x55, 0x7b, 0x8e, 0x7f, 0x15, 0x70, 0x91))
                },
                InitialDirectory = Path.GetDirectoryName(path),
                FileName = path,
                Filter = "Texture Cache Files (texture.cache)|texture.cache"
            };
            if (dialog.ShowDialog() == true)
            {
                locationTextBox.Text = dialog.FileName;
            }
        }
    }
}
