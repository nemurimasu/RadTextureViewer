using ReactiveUI;
using System;
using System.Buffers;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RadTextureViewer.Core
{
    public class CacheEntry : ReactiveObject
    {
        readonly string _root;
        public uint Index { get; }
        public Guid Uuid { get; }
        public uint BodySize { get; }
        public uint ImageSize => (uint)Prefix.Length + BodySize;
        readonly int _time;
        public DateTime Time => DateTimeOffset.FromUnixTimeSeconds(_time).ToLocalTime().DateTime;
        public ReadOnlyMemory<byte> Prefix { get; }
        public IObservable<Image?> Thumbnail { get; }

        public string BodyLocation
        {
            get
            {
                var builder = new StringBuilder(_root.Length + 47);
                builder.Append(_root);
                builder.Append(Path.DirectorySeparatorChar); // 1
                var index0 = builder.Length;
                builder.Append('\0'); // 2
                builder.Append(Path.DirectorySeparatorChar); // 3
                var index1 = builder.Length;
                builder.Append(Uuid); // 39
                builder.Append(".texture"); // 47
                builder[index0] = builder[index1];
                return builder.ToString();
            }
        }

        public async Task<Image?> LoadThumbnailAsync(CancellationToken ct)
        {
            if (ImageSize == 0xffffffff)
            {
                return null;
            }

            using var imageOwner = MemoryPool<byte>.Shared.Rent((int)ImageSize);
            var image = imageOwner.Memory.Slice(0, (int)ImageSize);
            Prefix.CopyTo(imageOwner.Memory.Slice(0, Prefix.Length));

            if (BodySize > 0)
            {
                await using var bodyStream = new FileStream(BodyLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                if (await bodyStream.ReadAsync(image.Slice(Prefix.Length, (int)BodySize), ct) != BodySize)
                {
                    throw new EndOfStreamException();
                }
            }

            try
            {
                return await Task.Run(() =>
                {
                    return OpenJpeg.Decode(image.Span, 64);
                }, ct).ConfigureAwait(false);
            }
            catch (OpenJpegException)
            {
                return null;
            }
        }

        public async Task<ReadOnlyMemory<byte>> LoadAsync(CancellationToken ct)
        {
            var image = new Memory<byte>(new byte[(int)ImageSize]);
            Prefix.CopyTo(image.Slice(0, Prefix.Length));

            if (BodySize > 0)
            {
                await using var bodyStream = new FileStream(BodyLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                if (await bodyStream.ReadAsync(image.Slice(Prefix.Length, (int)BodySize), ct) != BodySize)
                {
                    throw new EndOfStreamException();
                }
            }

            return image;
        }

        public CacheEntry(string root, uint index, Guid uuid, uint bodySize, int time, ReadOnlyMemory<byte> prefix)
        {
            _root = root;
            Index = index;
            Uuid = uuid;
            BodySize = bodySize;
            _time = time;
            Prefix = prefix;
            Thumbnail = Observable.Defer(() => Observable.FromAsync(async ct =>
            {
                return await LoadThumbnailAsync(ct).ConfigureAwait(false);
            })).PublishLast().RefCount();
        }
    }
}
