namespace MComms_Transmuxer.SmoothStreaming
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;

    public class SmoothStreamingSegmenter
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEFORMATEX
        {
            public ushort wFormatTag;
            public ushort nChannels;
            public uint nSamplesPerSec;
            public uint nAvgBytesPerSec;
            public ushort nBlockAlign;
            public ushort wBitsPerSample;
            public ushort cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public Int32 left;
            public Int32 top;
            public Int32 right;
            public Int32 bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public UInt32 biSize;
            public Int32 biWidth;
            public Int32 biHeight;
            public UInt16 biPlanes;
            public UInt16 biBitCount;
            public UInt32 biCompression;
            public UInt32 biSizeImage;
            public Int32 biXPelsPerMeter;
            public Int32 biYPelsPerMeter;
            public UInt32 biClrUsed;
            public UInt32 biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VIDEOINFOHEADER2
        {
            public RECT rcSource;
            public RECT rcTarget;
            public UInt32 dwBitRate;
            public UInt32 dwBitErrorRate;
            public Int64 AvgTimePerFrame;
            public UInt32 dwInterlaceFlags; // use AMINTERLACE_* defines. Reject connection if undefined bits are not 0
            public UInt32 dwCopyProtectFlags; // use AMCOPYPROTECT_* defines. Reject connection if undefined bits are not 0
            public UInt32 dwPictAspectRatioX; // X dimension of picture aspect ratio, e.g. 16 for 16x9 display
            public UInt32 dwPictAspectRatioY; // Y dimension of picture aspect ratio, e.g.  9 for 16x9 display
            public UInt32 dwControlFlags; // use AMCONTROL_* defines, use this from now on
            public UInt32 dwReserved2; // must be 0; reject connection otherwise
            public BITMAPINFOHEADER bmiHeader;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MPEG2VIDEOINFO
        {
            public VIDEOINFOHEADER2 hdr;
            public UInt32 dwStartTimeCode;
            public UInt32 cbSequenceHeader;
            public UInt32 dwProfile;
            public UInt32 dwLevel;
            public UInt32 dwFlags;
        }

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MCSSF_Initialize();

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MCSSF_AddStream(
            [In] Int32 muxId,
            [In] Int32 streamType,
            [In] Int32 bitrate,
            [In] UInt16 language,
            [In] Int32 extraDataSize,
            [In] IntPtr extraData);

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MCSSF_GetHeader(
            [In] Int32 muxId,
            [In] Int32 streamId,
            [Out] out Int32 dataSize,
            [Out] out IntPtr data);

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MCSSF_PushMedia(
            [In] Int32 muxId,
            [In] Int32 streamId,
            [In] Int64 startTime,
            [In] Int64 stopTime,
            [In] bool keyFrame,
            [In] Int32 sampleDataSize,
            [In] IntPtr sampleData,
            [Out] out Int32 outputDataSize,
            [Out] out IntPtr outputData);

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MCSSF_GetIndex(
            [In] Int32 muxId,
            [In] Int32 streamId,
            [Out] out Int32 dataSize,
            [Out] out IntPtr data);

        [DllImport("MCommsSSFSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MCSSF_Uninitialize([In] Int32 muxId);
    }
}
