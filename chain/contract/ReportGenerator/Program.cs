using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace ReportGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataList = new List<long> {1, 2, 3, 4, 5, 6};
            var rs = new ReportService();
            var a = rs.ConvertArray(ReportService.Int192, dataList);
            
            Console.WriteLine(a);
            Console.WriteLine(a.Length);
        }
    }

    public interface IReportService
    {
        string GenerateEthereumReport();
        string SerializeReport(string dataTypeStr, object[] data);
    }
    
    public class ReportService: IReportService
    {
        public const string ArraySuffix = "[]";
        public const string Bytes32 = "bytes32";
        public const string Int192 = "int192";
        public const int SlotByteSize = 32;
        public const int SlotBitSize = 256;

        private readonly Dictionary<string, Func<object, string>> serizalization;

        public ReportService()
        {
            serizalization = new Dictionary<string, Func<object, string>>
            {
                [Bytes32] = ConvertBytes32, [Int192] = ConvertLong
            };
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
                    long dataPosition = currentIndex.Mul(SlotBitSize);
                    result.Append(ConvertLong(dataPosition));
                    currentIndex = currentIndex.Add(arrayLength);
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
            byte[] b = ((long)i).ToBytes(false).LeftPad(SlotByteSize);
            return b.ToHex();
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

        public string ConvertBytes32(object i)
        {
            var data = i as ByteString;
            return data.ToHex();
        }
    }
}