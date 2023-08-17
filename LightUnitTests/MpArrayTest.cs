using LsMsgPack;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace LsMsgPackUnitTests
{
    [TestFixture]
    public class MpArrayTest
    {

        private static Randomizer rnd = new Randomizer();

        [TestCase(0, 1, MsgPackTypeId.MpArray4)]
        [TestCase(1, 2, MsgPackTypeId.MpArray4)]
        [TestCase(15, 16, MsgPackTypeId.MpArray4)]
        [TestCase(200, 203, MsgPackTypeId.MpArray16)]
        [TestCase(256, 259, MsgPackTypeId.MpArray16)]
        [TestCase(ushort.MaxValue, ushort.MaxValue + 3, MsgPackTypeId.MpArray16)]
        [TestCase(ushort.MaxValue + 1, ushort.MaxValue + 6, MsgPackTypeId.MpArray32)]
        // [TestCase(0x7FEFFFF9, 0x7FEFFFF9 + 6, MsgPackTypeId.MpArray32)] // Out of memory on my machine
        public void ArrayLengths(int length, int expectedBytes, MsgPackTypeId expedctedType)
        {
            object[] test = new object[length];
            int additionalBytes = FillArrayWithRandomNumbers(test);
            additionalBytes -= test.Length;
            MsgPackItem item = MsgPackTests.RoundTripTest<MpArray, object[]>(test, expectedBytes + additionalBytes, expedctedType);

            object[] ret = item.GetTypedValue<object[]>();

            Assert.AreEqual(length, ret.Length, string.Concat("Expected ", length, " items but got ", ret.Length, " items in the array."));
            for (int t = ret.Length - 1; t >= 0; t--)
            {
                Assert.AreEqual(test[t], ret[t], string.Concat("Expected ", test[t], " but got ", ret[t], " at index ", t));
            }
        }

        private int FillArrayWithRandomNumbers(object[] test)
        {
            int addSize = 0;
            for (int t = test.Length - 1; t >= 0; t--)
            {
                int newNr = rnd.Next();
                addSize += new MpInt() { Value = newNr }.ToBytes().Length;
                test[t] = newNr;
            }
            return addSize;
        }

        [TestCase(40)]
        public void AssortedMix(int expectedLength)
        {
            object[] items = new object[] {
        true,
        false,
        null,
        128,
        -30,
        5.2d,
        900.1f,
        "Hallo!",
        new byte[]{1,2,3 },
        new object[] { true,null,"yes"}
      };
            MsgPackItem item = MsgPackTests.RoundTripTest<MpArray, object[]>(items, expectedLength, MsgPackTypeId.MpArray4);

            object[] ret = item.GetTypedValue<object[]>();

            Assert.AreEqual(items.Length, ret.Length, string.Concat("Expected ", items.Length, " items but got ", ret.Length, " items in the array."));
            for (int t = ret.Length - 1; t >= 0; t--)
            {
                Assert.AreEqual(items[t], ret[t], string.Concat("Expected ", items[t], " but got ", ret[t], " at index ", t));
            }
        }
    }
}
