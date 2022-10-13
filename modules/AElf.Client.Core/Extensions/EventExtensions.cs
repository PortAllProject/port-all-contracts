using Google.Protobuf;

namespace AElf.Client.Core.Extensions;

public static class EventExtensions
{
    public static void MergeFrom<T>(this T eventData, LogEvent log) where T : IMessage<T>
    {
        foreach (var bs in log.Indexed) eventData.MergeFrom(bs);
        eventData.MergeFrom(log.NonIndexed);
    }
}