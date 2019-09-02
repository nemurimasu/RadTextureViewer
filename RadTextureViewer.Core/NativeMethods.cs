using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace RadTextureViewer.Core
{
    static class NativeMethods
    {
        [DllImport("openjp2")]
        public static extern StreamHandle opj_stream_default_create(bool p_is_input);

        [DllImport("openjp2")]
        static extern void opj_stream_destroy(IntPtr handle);

        [DllImport("openjp2")]
        public static extern void opj_stream_set_read_function(StreamHandle p_stream, opj_stream_read_fn p_function);
        public unsafe delegate UIntPtr opj_stream_read_fn(byte* p_buffer, UIntPtr p_nb_bytes, void* p_user_data);

        [DllImport("openjp2")]
        public static extern void opj_stream_set_skip_function(StreamHandle p_stream, opj_stream_skip_fn p_function);
        public unsafe delegate IntPtr opj_stream_skip_fn(IntPtr p_nb_bytes, void* p_user_data);

        [DllImport("openjp2")]
        public static extern void opj_stream_set_seek_function(StreamHandle p_stream, opj_stream_seek_fn p_function);
        public unsafe delegate bool opj_stream_seek_fn(IntPtr p_nb_bytes, void* p_user_data);

        [DllImport("openjp2")]
        public unsafe static extern void opj_stream_set_user_data(StreamHandle p_stream, void* p_data, opj_stream_free_user_data_fn p_function);
        public unsafe delegate void opj_stream_free_user_data_fn(void* p_user_data);

        [DllImport("openjp2")]
        public static extern void opj_stream_set_user_data_length(StreamHandle p_stream, ulong data_length);


        [DllImport("openjp2")]
        public static extern CodecHandle opj_create_decompress(CODEC_FORMAT format);

        [DllImport("openjp2")]
        static extern void opj_destroy_codec(IntPtr handle);

        [DllImport("openjp2")]
        public unsafe static extern bool opj_setup_decoder(CodecHandle p_codec, IntPtr parameters);

        [DllImport("openjp2")]
        public unsafe static extern void opj_set_default_decoder_parameters(IntPtr parameters);

        [DllImport("openjp2")]
        public static extern bool opj_set_decoded_resolution_factor(CodecHandle p_codec, uint res_factor);

        [DllImport("openjp2")]
        public unsafe static extern bool opj_read_header(StreamHandle p_stream, CodecHandle p_codec, opj_image** p_image);

        [DllImport("openjp2")]
        public unsafe static extern bool opj_decode(CodecHandle codec, StreamHandle stream, opj_image* image);

        [DllImport("openjp2")]
        public static extern bool opj_end_decompress(CodecHandle codec, StreamHandle stream);

        [DllImport("openjp2")]
        public unsafe static extern void opj_image_destroy(opj_image* image);

        [DllImport("openjp2")]
        public unsafe static extern bool opj_set_info_handler(CodecHandle p_codec, opj_msg_callback p_callback, void* p_user_data);
        [DllImport("openjp2")]
        public unsafe static extern bool opj_set_warning_handler(CodecHandle p_codec, opj_msg_callback p_callback, void* p_user_data);
        [DllImport("openjp2")]
        public unsafe static extern bool opj_set_error_handler(CodecHandle p_codec, opj_msg_callback p_callback, void* p_user_data);
        public unsafe delegate void opj_msg_callback([MarshalAs(UnmanagedType.LPUTF8Str)]string msg, void* client_data);

        public enum CODEC_FORMAT
        {
            CODEC_J2K = 0
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct opj_dparameters
        {
            public uint cp_reduce;
            public uint cp_layer;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
            public byte[] infile;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4096)]
            public byte[] outfile;
            public int decod_format;
            public int cod_format;
            public uint DA_x0;
            public uint DA_x1;
            public uint DA_y0;
            public uint DA_y1;
            public bool m_verbose;
            public uint tile_index;
            public uint nb_tile_to_decode;
            public bool jpwl_correct;
            public int jpwl_exp_comps;
            public int jpwl_max_tiles;
            public int flags;
        }

        [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
        public class CodecHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private CodecHandle()
                : base(true)
            {
            }
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            override protected bool ReleaseHandle()
            {
                opj_destroy_codec(handle);
                return true;
            }
        }

        [SecurityPermission(SecurityAction.InheritanceDemand, UnmanagedCode = true)]
        [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
        public class StreamHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private StreamHandle()
                : base(true)
            {
            }
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
            override protected bool ReleaseHandle()
            {
                opj_stream_destroy(handle);
                return true;
            }
        }

        public enum OPJ_COLOR_SPACE
        {
            OPJ_CLRSPC_UNKNOWN = 0,
            OPJ_CLRSPC_UNSPECIFIED,
            OPJ_CLRSPC_SRGB,
            OPJ_CLRSPC_GRAY,
            OPJ_CLRSPC_SYCC,
            OPJ_CLRSPC_EYCC,
            OPJ_CLR_PC_CMYK
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct opj_image
        {
            public uint x0;
            public uint y0;
            public uint x1;
            public uint y1;
            public uint numcomps;
            public OPJ_COLOR_SPACE color_space;
            public opj_image_comp* comps;
            public byte* iccp_profile_buf;
            public uint icc_profile_len;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct opj_image_comp
        {
            public uint dx;
            public uint dy;
            public uint w;
            public uint h;
            public uint x0;
            public uint y0;
            public uint prec;
            public uint bpp;
            public bool sgnd;
            public uint resno_decoded;
            public uint factor;
            public int* data;
            public ushort alpha;
        }
    }
}
