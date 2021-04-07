﻿using System;
using System.Collections.Generic;
using System.Linq;
using AElf;
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
        private static string privateKey = "0x7f6965ae260469425ae839f5abc85b504883022140d5f6fc9664a96d480c068d";
        static void Main(string[] args)
        {
            // var merkelTreeRoot = HashHelper.ComputeFrom("test");
            // Console.WriteLine(merkelTreeRoot.ToHex());
            //TestWithMultipleObservations();
            //TestWithSingleOneObservation();
            // var hexStr = "0x0a0431203a34";
            // var recoverStrBytes = ByteStringHelper.FromHexString(hexStr);
            // var recoverStr = StringValue.Parser.ParseFrom(recoverStrBytes);
            // Console.WriteLine(recoverStr.Value);

            var signService = new SignService();
            var privateKeyBytes = ByteStringHelper.FromHexString(privateKey).ToByteArray();
            var report =
                "0x000000000000f6f3ed664fd0e7be332f035ec351acf1000000000000000a0007000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f0a056173646173000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000e00000000000000000000000000000000000000000000000000000000000000000";
            var hashMsg = SignService.GetKeccak256(report);
            Console.WriteLine(hashMsg);
            signService.Sign(report, privateKeyBytes, out var r, out var s, out var v);
            Console.WriteLine("r is : " + r);
            Console.WriteLine("s is : " + s);
            Console.WriteLine("v is : " + v);
        }

        static void TestWithMultipleObservations()
        {
            var rs = new ReportService();
            var digestStr = "0x22d6f8928689ea183a3eb24df3919a94";
            var digestBytes = ByteStringHelper.FromHexString(digestStr).ToByteArray();
            //Console.WriteLine("digest length :" + digestBytes.Length);
            var digest = ByteString.CopyFrom(digestBytes);
            var merkelTreeRoot = HashHelper.ComputeFrom("test");
            var report = new Report
            {
                RoundId = 11, AggregatedData = merkelTreeRoot.Value, Observations = new Observations()
            };
            report.Observations.Value.Add(new Observation
            {
                Key = "0",
                Data = new StringValue
                {
                    Value = "1 :4"
                }.ToByteString()
            });
            report.Observations.Value.Add(new Observation
            {
                Key = "1",
                Data = new StringValue
                {
                    Value = "1 :5"
                }.ToByteString()
            });
            report.Observations.Value.Add(new Observation
            {
                Key = "2",
                Data = new StringValue
                {
                    Value = "1 :6"
                }.ToByteString()
            });
            var bytesInEthereum = rs.GenerateEthereumReport(digest, report);
            Console.WriteLine("0x" + bytesInEthereum);
        }

        static void TestWithSingleOneObservation()
        {
            var rs = new ReportService();
            var digestStr = "0xf6f3ed664fd0e7be332f035ec351acf1";
            var digestBytes = ByteStringHelper.FromHexString(digestStr).ToByteArray();
            //Console.WriteLine("digest length :" + digestBytes.Length);
            var digest = ByteString.CopyFrom(digestBytes);
            var data = new StringValue()
            {
                Value = "asdas"
            };
            var report = new Report
            {
                RoundId = 10, AggregatedData = data.ToByteString(), Observations = new Observations()
            };
            var bytesInEthereum = rs.GenerateEthereumReport(digest, report);
            Console.WriteLine("0x" + bytesInEthereum);
        }
    }

    public interface IReportService
    {
        string GenerateEthereumReport(ByteString configDigest, Report report);
    }

    public class ReportService : IReportService
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

        // public string GenerateEthereumReportWithMultipleData(ByteString configDigest, Report report)
        // {
        //     var data = new object[3];
        //     data[0] = GenerateConfigText(configDigest, report);
        //     data[1] = GenerateObserverIndex(report);
        //     data[2] = report.AggregatedData;
        //     return SerializeReport(data, Bytes32, Bytes32, Bytes32Array).ToArray().ToHex();
        // }
        public string GenerateEthereumReport(ByteString configDigest, Report report)
        {
            var data = new List<object>();
            data.Add(GenerateConfigText(configDigest, report));
            GenerateObserverIndex(32, out var observerIndex, out var observationsCount);
            data.Add(observerIndex);
            data.Add(observationsCount);
            data.Add(FillObservationBytes(report.AggregatedData));
            GenerateMultipleObservation(report, out var observerOrder, out var observationLength, out var observations);
            data.Add(observerOrder);
            data.Add(observationLength);
            data.Add(observations);
            return SerializeReport(data, Bytes32, Bytes32, Bytes32, Bytes32, Bytes32, Bytes32, Bytes32Array).ToArray().ToHex();
        }

        private void GenerateMultipleObservation(Report report, out byte[] observerOrder, out byte[] observationLength, 
            out byte[] observations)
        {
            observerOrder = new byte[SlotByteSize];
            observationLength = new byte[SlotByteSize];
            observations = new byte[report.Observations.Value.Count * SlotByteSize];
            int i = 0;
            int offset = 0;
            foreach (var observation in report.Observations.Value)
            {
                observerOrder[i] = (byte) int.Parse(observation.Key);
                var observationArray = observation.Data.ToByteArray();
                observationLength[i] = (byte)observationArray.Length;
                BytesCopy(observationArray, 0, observations, offset, observationArray.Length);
                i++;
                offset += SlotByteSize;
            }
        }

        public byte[] GenerateConfigText(ByteString configDigest, Report report)
        {
            long round = report.RoundId;
            byte observerCount = (byte) report.Observations.Value.Count;
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

        public byte[] FillObservationBytes(ByteString result)
        {
            var observation = result.ToByteArray();
            if (observation.Length == SlotByteSize)
                return observation;
            var ret = new byte[SlotByteSize];
            BytesCopy(observation, 0, ret, 0, observation.Length);
            return ret;
        }

        private void GenerateObserverIndex(int indexCount, out byte[] observerIndex,
            out byte[] observationsCount)
        {
            observerIndex = new byte[SlotByteSize];
            observationsCount = new byte[SlotByteSize];
            int i = 0;
            while (i < indexCount)
            {
                observerIndex[i] = (byte) i;
                observationsCount[i] = (byte) i;
                i++;
            }
        }

        public IList<byte> SerializeReport(List<object> data, params string[] dataType)
        {
            var dataLength = (long) dataType.Length;
            if (dataLength != data.Count)
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

                    var bytesArray = data[i] as byte[];
                    long bytes32Count = bytesArray.Length % SlotByteSize == 0
                        ? bytesArray.Length.Div(SlotByteSize)
                        : bytesArray.Length.Div(SlotByteSize).Add(1);
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
                dstOffset++;
            }

            //Buffer.BlockCopy(src, srcOffset, dst, dstOffset, count);
        }

        private int GetIndex(Address key)
        {
            return 1;
        }
    }
}