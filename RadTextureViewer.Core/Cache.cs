using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace RadTextureViewer.Core
{
    class Cache
    {
        readonly string _location;

        const int HEADER_SIZE = 4 * 11;
        const int ENTRY_SIZE = 4 * 7;
        const int CACHE_SIZE = 600;

        public Cache(string location)
        {
            _location = location;
        }

        public async IAsyncEnumerable<CacheEntry> LoadAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            var root = Path.GetDirectoryName(_location) ?? ".";
            await using var indexStream = new FileStream(_location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);
            await using var cacheStream = new FileStream(Path.Combine(root, "texture.cache"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);

            using var bufferOwner = MemoryPool<byte>.Shared.Rent(HEADER_SIZE);
            var buffer = bufferOwner.Memory.Slice(0, HEADER_SIZE);
            if (await indexStream.ReadAsync(buffer, ct) != HEADER_SIZE)
            {
                throw new EndOfStreamException();
            }

            var version = BitConverter.ToSingle(buffer.Slice(0, 4).Span);
            var address = BitConverter.ToUInt32(buffer.Slice(4, 4).Span);
            var encoder = Encoding.UTF8.GetString(buffer.Slice(8, 32).Span).TrimEnd('\0');
            var entryCount = BitConverter.ToUInt32(buffer.Slice(40, 4).Span);

            for (var i = 0u; i < entryCount; i++)
            {
                if (await indexStream.ReadAsync(buffer.Slice(0, ENTRY_SIZE), ct) != ENTRY_SIZE)
                {
                    throw new EndOfStreamException();
                }

                var id = new Guid(BinaryPrimitives.ReadInt32BigEndian(buffer.Slice(0, 4).Span),
                        BinaryPrimitives.ReadInt16BigEndian(buffer.Slice(4, 2).Span),
                        BinaryPrimitives.ReadInt16BigEndian(buffer.Slice(6, 2).Span),
                        buffer.Span[8], buffer.Span[9], buffer.Span[10], buffer.Span[11],
                        buffer.Span[12], buffer.Span[13], buffer.Span[14], buffer.Span[15]);
                var bodySize = BitConverter.ToUInt32(buffer.Slice(20, 4).Span);
                var imageSize = BitConverter.ToUInt32(buffer.Slice(16, 4).Span);
                var time = BitConverter.ToInt32(buffer.Slice(24, 4).Span);

                var prefix = new Memory<byte>(new byte[Math.Min(CACHE_SIZE, imageSize - bodySize)]);
                if (await cacheStream.ReadAsync(prefix) != prefix.Length)
                {
                    // all remaining entries, if any, are incomplete
                    throw new EndOfStreamException();
                }
                if (prefix.Length < CACHE_SIZE)
                {
                    cacheStream.Seek(CACHE_SIZE - prefix.Length, SeekOrigin.Current);
                }

                yield return new CacheEntry(root, i, id, bodySize, time, prefix);
            }
        }
    }
}
