using System;
using System.IO;

namespace LsMsgPack
{
    public class MpDateTime : MpExt
    {

        public MpDateTime() : base() { TypeSpecifier = -1; }
        public MpDateTime(MpExt ext) : base()
        {
            CopyBaseDataFrom(ext);
            value = ConvertExt(ext).ToUniversalTime();
        }

        private DateTime value;

        public override object Value
        {
            get
            {
                return value.ToLocalTime();
            }
            set
            {
                if (value is DateTime)
                    this.value = ((DateTime)value).ToUniversalTime();
                else
                    this.value = Convert.ToDateTime(value).ToUniversalTime();

                base.Value = ToBaseValue(this.value);
            }
        }

        public override T GetTypedValue<T>()
        {
            return (T)(object)value.ToLocalTime();
        }

        public override MsgPackItem Read(MsgPackTypeId typeId, Stream data)
        {
            base.Read(typeId, data);
            value = ConvertExt(this);
            return this;
        }

        public override byte[] ToBytes()
        {
            if (base.Value is null || ((byte[])base.Value).Length <= 0)
                return FromDateTime(value).ToBytes();

            return base.ToBytes();
        }

        public override string ToString()
        {
            return string.Concat("DateTime (", GetOfficialTypeName(TypeId), ") extension type ", TypeSpecifier, " with the value ", value.ToLongDateString());
        }

        public static DateTime ConvertExt(MpExt ext)
        {
            if (ext.TypeSpecifier != -1)
                throw new MsgPackException(string.Concat("The extension type ", ext.TypeSpecifier, " is not a recognised DatTime or TimeStamp, expected type -1."), -1, ext.TypeId);

            switch (ext.TypeId)
            {
                case MsgPackTypeId.MpFExt4:
                    uint seconds = BitConverter.ToUInt32(SwapIfLittleEndian((byte[])ext.Value), 0);
                    return EpochToLocalDateTime(seconds);

                case MsgPackTypeId.MpFExt8:
                    byte[] bitVal = (byte[])ext.Value;

                    if (BitConverter.IsLittleEndian)
                    {
                        SwapIfLittleEndian(bitVal, 0, 4);
                        SwapIfLittleEndian(bitVal, 4, 4);
                    }

                    ulong bytes = BitConverter.ToUInt64(bitVal, 0);
                    ulong nanoSecc;
                    ulong sec;
                    long ticks;
                    unchecked
                    {
                        nanoSecc = bytes >> 34;
                        ulong mask = 0x3FFFFFFFF;
                        sec = (bytes & mask);
                        ticks = (long)nanoSecc / 100;
                    }

                    DateTime ret = Zero.AddSeconds(sec).Add(new TimeSpan(ticks));
                    return ret.ToLocalTime();
                case MsgPackTypeId.MpExt8:
                    byte[] vall = (byte[])ext.Value;
                    uint nanoSec = BitConverter.ToUInt32(SwapIfLittleEndian(vall, 0, 4), 0);
                    long sc = BitConverter.ToInt64(SwapIfLittleEndian(vall, 4, 8), 0);
                    long tick = nanoSec / 100;
                    TimeSpan subSec = TimeSpan.FromTicks(tick);
                    if (sc < 0)
                        return EpochToLocalDateTime(sc) - subSec;
                    else
                        return EpochToLocalDateTime(sc) + subSec;
            }

            throw new MsgPackException(string.Concat("The extension type with base type ", GetOfficialTypeName(ext.TypeId), " is not recognised as a DatTime or TimeStamp. expected ", GetOfficialTypeName(MsgPackTypeId.MpFExt4), " or ", GetOfficialTypeName(MsgPackTypeId.MpFExt8), " or ", GetOfficialTypeName(MsgPackTypeId.MpExt8)), -1, ext.TypeId);
        }

