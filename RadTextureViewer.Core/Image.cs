using System;
using System.Collections.Generic;

namespace RadTextureViewer.Core
{
    public readonly struct Image : IEquatable<Image>
    {
        public int Width { get; }
        public int Height { get; }
        readonly int[] _data;
        public ReadOnlySpan<int> Data => _data;

        public Image(int width, int height, int[] data)
        {
            Width = width;
            Height = height;
            _data = data;
        }

        public override bool Equals(object obj)
        {
            return obj is Image image && Equals(image);
        }

        public bool Equals(Image other)
        {
            return Width == other.Width &&
                   Height == other.Height &&
                   EqualityComparer<int[]>.Default.Equals(_data, other._data);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height, _data);
        }

        public static bool operator ==(Image left, Image right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Image left, Image right)
        {
            return !(left == right);
        }
    }
}
