using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf;
using AElf.Contracts.Report;
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
            //Test1();
            //TestWithMultipleData();
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
            var data = new StringValue()
            {
                Value = "asdas"
            };
            var report = new Report
            {
                RoundId = 10, AggregatedData = data.ToByteString(), Observations = new Observations()
            };
            report.Observations.Value.Add(new Observation
            {
                Key = "0",
                Data = ByteString.Empty
            });
            report.Observations.Value.Add(new Observation
            {
                Key = "4",
                Data = ByteString.Empty
            });
            report.Observations.Value.Add(new Observation
            {
                Key = "10",
                Data = ByteString.Empty
            });
            var bytesInEthereum = rs.GenerateEthereumReport(digest, report);
            Console.WriteLine(bytesInEthereum);
        }
        static void TestWithMultipleData()
        {
            var rs = new ReportService();
            var digestBytes = new byte[16];
            for (var i = 0; i < digestBytes.Length; i++)
            {
                digestBytes[i] = 7;
            }
            var digest = ByteString.CopyFrom(digestBytes);
            var data = new StringValue()
            {
                Value = "asdasasdasdd萨卡是咖啡吧是咖啡吧康师傅"
            };
            var report = new Report
            {
                RoundId = 10, AggregatedData = data.ToByteString(), Observations = new Observations()
            };
            report.Observations.Value.Add(new Observation
            {
                Key = "0",
                Data = ByteString.Empty
            });
            report.Observations.Value.Add(new Observation
            {
                Key = "4",
                Data = ByteString.Empty
            });
            report.Observations.Value.Add(new Observation
            {
                Key = "10",
                Data = ByteString.Empty
            });
            var bytesInEthereum = rs.GenerateEthereumReportWithMultipleData(digest, report);
            Console.WriteLine(bytesInEthereum);
        }
    }

    public interface IReportService
    {
        string GenerateEthereumReport(ByteString configDigest, Report report);
    }
    
    public class ReportService: IReportService
    {
        public const string ArraySuffix = "[]";
        public const string Bytes32 = "bytes32";
        public const string Bytes32Array = Bytes32 + ArraySuffix;
        public const string Uint256 = "uint256";
        public const int SlotByteSize = 32;

        private readonly Dictionary<string, Func<object, string>> _serialization;

        public ReportService()
        {
            _serialization = new Dictionary<string, Func<object, string>>
            {
                [Bytes32] = ConvertBytes32, [Uint256] = ConvertLong
            };
        }
        public string GenerateEthereumReportWithMultipleData(ByteString configDigest, Report report)
        {
            var data = new object[3];
            data[0] = GenerateConfigText(configDigest, report);
            data[1] = GenerateObserverIndex(report);
            data[2] = report.AggregatedData;
            return SerializeReport(data, Bytes32, Bytes32, Bytes32Array);
        }
        public string GenerateEthereumReport(ByteString configDigest, Report report)
        {
            var data = new object[3];
            data[0] = GenerateConfigText(configDigest, report);
            data[1] = GenerateObserverIndex(report);
            Console.WriteLine(ConvertBytes32(data[1]));
            data[2] = GenerateObservation(report.AggregatedData);
            return SerializeReport(data, Bytes32, Bytes32, Bytes32);
        }
        public byte[] GenerateConfigText(ByteString configDigest, Report report)
        {
            long round = report.RoundId;
            byte observerCount = (byte)report.Observations.Value.Count;
            byte validBytesCount = (byte) report.AggregatedData.ToByteArray().Length;
            if (round < 0)
            {
                throw new AssertionException("invalid round");
            }
            var configText = new byte[SlotByteSize];
            var digestBytes = configDigest.ToByteArray();
            Buffer.BlockCopy(digestBytes, 0, configText, 6, 16);
            var roundBytes = round.ToBytes();
            Buffer.BlockCopy(roundBytes, 0, configText, 22, 8);
            configText[SlotByteSize - 2] = observerCount;
            configText[SlotByteSize - 1] = validBytesCount;
            return configText;
        }

        public byte[] GenerateObservation(ByteString result)
        {
            var observation = result.ToByteArray();
            if (observation.Length == SlotByteSize)
                return observation;
            var ret = new byte[SlotByteSize];
            Buffer.BlockCopy(observation, 0, ret, 0, observation.Length);
            return ret;
        }

        public byte[] GenerateObserverIndex(Report report)
        {
            var observations = report.Observations.Value;
            var observerIndex = new byte[SlotByteSize];
            for (var i = 0; i < observations.Count; i++)
            {
                observerIndex[i] =  (byte)int.Parse(observations[i].Key);
            }
            return observerIndex;
        }
        
        public string SerializeReport(object[] data, params string[] dataType)
        {
            var dataLength = (long)dataType.Length;
            if(dataLength != data.Length)
                throw new AssertionException("invalid data length");
            var result = new StringBuilder("0x");
            long currentIndex = dataLength;
            var lazyData = new StringBuilder();
            for (int i = 0; i < dataLength; i++)
            {
                var typeStrLen = dataType[i].Length;
                if (string.CompareOrdinal(dataType[i].Substring(typeStrLen.Sub(2), 2), ArraySuffix) == 0)
                {
                    var typePrefix = dataType[i].Substring(0, typeStrLen.Sub(2));
                    long dataPosition;
                    if (data[i] is IEnumerable<long> dataList)
                    {
                        long arrayLength = dataList.Count();
                        dataPosition = currentIndex.Mul(SlotByteSize);
                        result.Append(ConvertLong(dataPosition));
                        currentIndex = currentIndex.Add(arrayLength).Add(1);
                        lazyData.Append(ConvertLong(arrayLength));
                        lazyData.Append(ConvertLongArray(typePrefix, dataList));
                        continue;
                    }

                    var bytesArray = (data[i] as ByteString).ToByteArray();
                    var bytes32Count = (long)(bytesArray.Length / SlotByteSize + 1);
                    dataPosition = currentIndex.Mul(SlotByteSize);
                    result.Append(ConvertLong(dataPosition));
                    currentIndex = currentIndex.Add(bytes32Count).Add(1);
                    lazyData.Append(ConvertLong(bytes32Count));
                    lazyData.Append(ConvertBytes32Array(bytesArray, bytes32Count * 32));
                    continue;
                }
                result.Append(_serialization[dataType[i]](data[i]));
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

        public string ConvertLongArray(string dataType, IEnumerable<long> dataList)
        {
            if (dataType != Uint256)
                return string.Empty;
            var dataBytes = new StringBuilder();
            foreach (var data in dataList)
            {
                dataBytes.Append(_serialization[dataType](data));
            }

            return dataBytes.ToString();
        }

        public string ConvertBytes32Array(byte[] data, long dataSize)
        {
            var target = new byte[dataSize];
            Buffer.BlockCopy(data, 0, target, 0, data.Length);
            return target.ToHex();
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