        public static MpDateTime FromDateTime(DateTime dt, bool preserveFractionalSeconds = true)
        {
            dt = dt.ToUniversalTime();

            if (dt < Zero || dt > MaxFExt8)
                return ToExt8(dt); // lartgest 15 bytes

            if (dt > MaxFExt4 || (preserveFractionalSeconds && HasFractionalSeconds(dt)))
                return ToFExt8(dt); // 10 bytes

            return ToFExt4(dt); // 6 bytes
        }

        private static byte[] ToBaseValue(DateTime dt, bool preserveFractionalSeconds = true)
        {
            dt = dt.ToUniversalTime();

            if (dt < Zero || dt > MaxFExt8)
                return ToExt8Bytes(dt); // lartgest 15 bytes

            if (dt > MaxFExt4 || (preserveFractionalSeconds && HasFractionalSeconds(dt)))
                return ToFExt8Bytes(dt); // 10 bytes

            return ToFExt4Bytes(dt); // 6 bytes
        }

        private static bool HasFractionalSeconds(DateTime dt)
        {
            return (dt.Ticks % TimeSpan.TicksPerSecond) != 0;
        }

        private static byte[] ToFExt4Bytes(DateTime dt)
        {
            long seconds = DateTimeToEpoch(dt);
            uint sec = (uint)seconds;
            return SwapIfLittleEndian(BitConverter.GetBytes(sec));
        }

        private static MpDateTime ToFExt4(DateTime dt)
        {
            MpDateTime mpdt = new MpDateTime()
            {
                BaseValue = ToFExt4Bytes(dt),
                Value = dt
            };
            return mpdt;
        }

        private static byte[] ToFExt8Bytes(DateTime dt)
        {
            ulong seconds = (ulong)DateTimeToEpoch(dt);
            long ticksFractional = (dt.Ticks % TimeSpan.TicksPerSecond);
            ulong nanoSec = (ulong)ticksFractional * 100;

            ulong prepareBytes = nanoSec << 34;
            prepareBytes = prepareBytes | seconds;
            byte[] bitVal = BitConverter.GetBytes(prepareBytes);
            if (BitConverter.IsLittleEndian)
            {
                SwapIfLittleEndian(bitVal, 0, 4);
                SwapIfLittleEndian(bitVal, 4, 4);
            }
            return bitVal;
        }

        private static MpDateTime ToFExt8(DateTime dt)
        {
            MpDateTime mpdt = new MpDateTime()
            {
                BaseValue = ToFExt8Bytes(dt),
                Value = dt
            };
            return mpdt;
        }

        private static byte[] ToExt8Bytes(DateTime dt)
        {
            long seconds = DateTimeToEpoch(dt);
            long ticksFractional = (dt.Ticks % TimeSpan.TicksPerSecond);
            uint nanoSec = (uint)ticksFractional * 100;
            byte[] bitVal = new byte[12];
            SwapIfLittleEndian(BitConverter.GetBytes(nanoSec)).CopyTo(bitVal, 0);
            SwapIfLittleEndian(BitConverter.GetBytes(seconds)).CopyTo(bitVal, 4);
            return bitVal;
        }

        private static MpDateTime ToExt8(DateTime dt)
        {
            MpDateTime mpdt = new MpDateTime()
            {
                BaseValue = ToExt8Bytes(dt),
                Value = dt
            };
            return mpdt;
        }

        public static readonly DateTime Zero = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime MaxFExt4 = Zero.AddSeconds(uint.MaxValue);
        public static readonly DateTime MaxFExt8 = new DateTime(2514, 5, 30, 1, 53, 03, DateTimeKind.Utc).Add(TimeSpan.FromTicks(9999999));

        public static DateTime EpochToLocalDateTime(long secconds)
        {
            DateTime dt = Zero.AddSeconds(secconds).ToLocalTime();
            return dt;
        }

        public static long DateTimeToEpoch(DateTime dateTime)
        {
            DateTime uni = dateTime.ToUniversalTime();
            TimeSpan diff = uni - Zero;
            return diff.Ticks / TimeSpan.TicksPerSecond; // Do not use diff.TotalSecconds, it has rounding errors!
        }
    }
}
