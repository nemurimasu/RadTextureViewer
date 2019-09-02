using Microsoft.Win32;
using RadTextureViewer.Core;
using ReactiveUI;
using System;
using System.Globalization;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RadTextureViewer
{
    /// <summary>
    /// Interaction logic for CacheEntryView.xaml
    /// </summary>
    public partial class CacheEntryView : ReactiveUserControl<CacheEntry>
    {
        public CacheEntryView()
        {
            InitializeComponent();
            this.WhenActivated(disposableRegistration =>
            {
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.Uuid,
                        view => view.uuid.Text)
                    .DisposeWith(disposableRegistration);
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.ImageSize,
                        view => view.size.Text,
                        size => (size / 1024.0).ToString("f1", CultureInfo.CurrentCulture))
                    .DisposeWith(disposableRegistration);
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.Time,
                        view => view.time.Text)
                    .DisposeWith(disposableRegistration);

                MouseDoubleClick += (o, e) =>
                {
                    var dialog = new SaveFileDialog
                    {
                        AddExtension = true,
                        CheckPathExists = true,
                        FileName = $"{ViewModel.Uuid}.jp2",
                        Filter = "JPEG 2000 Files (*.jp2)|*.jp2",
                        OverwritePrompt = true,
                        DefaultExt = ".jp2"
                    };
                    var result = dialog.ShowDialog(Window.GetWindow(this));
                    if (result == true)
                    {
                        var model = ViewModel;
                        var path = dialog.FileName;
                        Task.Run(async () =>
                        {
                            var image = await model.LoadAsync(CancellationToken.None).ConfigureAwait(false);
                            await using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                            await output.WriteAsync(image);
                        });
                    }
                };
                disposableRegistration.Add(ViewModel.Thumbnail
                    .Select(raw =>
                    {
                        if (raw == null)
                        {
                            return null;
                        }
                        BitmapSource thumbnail;
                        unsafe
                        {
                            fixed (int* data = raw.Value.Data)
                            {
                                thumbnail = BitmapSource.Create(
                                    raw.Value.Width, raw.Value.Height, 96, 96, PixelFormats.Pbgra32, null,
                                    new IntPtr(data), raw.Value.Data.Length * 4, raw.Value.Width * 4);
                            }
                        }
                        thumbnail.Freeze();
                        return thumbnail;
                    })
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(thumbnail =>
                    {
                        image.Source = thumbnail;
                    }));
            });
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            if (hitTestParameters == null)
            {
                throw new ArgumentNullException(nameof(hitTestParameters));
            }
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
    }
}
