using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RadTextureViewer.Core
{
    class OpenJpeg
    {
        unsafe static readonly IntPtr dparams;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "impossible")]
        static OpenJpeg()
        {
            dparams = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.opj_dparameters>());
            NativeMethods.opj_set_default_decoder_parameters(dparams);
        }

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct StreamState
        {
            public byte* data;
            public ulong pos;
            public ulong len;
        }

        public static Image Decode(ReadOnlySpan<byte> data, int maxDimension)
        {
            unsafe
            {
                fixed (byte* dataPtr = data)
                {
                    var state = Marshal.AllocHGlobal(Marshal.SizeOf<StreamState>());
                    Marshal.StructureToPtr(new StreamState
                    {
                        data = dataPtr,
                        pos = 0,
                        len = (ulong)data.Length
                    }, state, false);
                    var destroyUserData = new NativeMethods.opj_stream_free_user_data_fn(DestroyUserData);
                    var readData = new NativeMethods.opj_stream_read_fn(ReadData);
                    var seekData = new NativeMethods.opj_stream_seek_fn(SeekData);
                    var skipData = new NativeMethods.opj_stream_skip_fn(SkipData);
                    var writeInfo = new NativeMethods.opj_msg_callback(WriteInfo);
                    var writeWarning = new NativeMethods.opj_msg_callback(WriteWarning);
                    var writeError = new NativeMethods.opj_msg_callback(WriteError);
                    using var stream = NativeMethods.opj_stream_default_create(true);
                    try
                    {
                        NativeMethods.opj_stream_set_user_data(stream, state.ToPointer(), destroyUserData);
                        NativeMethods.opj_stream_set_read_function(stream, readData);
                        NativeMethods.opj_stream_set_seek_function(stream, seekData);
                        NativeMethods.opj_stream_set_skip_function(stream, skipData);
                        NativeMethods.opj_stream_set_user_data_length(stream, (ulong)data.Length);

                        using var codec = NativeMethods.opj_create_decompress(NativeMethods.CODEC_FORMAT.CODEC_J2K);
                        if (!NativeMethods.opj_setup_decoder(codec, dparams))
                        {
                            throw new OpenJpegException(Resources.Jpeg2000ErrorUnexpected);
                        }
                        NativeMethods.opj_set_info_handler(codec, writeInfo, null);
                        NativeMethods.opj_set_warning_handler(codec, writeWarning, null);
                        NativeMethods.opj_set_error_handler(codec, writeError, null);

                        NativeMethods.opj_image* image = null;
                        try
                        {
                            if (!NativeMethods.opj_read_header(stream, codec, &image))
                            {
                                throw new OpenJpegException(Resources.Jpeg2000ErrorImageHeaderDecode);
                            }
                            var width = (int)(image->x1 - image->x0);
                            var height = (int)(image->y1 - image->y0);
                            var inputMax = Math.Max(width, height);
                            if (inputMax > maxDimension)
                            {
                                var factor = Log2(inputMax / maxDimension);
                                NativeMethods.opj_set_decoded_resolution_factor(codec, (uint)factor);
                                width = Math.Max(1, width >> factor);
                                height = Math.Max(1, height >> factor);
                            }
                            if (!(NativeMethods.opj_decode(codec, stream, image) && NativeMethods.opj_end_decompress(codec, stream)))
                            {
                                throw new OpenJpegException(Resources.Jpeg2000ErrorImageDecode);
                            }

                            var bitmapData = new int[width * height];
                            fixed (int* dataStart = bitmapData)
                            {
                                if (image->numcomps == 1)
                                {
                                    var max = (1 << (int)image->comps[0].prec) - 1;
                                    var shift = (int)image->comps[0].prec - 8;
                                    var offset = image->comps[0].sgnd ? 1 << (int)(image->comps[0].prec - 1) : 0;
                                    var c_in = image->comps[0].data;
                                    var c_out = dataStart;
                                    for (var y = 0; y < image->comps[0].h; y++)
                                    {
                                        for (var x = 0; x < image->comps[0].w; x++)
                                        {
                                            var value = (uint)Math.Min(Math.Max(0, *(c_in++) + offset), max) >> shift;
                                            var c_outy = c_out;
                                            for (var i = 0; i < image->comps[0].dy; i++)
                                            {
                                                var c_outx = c_outy;
                                                for (var j = 0; j < image->comps[0].dx; j++)
                                                {
                                                    *(c_outx++) = (int)((value << 16) | (value << 8) | value);
                                                }
                                                c_outy += image->comps[0].w;
                                            }
                                            c_out += image->comps[0].dx;
                                        }
                                        c_out += image->comps[0].w * (image->comps[0].dy - 1);
                                    }
                                }
                                else if (image->numcomps == 2)
                                {
                                    var max = (1 << (int)image->comps[0].prec) - 1;
                                    var shift = (int)image->comps[0].prec - 8;
                                    var offset = image->comps[0].sgnd ? 1 << (int)(image->comps[0].prec - 1) : 0;
                                    var c_in = image->comps[0].data;
                                    var c_out = dataStart;
                                    for (var y = 0; y < image->comps[0].h; y++)
                                    {
                                        for (var x = 0; x < image->comps[0].w; x++)
                                        {
                                            var value = (uint)Math.Min(Math.Max(0, *(c_in++) + offset), max) >> shift;
                                            var c_outy = c_out;
                                            for (var i = 0; i < image->comps[0].dy; i++)
                                            {
                                                var c_outx = c_outy;
                                                for (var j = 0; j < image->comps[0].dx; j++)
                                                {
                                                    *(c_outx++) = (int)((value << 16) | (value << 8) | value);
                                                }
                                                c_outy += image->comps[0].w;
                                            }
                                            c_out += image->comps[0].dx;
                                        }
                                        c_out += image->comps[0].w * (image->comps[0].dy - 1);
                                    }
                                    max = (1 << (int)image->comps[1].prec) - 1;
                                    shift = (int)image->comps[1].prec - 8;
                                    offset = image->comps[1].sgnd ? 1 << (int)(image->comps[1].prec - 1) : 0;
                                    c_in = image->comps[1].data;
                                    c_out = dataStart;
                                    for (var y = 0; y < image->comps[1].h; y++)
                                    {
                                        for (var x = 0; x < image->comps[1].w; x++)
                                        {
                                            var value = (uint)Math.Min(Math.Max(0, *(c_in++) + offset), max) >> shift;
                                            var c_outy = c_out;
                                            for (var i = 0; i < image->comps[1].dy; i++)
                                            {
                                                var c_outx = c_outy;
                                                for (var j = 0; j < image->comps[1].dx; j++)
                                                {
                                                    *c_outx = (int)(value << 24) | *c_outx;
                                                    c_outx++;
                                                }
                                                c_outy += image->comps[1].w;
                                            }
                                            c_out += image->comps[1].dx;
                                        }
                                        c_out += image->comps[1].w * (image->comps[1].dy - 1);
                                    }
                                }
                                else if (image->numcomps == 3)
                                {
                                    var max = (1 << (int)image->comps[0].prec) - 1;
                                    var shift = (int)image->comps[0].prec - 8;
                                    var offset = image->comps[0].sgnd ? 1 << (int)(image->comps[0].prec - 1) : 0;
                                    var c_in = image->comps[0].data;
                                    var c_out = dataStart;
                                    for (var y = 0; y < image->comps[0].h; y++)
                                    {
                                        for (var x = 0; x < image->comps[0].w; x++)
                                        {
                                            var value = (uint)Math.Min(Math.Max(0, *(c_in++) + offset), max) >> shift;
                                            var c_outy = c_out;
                                            for (var i = 0; i < image->comps[0].dy; i++)
                                            {
                                                var c_outx = c_outy;
                                                for (var j = 0; j < image->comps[0].dx; j++)
                                                {
                                                    *(c_outx++) = (int)(0xff000000 | (value << 16));
                                                }
                                                c_outy += image->comps[0].w;
                                            }
                                            c_out += image->comps[0].dx;
                                        }
                                        c_out += image->comps[0].w * (image->comps[0].dy - 1);
                                    }
                                    max = (1 << (int)image->comps[1].prec) - 1;
                                    shift = (int)image->comps[1].prec - 8;
                                    offset = image->comps[1].sgnd ? 1 << (int)(image->comps[1].prec - 1) : 0;
                                    c_in = image->comps[1].data;
                                    c_out = dataStart;
                                    for (var y = 0; y < image->comps[1].h; y++)
                                    {
                                        for (var x = 0; x < image->comps[1].w; x++)
                                        {
                                            var value = (uint)Math.Min(Math.Max(0, *(c_in++) + offset), max) >> shift;
                                            var c_outy = c_out;
                                            for (var i = 0; i < image->comps[1].dy; i++)
                                            {
                                                var c_outx = c_outy;
                                                for (var j = 0; j < image->comps[1].dx; j++)
                                                {
                                                    *c_outx = (int)(value << 8) | *c_outx;
                                                    c_outx++;
                                                }
                                                c_outy += image->comps[1].w;
                                            }
                                            c_out += image->comps[1].dx;
                                        }
                                        c_out += image->comps[1].w * (image->comps[1].dy - 1);
                                    }
                                    max = (1 << (int)image->comps[2].prec) - 1;
                                    shift = (int)image->comps[2].prec - 8;
                                    offset = image->comps[2].sgnd ? 1 << (int)(image->comps[2].prec - 1) : 0;
                                    c_in = image->comps[2].data;
                                    c_out = dataStart;
                                    for (var y = 0; y < image->comps[2].h; y++)
                                    {
                                        for (var x = 0; x < image->comps[2].w; x++)
                                        {
                                            var value = (uint)Math.Min(Math.Max(0, *(c_in++) + offset), max) >> shift;
                                            var c_outy = c_out;
                                            for (var i = 0; i < image->comps[2].dy; i++)
                                            {
                                                var c_outx = c_outy;
                                                for (var j = 0; j < image->comps[2].dx; j++)
                                                {
                                                    *c_outx = (int)value | *c_outx;
                                                    c_outx++;
                                                }
                                                c_outy += image->comps[2].w;
                                            }
                                            c_out += image->comps[2].dx;
                                        }
                                        c_out += image->comps[2].w * (image->comps[2].dy - 1);
                                    }
                                }
                                else if (image->numcomps == 4)
                                {
                                    var max = (1 << (int)image->comps[0].prec) - 1;
                                    var shift = (int)image->comps[0].prec - 8;
                                    var offset = image->comps[0].sgnd ? 1 << (int)(image->comps[0].prec - 1) : 0;
                                    var c_in = image->comps[0].data;
                                    var c_out = dataStart;
                                    for (var y = 0; y < image->comps[0].h; y++)
                                    {
                                        for (var x = 0; x < image->comps[0].w; x++)
                                        {
                                            var value = (uint)Math.Min(Math.Max(0, *(c_in++) + offset), max) >> shift;
                                            var c_outy = c_out;
                                            for (var i = 0; i < image->comps[0].dy; i++)
                                            {
                                                var c_outx = c_outy;
                                                for (var j = 0; j < image->comps[0].dx; j++)
                                                {
                                                    *(c_outx++) = (int)(value << 16);
                                                }
                                                c_outy += image->comps[0].w;
                                            }
                                            c_out += image->comps[0].dx;
                                        }
                                        c_out += image->comps[0].w * (image->comps[0].dy - 1);
                                    }
                                    max = (1 << (int)image->comps[1].prec) - 1;
                                    shift = (int)image->comps[1].prec - 8;
                                    offset = image->comps[1].sgnd ? 1 << (int)(image->comps[1].prec - 1) : 0;
                                    c_in = image->comps[1].data;
                                    c_out = dataStart;
                                    for (var y = 0; y < image->comps[1].h; y++)
                                    {
                                        for (var x = 0; x < image->comps[1].w; x++)
                                        {
                                            var value = (uint)Math.Min(Math.Max(0, *(c_in++) + offset), max) >> shift;
                                            var c_outy = c_out;
                                            for (var i = 0; i < image->comps[1].dy; i++)
                                            {
                                                var c_outx = c_outy;
                                                for (var j = 0; j < image->comps[1].dx; j++)
                                                {
                                                    *c_outx = (int)(value << 8) | *c_outx;
                                                    c_outx++;
                                                }
                                                c_outy += image->comps[1].w;
                                            }
                                            c_out += image->comps[1].dx;
                                        }
                                        c_out += image->comps[1].w * (image->comps[1].dy - 1);
                                    }
                                    max = (1 << (int)image->comps[2].prec) - 1;
                                    shift = (int)image->comps[2].prec - 8;
                                    offset = image->comps[2].sgnd ? 1 << (int)(image->comps[2].prec - 1) : 0;
                                    c_in = image->comps[2].data;
                                    c_out = dataStart;
                                    for (var y = 0; y < image->comps[2].h; y++)
                                    {
                                        for (var x = 0; x < image->comps[2].w; x++)
                                        {
                                            var value = (uint)Math.Min(Math.Max(0, *(c_in++) + offset), max) >> shift;
                                            var c_outy = c_out;
                                            for (var i = 0; i < image->comps[2].dy; i++)
                                            {
                                                var c_outx = c_outy;
                                                for (var j = 0; j < image->comps[2].dx; j++)
                                                {
                                                    *c_outx = (int)value | *c_outx;
                                                    c_outx++;
                                                }
                                                c_outy += image->comps[2].w;
                                            }
                                            c_out += image->comps[2].dx;
                                        }
                                        c_out += image->comps[2].w * (image->comps[2].dy - 1);
                                    }
                                    max = (1 << (int)image->comps[3].prec) - 1;
                                    shift = (int)image->comps[3].prec - 8;
                                    offset = image->comps[3].sgnd ? 1 << (int)(image->comps[3].prec - 1) : 0;
                                    c_in = image->comps[3].data;
                                    c_out = dataStart;
                                    for (var y = 0; y < image->comps[3].h; y++)
                                    {
                                        for (var x = 0; x < image->comps[3].w; x++)
                                        {
                                            var value = (uint)Math.Min(Math.Max(0, *(c_in++) + offset), max) >> shift;
                                            var c_outy = c_out;
                                            for (var i = 0; i < image->comps[3].dy; i++)
                                            {
                                                var c_outx = c_outy;
                                                for (var j = 0; j < image->comps[3].dx; j++)
                                                {
                                                    *c_outx = (int)(value << 24) | *c_outx;
                                                    c_outx++;
                                                }
                                                c_outy += image->comps[3].w;
                                            }
                                            c_out += image->comps[3].dx;
                                        }
                                        c_out += image->comps[3].w * (image->comps[3].dy - 1);
                                    }
                                }
                            }
                            return new Image(width, height, bitmapData);
                        }
                        finally
                        {
                            NativeMethods.opj_image_destroy(image);
                        }
                    }
                    finally
                    {
                        GC.KeepAlive(destroyUserData);
                        GC.KeepAlive(readData);
                        GC.KeepAlive(seekData);
                        GC.KeepAlive(skipData);
                        GC.KeepAlive(writeError);
                        GC.KeepAlive(writeWarning);
                        GC.KeepAlive(writeInfo);
                    }
                }
            }
        }

        private static unsafe void WriteError(string msg, void* client_data)
        {
            Trace.Write($"error: {msg}");
        }

        private static unsafe void WriteWarning(string msg, void* client_data)
        {
            Trace.Write($"warning: {msg}");
        }

        private static unsafe void WriteInfo(string msg, void* client_data)
        {
            Trace.Write($"info: {msg}");
        }

        static int Log2(int v)
        {
            if (v < 2)
            {
                return 0;
            }
            if (v < 4)
            {
                return 1;
            }
            if (v < 8)
            {
                return 2;
            }
            if (v < 16)
            {
                return 3;
            }
            if (v < 32)
            {
                return 4;
            }
            if (v < 64)
            {
                return 5;
            }
            if (v < 128)
            {
                return 6;
            }
            if (v < 256)
            {
                return 7;
            }
            if (v < 512)
            {
                return 8;
            }
            if (v < 1024)
            {
                return 9;
            }
            // this is used for scale factors, so no need to do more than these cases
            return 10;
        }

        unsafe static void DestroyUserData(void* data)
        {
            Marshal.FreeHGlobal(new IntPtr(data));
        }

        unsafe static IntPtr SkipData(IntPtr p_nb_bytes, void* p_user_data)
        {
            var state = (StreamState*)p_user_data;
            var offset = p_nb_bytes.ToInt64();
            if (offset < 0)
            {
                var len = Math.Min(state->pos, (ulong)-offset);
                state->pos -= len;
            }
            else
            {
                var len = Math.Min(state->len - state->pos, (ulong)offset);
                state->pos += len;
            }
            return new IntPtr((long)state->pos);
        }

        unsafe static bool SeekData(IntPtr p_nb_bytes, void* p_user_data)
        {
            var state = (StreamState*)p_user_data;
            var pos = p_nb_bytes.ToInt32();
            if (pos < 0 || (ulong)pos > state->len)
            {
                return false;
            }
            state->pos = (ulong)p_nb_bytes.ToInt64();
            return true;
        }

        unsafe static UIntPtr ReadData(byte* p_buffer, UIntPtr p_nb_bytes, void* p_user_data)
        {
            var state = (StreamState*)p_user_data;
            var len = (int)Math.Min(state->len - state->pos, p_nb_bytes.ToUInt64());
            new Span<byte>((void*)state->data, len).CopyTo(new Span<byte>((void*)p_buffer, len));
            state->pos += (ulong)len;
            return new UIntPtr((ulong)len);
        }
    }
}
