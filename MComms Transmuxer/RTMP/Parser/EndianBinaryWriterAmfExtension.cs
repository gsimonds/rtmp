namespace MComms_Transmuxer.RTMP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using MComms_Transmuxer.Common;

    public static class EndianBinaryWriterAmfExtension
    {
        public static void WriteAmf0(this EndianBinaryWriter writer, double number)
        {
            writer.Write((byte)RtmpAmf0Types.Number);
            writer.Write(number);
        }

        public static void WriteAmf0(this EndianBinaryWriter writer, bool boolean)
        {
            writer.Write((byte)RtmpAmf0Types.Boolean);
            writer.Write(boolean);
        }

        public static void WriteAmf0(this EndianBinaryWriter writer, string str, bool objectStart = false)
        {
            if (objectStart == false)
            {
                writer.Write((byte)RtmpAmf0Types.String);
            }

            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(str);
            writer.Write((ushort)utf8.Length);
            writer.Write(utf8);
        }

        public static void WriteAmf0(this EndianBinaryWriter writer, RtmpAmfObject amfObject, bool isArray = false)
        {
            if (!isArray)
            {
                writer.Write((byte)RtmpAmf0Types.Object);
            }
            else
            {
                writer.Write((byte)RtmpAmf0Types.Array);
                writer.Write((int)(amfObject.Booleans.Count + amfObject.Numbers.Count + amfObject.Strings.Count + amfObject.Nulls));
            }

            foreach (var s in amfObject.Strings)
            {
                writer.WriteAmf0(s.Key, true);
                writer.WriteAmf0(s.Value);
            }

            foreach (var s in amfObject.Numbers)
            {
                writer.WriteAmf0(s.Key, true);
                writer.WriteAmf0(s.Value);
            }

            foreach (var s in amfObject.Booleans)
            {
                writer.WriteAmf0(s.Key, true);
                writer.WriteAmf0(s.Value);
            }

            //objects end with 0x00,0x00, (oject end identifier [0x09 in this case])
            writer.Write((byte)0x00);
            writer.Write((byte)0x00);
            writer.Write((byte)RtmpAmf0Types.ObjectEnd);
        }

        public static void WriteAmf0(this EndianBinaryWriter writer, object obj)
        {
            Type objType = obj.GetType();

            if (objType == typeof(double))
            {
                writer.WriteAmf0((double)obj);
            }
            else if (objType == typeof(bool))
            {
                writer.WriteAmf0((bool)obj);
            }
            else if (objType == typeof(string))
            {
                writer.WriteAmf0((string)obj);
            }
            else if (objType == typeof(RtmpAmfNull))
            {
                writer.WriteAmf0Null();
            }
            else if (objType == typeof(RtmpAmfObject))
            {
                writer.WriteAmf0((RtmpAmfObject)obj);
            }
        }

        public static void WriteAmf0(this EndianBinaryWriter writer, List<object> objList, int startIndex = 0)
        {
            int index = 0;
            foreach (object obj in objList)
            {
                if (index++ < startIndex) continue;
                writer.WriteAmf0(obj);
            }
        }

        public static void WriteAmf0Null(this EndianBinaryWriter writer)
        {
            writer.Write((byte)RtmpAmf0Types.Null);
        }
    }
}
