using System;
using Rudine.Web;

namespace RudineApp.App_Code
{
    public class BoogerForm101 : BaseDoc, IExternalDoc
    {
        public bool? Optional_bool { get; set; }
        public byte? Optional_byte { get; set; }
        public DateTime? Optional_DateTime { get; set; }
        public decimal? Optional_decimal { get; set; }
        public double? Optional_double { get; set; }
        public float? Optional_float { get; set; }
        public int? Optional_int { get; set; }
        public long? Optional_long { get; set; }
        public sbyte? Optional_sbyte { get; set; }
        public short? Optional_short { get; set; }
        public string Optional_String { get; set; }
        public uint? Optional_uint { get; set; }
        public ulong? Optional_ulong { get; set; }
        public ushort? Optional_ushort { get; set; }
        public byte[] RawBytes { get; set; }
        public bool Required_bool { get; set; }
        public byte Required_byte { get; set; }
        public DateTime Required_DateTime { get; set; }
        public decimal Required_decimal { get; set; }
        public double Required_double { get; set; }
        public float Required_float { get; set; }
        public int Required_int { get; set; }
        public long Required_long { get; set; }
        public sbyte Required_sbyte { get; set; }
        public short Required_short { get; set; }
        public uint Required_uint { get; set; }
        public ulong Required_ulong { get; set; }
        public ushort Required_ushort { get; set; }
    }
}