using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace ReportGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Test1();
        }

        static void Test1()
        {
            var rs = new ReportService();
            var digestBytes = new byte[16];
            for (var i = 0; i < digestBytes.Length; i++)
            {
                digestBytes[i] = 7;
            }
            var digest = ByteString.CopyFrom(digestBytes);
            var observers = new byte[] {0, 1, 2, 3, 4};
            var observations = new [] { -123, 245, -13213123, 123214214124421, 1};
            var report = rs.GenerateEthereumReport(digest, 16, 16, observers, observations);
            report = report.Substring(2, report.Length - 2);
            //Console.WriteLine(report.Length);
            var loopCount = report.Length / 64;
            var total = loopCount;
            while (loopCount > 0)
            {
                var sub = report.Substring((total - loopCount) * 64, 64);
                //Console.WriteLine(sub);
                loopCount--;
            }

            report =
                "0000000000000000000000070707070707070707070707070707070000001010000102030400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000600000000000000000000000000000000000000000000000000000000000000005000000000000000000000000000000000000000000000000000000000000007b00000000000000000000000000000000000000000000000000000000000000f50000000000000000000000000000000000000000000000000000000000c99dc3000000000000000000000000000000000000000000000000000070100b76d3850000000000000000000000000000000000000000000000000000000000000001";
            loopCount = report.Length / 64;
            total = loopCount;
            //Console.WriteLine("========= real");
            while (loopCount > 0)
            {
                var sub = report.Substring((total - loopCount) * 64, 64);
                //Console.WriteLine(sub);
                loopCount--;
            }
            var hashBytes = ByteStringHelper.FromHexString(report);
            var shaHash = HashHelper.ComputeFrom(hashBytes.ToByteArray());
            Console.WriteLine(shaHash);
        }
    }

    public interface IReportService
    {
        string GenerateEthereumReport(ByteString configDigest, uint epoch, byte round, byte[] observer, long[] observation);
        string SerializeReport(string dataTypeStr, object[] data);
    }
    
    public class ReportService: IReportService
    {
        public const string ArraySuffix = "[]";
        public const string Bytes32 = "bytes32";
        public const string Int192 = "int192";
        public const int SlotByteSize = 32;

        private readonly Dictionary<string, Func<object, string>> serizalization;

        public ReportService()
        {
            serizalization = new Dictionary<string, Func<object, string>>
            {
                [Bytes32] = ConvertBytes32, [Int192] = ConvertLong
            };
        }

        public string GenerateEthereumReport(ByteString configDigest, uint epoch, byte round, byte[] observer,
            long[] observation)
        {
            var configText = GenerateConfigText(configDigest, epoch, round);
            var observerIndex = GenerateObserver(observer);
            var data = new object[3];
            data[0] = configText;
            data[1] = observerIndex;
            data[2] = observation;
            return SerializeReport("bytes32,bytes32,int192[]", data);
        }

        public const long MaxEpoch = 4294967296;
        public byte[] GenerateConfigText(ByteString configDigest, uint epoch, byte round)
        {
            if (epoch > MaxEpoch)
            {
                throw new AssertionException("invalid epoch");
            }
            var configText = new byte[SlotByteSize];
            var digestBytes = configDigest.ToByteArray();
            Buffer.BlockCopy(digestBytes, 0, configText, 11, 16);
            var epochBytes = epoch.ToBytes();
            Buffer.BlockCopy(epochBytes, 0, configText, 27, 4);
            configText[SlotByteSize.Sub(1)] = round;
            return configText;
        }

        public byte[] GenerateObserver(byte[] observer)
        {
            var observerIndex = new byte[SlotByteSize];
            Buffer.BlockCopy(observer, 0, observerIndex, 0, observer.Length);
            return observerIndex;
        }
        
        public string SerializeReport(string dataTypeStr, object[] data)
        {
            var dataType = dataTypeStr.Split(',');
            var dataLength = (long)dataType.Length;
            if(dataLength != data.Length)
                throw new AssertionException("invalid data length");
            var result = new StringBuilder("0x");
            long currentIndex = dataLength;
            var lazyData = new StringBuilder();
            for (int i = 0; i < dataLength; i++)
            {
                var typeStrLen = dataType[i].Length;
                if (String.CompareOrdinal(dataType[i].Substring(typeStrLen.Sub(2), 2), ArraySuffix) == 0)
                {
                    var typePrefix = dataType[i].Substring(0, typeStrLen.Sub(2));
                    var dataList = data[i] as IEnumerable<long>;
                    long arrayLength = dataList.Count();
                    long dataPosition = currentIndex.Mul(SlotByteSize);
                    result.Append(ConvertLong(dataPosition));
                    currentIndex = currentIndex.Add(arrayLength).Add(1);
                    lazyData.Append(ConvertLong(arrayLength));
                    lazyData.Append(ConvertArray(typePrefix, dataList));
                    continue;
                }
                result.Append(serizalization[dataType[i]](data[i]));
            }

            result.Append(lazyData);
            return result.ToString();
        }
        public string ConvertLong(object i)
        {
            var data = (long) i;
            var b = data.ToBytes();
            if (b.Length == SlotByteSize)
                return b.ToHex();
            var diffCount = SlotByteSize.Sub(b.Length);
            var longDataBytes = new byte[SlotByteSize];
            byte c = 0;
            if (data < 0)
            {
                c = 0xff;
            }

            for (var j = 0; j < diffCount; j++)
            {
                longDataBytes[j] = c;
            }
           
            Buffer.BlockCopy(b, 0, longDataBytes, diffCount, b.Length);
            return longDataBytes.ToHex();
        }

        public string ConvertArray(string dataType, IEnumerable<long> dataList)
        {
            if (dataType != Int192)
                return string.Empty;
            var dataBytes = new StringBuilder();
            foreach (var data in dataList)
            {
                dataBytes.Append(serizalization[dataType](data));
            }

            return dataBytes.ToString();
        }

        public string ConvertBytes32(object data)
        {
            var dataBytes = data as byte[];
            if (dataBytes.Length != SlotByteSize)
            {
                throw new AssertionException("invalid bytes32 data");
            }

            return dataBytes.ToHex();
        }
    }
}