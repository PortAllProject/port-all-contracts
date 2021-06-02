using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public const string ArraySuffix = "[]";
        public const string Bytes32 = "bytes32";
        public const string Bytes32Array = Bytes32 + ArraySuffix;
        public const string Uint256 = "uint256";
        public const int SlotByteSize = 32;
        public const int DigestFixedLength = 16;

        private string GenerateRawReport(ByteString configDigest, Address organization, Report report)
        {
            var data = new List<object>();
            Assert(configDigest.Length == DigestFixedLength, "invalid config digest");
            var config = GenerateConfigText(configDigest, report);
            var totalObserverCount =
                GenerateObserverIndex(organization, report, out var observerIndex, out var observationsCount);
            config[SlotByteSize.Sub(2)] = (byte) totalObserverCount;
            data.Add(config);
            data.Add(observerIndex);
            data.Add(observationsCount);
            data.Add(FillObservationBytes(report.AggregatedData.ToByteArray()));
            GenerateMultipleObservation(report, out var observerOrder, out var observationsLength,
                out var observations);
            data.Add(observerOrder);
            data.Add(observationsLength);
            data.Add(observations);
            // bytes32: config digest
            // bytes32: observer index in ethereum contract
            // bytes32: the observation count of each observer
            // bytes32: the aggregated data or merkel tree root
            // bytes32: the index of chainInfo
            // bytes32: the answer's length of each chainInfo
            // bytes32[]: the concrete answer
            return SerializeReport(data, Bytes32, Bytes32, Bytes32, Bytes32Array, Bytes32, Bytes32, Bytes32Array).ToArray()
                .ToHex();
        }

        private IList<byte> GenerateConfigText(ByteString configDigest, Report report)
        {
            long round = report.RoundId;
            byte validBytesCount = (byte) report.AggregatedData.Length;
            if (round < 0)
            {
                throw new AssertionException("invalid round");
            }

            // configText consists of:
            // 6-byte zero padding
            // 16-byte configDigest
            // 8-byte round id
            // 1-byte observer count
            // 1-byte valid byte count (aggregated answer)
            var configText = GetByteListWithCapacity(SlotByteSize);
            var digestBytes = configDigest.ToByteArray();
            BytesCopy(digestBytes, 0, configText, 6, 16);
            var roundBytes = round.ToBytes();
            BytesCopy(roundBytes, 0, configText, 22, 8);
            configText[SlotByteSize.Sub(1)] = validBytesCount;
            return configText;
        }

        private IList<byte> FillObservationBytes(byte[] result)
        {
            if (result.Length == 0)
                return GetByteListWithCapacity(SlotByteSize);
            var totalBytesLength = result.Length.Sub(1).Div(SlotByteSize).Add(1);
            var ret = GetByteListWithCapacity(totalBytesLength.Mul(SlotByteSize));
            BytesCopy(result, 0, ret, 0, result.Length);
            return ret;
        }

        private int GenerateObserverIndex(Address regimentAssociationAddress, Report report, out List<byte> observerIndex,
            out List<byte> observationsCount)
        {
            var observations = report.Observers.Any()
                ? report.Observers.SelectMany(x => x.Value)
                : report.Observations.Value.Select(x => Address.FromBytes(ByteArrayHelper.HexStringToByteArray(x.Key)));

            var groupObservation = observations.GroupBy(x => x).ToList();
            observerIndex = GetByteListWithCapacity(SlotByteSize);
            observationsCount = GetByteListWithCapacity(SlotByteSize);
            IList<Address> memberList = State.ObserverListMap[regimentAssociationAddress].Value;

            var i = 0;
            foreach (var gp in groupObservation)
            {
                observerIndex[i] = (byte) memberList.IndexOf(gp.Key);
                observationsCount[i] = (byte) gp.Count();
                i++;
            }

            return groupObservation.Count();
        }

        private void GenerateMultipleObservation(Report report, out List<byte> observerOrder,
            out List<byte> observationsLength,
            out List<byte> observations)
        {
            observerOrder = GetByteListWithCapacity(SlotByteSize);
            observationsLength = GetByteListWithCapacity(SlotByteSize);
            observations = new List<byte>();
            if (report.Observations.Value.Any() && !int.TryParse(report.Observations.Value[0].Key, out _))
            {
                return;
            }

            int i = 0;
            foreach (var observation in report.Observations.Value)
            {
                Assert(int.TryParse(observation.Key, out var order), $"invalid observation key : {observation.Key}");
                observerOrder[i] = (byte) order;
                observation.Data = observation.Data;
                observationsLength[i] = (byte) observation.Data.Length;
                observations.AddRange(FillObservationBytes(observation.Data.GetBytes()));
                i++;
            }
        }

        private IList<byte> SerializeReport(IList<object> data, params string[] dataType)
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

                    var bytesList = data[i] as List<byte>;
                    Assert(bytesList != null, "invalid observations");
                    var bytes32Count = bytesList.Count % SlotByteSize == 0
                        ? bytesList.Count.Div(SlotByteSize)
                        : bytesList.Count.Div(SlotByteSize).Add(1);
                    dataPosition = currentIndex.Mul(SlotByteSize);
                    result.AddRange(ConvertLong(dataPosition));
                    currentIndex = currentIndex.Add(bytes32Count).Add(1);
                    lazyData.AddRange(ConvertLong(bytes32Count));
                    lazyData.AddRange(ConvertBytes32Array(bytesList, bytes32Count.Mul(SlotByteSize)));
                    continue;
                }

                if (dataType[i] == Bytes32)
                {
                    result.AddRange(ConvertBytes32(data[i]));
                }
                else if (dataType[i] == Uint256)
                {
                    result.AddRange(ConvertLong((long) data[i]));
                }
            }

            result.AddRange(lazyData);
            return result;
        }

        private IList<byte> ConvertLong(long data)
        {
            var b = data.ToBytes();
            if (b.Length == SlotByteSize)
                return b;
            var diffCount = SlotByteSize.Sub(b.Length);
            var longDataBytes = GetByteListWithCapacity(SlotByteSize);
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
                dataBytes.AddRange(ConvertLong(data));
            }

            return dataBytes;
        }

        private IList<byte> ConvertBytes32Array(IList<byte> data, int dataSize)
        {
            if (dataSize == 0)
            {
                return new List<byte>();
            }

            var target = GetByteListWithCapacity(dataSize);
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

        private List<byte> GetByteListWithCapacity(int count)
        {
            var list = new List<byte>();
            list.AddRange(Enumerable.Repeat((byte) 0, count));
            return list;
        }

        private long GetAmercementAmount(Address associationAddress = null)
        {
            return associationAddress == null
                ? MinimumAmercementAmount
                : Math.Max(State.AmercementAmountMap[associationAddress], MinimumAmercementAmount);
        }
    }
}