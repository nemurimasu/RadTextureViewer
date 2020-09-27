using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RadTextureViewer.Core
{
    public sealed class ViewerWindow : ReactiveObject, IDisposable
    {
        string _location = @"%LOCALAPPDATA%\Catznip64\texturecache\texture.entries";
        public string Location
        {
            get => _location;
            set
            {
                this.RaiseAndSetIfChanged(ref _location, value);
            }
        }

        readonly ObservableCollection<CacheEntry> _cacheContents = new ObservableCollection<CacheEntry>();
        public ReadOnlyObservableCollection<CacheEntry> CacheContents { get; }

        bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        readonly IDisposable _subscription;

        public ViewerWindow()
        {
            CacheContents = new ReadOnlyObservableCollection<CacheEntry>(_cacheContents);
            _subscription = PopulateCache(_cacheContents, this
                .WhenAnyValue(x => x.Location)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Select(location => location == null ? null : Environment.ExpandEnvironmentVariables(location))
                .DistinctUntilChanged()
                .Select(location => location == null ?
                    Observable.Empty<CacheEntry>() :
                    new Cache(location).LoadAsync().Where(e => e.BodySize > 0).ToObservable().Catch(Observable.Empty<CacheEntry>())));
        }

        IDisposable PopulateCache<TObject>(ObservableCollection<TObject> cache, IObservable<IObservable<TObject>> source)
        {
            IDisposable? populate = null;
            var subscription = source.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(next =>
                {
                    populate?.Dispose();
                    cache.Clear();
                    IsLoading = true;
                    populate = next.Buffer(TimeSpan.FromMilliseconds(100))
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(e =>
                        {
                            foreach (var entry in e)
                            {
                                cache.Add(entry);
                            }
                        }, () => IsLoading = false);
                }, () => populate?.Dispose());
            return Disposable.Create(() =>
            {
                subscription.Dispose();
                populate?.Dispose();
            });
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}
