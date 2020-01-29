using Pahkat.Sdk;

namespace Pahkat.Extensions
{
    public static class RepositoryChannelExt
    {
        public static string ToLocalisedName(this RepositoryMeta.Channel channel)
        {
            switch (channel)
            {
                case RepositoryMeta.Channel.Alpha:
                    return Strings.Alpha;
                case RepositoryMeta.Channel.Beta:
                    return Strings.Beta;
                case RepositoryMeta.Channel.Stable:
                    return Strings.Stable;
                case RepositoryMeta.Channel.Nightly:
                    return Strings.Nightly;
                default:
                    return channel.Value() ?? channel.ToString();
            }
        }
    }
}