using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public class ReportGenerator
        {
            public const string ArraySuffix = "[]";
            public const string Bytes32 = "bytes32";
            public const string Bytes32Array = Bytes32 + ArraySuffix;
            public const string Uint256 = "uint256";
            public const int SlotByteSize = 32;
            public const int DigestFixedLength = 16;

            private readonly Dictionary<string, Func<object, IList<byte>>> _serialization;

            public ReportGenerator()
            {
                _serialization = new Dictionary<string, Func<object, IList<byte>>>
                {
                    [Bytes32] = ConvertBytes32, [Uint256] = ConvertLong
                };
            }

            public string GenerateEthereumReportWithMultipleData(ByteString configDigest, Report report)
            {
                var data = new object[3];
                if (configDigest.Length != DigestFixedLength)
                {
                    throw new AssertionException("invalid config digest");
                }
                data[0] = GenerateConfigText(configDigest, report);
                data[1] = GenerateObserverIndex(report);
                data[2] = report.AggregatedData;
                return SerializeReport(data, Bytes32, Bytes32, Bytes32Array).ToArray().ToHex();
            }

            public string GenerateEthereumReport(ByteString configDigest, Report report)
            {
                var data = new object[3];
                if (configDigest.Length != DigestFixedLength)
                {
                    throw new AssertionException("invalid config digest");
                }
                data[0] = GenerateConfigText(configDigest, report);
                data[1] = GenerateObserverIndex(report);
                var aggregatedData = report.AggregatedData;
                if(aggregatedData.Length > SlotByteSize)
                {
                    throw new AssertionException("aggregated data is oversize(32 bytes)");
                }
                data[2] = GenerateObservation(report.AggregatedData);
                return SerializeReport(data, Bytes32, Bytes32, Bytes32).ToArray().ToHex();
            }

            private IList<byte> GenerateConfigText(ByteString configDigest, Report report)
            {
                long round = report.RoundId;
                byte observerCount = (byte) report.Observations.Value.Count;
                byte validBytesCount = (byte) report.AggregatedData.ToByteArray().Length;
                if (round < 0)
                {
                    throw new AssertionException("invalid round");
                }

                var configText = GetByteListWithCount(SlotByteSize);
                var digestBytes = configDigest.ToByteArray();
                BytesCopy(digestBytes, 0, configText, 6, 16);
                var roundBytes = round.ToBytes();
                BytesCopy(roundBytes, 0, configText, 22, 8);
                configText[SlotByteSize.Sub(2)] = observerCount;
                configText[SlotByteSize.Sub(1)] = validBytesCount;
                return configText;
            }

            private IList<byte> GenerateObservation(ByteString result)
            {
                var observation = result.ToByteArray();
                if (observation.Length == SlotByteSize)
                    return observation;
                var ret = GetByteListWithCount(SlotByteSize);
                BytesCopy(observation, 0, ret, 0, observation.Length);
                return ret;
            }

            private IList<byte> GenerateObserverIndex(Report report)
            {
                var observations = report.Observations.Value;
                var observerIndex = GetByteListWithCount(SlotByteSize);
                for (var i = 0; i < observations.Count; i++)
                {
                    observerIndex[i] = (byte) int.Parse(observations[i].Key);
                }

                return observerIndex;
            }

            private IList<byte> SerializeReport(object[] data, params string[] dataType)
            {
                var dataLength = (long) dataType.Length;
                if (dataLength != data.Length)
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
                        var bytes32Count = bytesArray.Length.Div(SlotByteSize).Add(1);
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

            private IList<byte> ConvertLong(object i)
            {
                var data = (long) i;
                var b = data.ToBytes();
                if (b.Length == SlotByteSize)
                    return b;
                var diffCount = SlotByteSize.Sub(b.Length);
                var longDataBytes = GetByteListWithCount(SlotByteSize);
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

            private IList<byte> ConvertLongArray(string dataType, IEnumerable<long> dataList)
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

            private IList<byte> ConvertBytes32Array(IList<byte> data, int dataSize)
            {
                var target = GetByteListWithCount(dataSize);
                BytesCopy(data, 0, target, 0, data.Count);
                return target;
            }

            private IList<byte> ConvertBytes32(object data)
            {
                var dataBytes = data as IList<byte>;
                if (dataBytes.Count != SlotByteSize)
                {
                    throw new AssertionException("invalid bytes32 data");
                }

                return dataBytes;
            }

            private void BytesCopy(IList<byte> src, int srcOffset, IList<byte> dst, int dstOffset, int count)
            {
                for (var i = srcOffset; i < srcOffset + count; i++)
                {
                    dst[dstOffset] = src[i];
                    dstOffset++;
                }
            }

            private List<byte> GetByteListWithCount(int count)
            {
                var list = new List<byte>();
                list.AddRange(Enumerable.Repeat((byte) 0, count));
                return list;
            }
        }
    }
}