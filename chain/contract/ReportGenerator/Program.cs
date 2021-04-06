using System;
using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.Oracle;
using AElf.Contracts.Report;
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
            //TestWithMultipleData();
        }

        static void Test1()
        {
            var rs = new ContractReportGenerateService();
            //var rs = new ReportService();
            var digestStr = "0xf6f3ed664fd0e7be332f035ec351acf1";
            var digestBytes = ByteStringHelper.FromHexString(digestStr).ToByteArray();
            Console.WriteLine("digest length :" + digestBytes.Length);
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
            Console.WriteLine("0x" + bytesInEthereum);
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

        private readonly Dictionary<string, Func<object, IList<byte>>> _serialization;

        public ReportService()
        {
            _serialization = new Dictionary<string, Func<object, IList<byte>>>
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
            return SerializeReport(data, Bytes32, Bytes32, Bytes32Array).ToArray().ToHex();
        }
        public string GenerateEthereumReport(ByteString configDigest, Report report)
        {
            var data = new object[3];
            data[0] = GenerateConfigText(configDigest, report);
            data[1] = GenerateObserverIndex(report);
            data[2] = GenerateObservation(report.AggregatedData);
            return SerializeReport(data, Bytes32, Bytes32, Bytes32).ToArray().ToHex();
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
            BytesCopy(digestBytes, 0, configText, 6, 16);
            var roundBytes = round.ToBytes();
            BytesCopy(roundBytes, 0, configText, 22, 8);
            configText[SlotByteSize.Sub(2)] = observerCount;
            configText[SlotByteSize.Sub(1)] = validBytesCount;
            return configText;
        }

        public byte[] GenerateObservation(ByteString result)
        {
            var observation = result.ToByteArray();
            if (observation.Length == SlotByteSize)
                return observation;
            var ret = new byte[SlotByteSize];
            BytesCopy(observation, 0, ret, 0, observation.Length);
            return ret;
        }

        // public byte[] GenerateObserverIndex(Report report)
        // {
        //     var observations = report.Observations.Value;
        //     var observerIndex = new byte[SlotByteSize];
        //     for (var i = 0; i < observations.Count; i++)
        //     {
        //         observerIndex[i] =  (byte)int.Parse(observations[i].Key);
        //     }
        //     return observerIndex;
        // }
        
        private bool GenerateObserverIndex(NodeDataList nodeList, out IList<byte> observerIndex, out IList<byte> observationsCount)
        {
            var groupObservation = nodeList.Value.GroupBy(x => x.Address);
            observerIndex = new byte[SlotByteSize];
            observationsCount = new byte[SlotByteSize];
            var observerCount = 
            foreach (var observerInfo in groupObservation)
            {
                observerIndex = (byte)GetIndex(observerInfo.Key);
            }
            for (var i = 0; i < groupObservation; i++)
            {
                observerIndex[i] =  (byte)int.Parse(observations[i].Key);
            }
            
            
            

            return true;
        }

        public IList<byte> SerializeReport(object[] data, params string[] dataType)
        {
            var dataLength = (long)dataType.Length;
            if(dataLength != data.Length)
                throw new AssertionException("invalid data length");
            var result = new List<byte>();
            long currentIndex = dataLength;
            var lazyData = new List<byte>();
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
                        result.AddRange(ConvertLong(dataPosition));
                        currentIndex = currentIndex.Add(arrayLength).Add(1);
                        lazyData.AddRange(ConvertLong(arrayLength));
                        lazyData.AddRange(ConvertLongArray(typePrefix, dataList));
                        continue;
                    }

                    var bytesArray = (data[i] as ByteString).ToByteArray();
                    var bytes32Count = (long)bytesArray.Length.Div(SlotByteSize).Add(1);
                    dataPosition = currentIndex.Mul(SlotByteSize);
                    result.AddRange(ConvertLong(dataPosition));
                    currentIndex = currentIndex.Add(bytes32Count).Add(1);
                    lazyData.AddRange(ConvertLong(bytes32Count));
                    lazyData.AddRange(ConvertBytes32Array(bytesArray, bytes32Count.Mul(SlotByteSize)));
                    continue;
                }
                result.AddRange(_serialization[dataType[i]](data[i]));
            }

            result.AddRange(lazyData);
            return result;
        }
        public byte[] ConvertLong(object i)
        {
            var data = (long) i;
            var b = data.ToBytes();
            if (b.Length == SlotByteSize)
                return b;
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
           
            BytesCopy(b, 0, longDataBytes, diffCount, b.Length);
            return longDataBytes;
        }

        public IList<byte> ConvertLongArray(string dataType, IEnumerable<long> dataList)
        {
            if (dataType != Uint256)
                return null;
            var dataBytes = new List<byte>();
            foreach (var data in dataList)
            {
                dataBytes.AddRange(_serialization[dataType](data));
            }

            return dataBytes;
        }

        public IList<byte> ConvertBytes32Array(byte[] data, long dataSize)
        {
            var target = new byte[dataSize];
            BytesCopy(data, 0, target, 0, data.Length);
            return target;
        }

        public IList<byte> ConvertBytes32(object data)
        {
            var dataBytes = data as byte[];
            if (dataBytes.Length != SlotByteSize)
            {
                throw new AssertionException("invalid bytes32 data");
            }

            return dataBytes;
        }
        
        private void BytesCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int count)
        {
            for (var i = srcOffset; i < srcOffset + count; i++)
            {
                dst[dstOffset] = src[i];
                dstOffset ++;
            }
            //Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
        }

        private int GetIndex(Address key)
        {
            return 1;
        }
    }
}