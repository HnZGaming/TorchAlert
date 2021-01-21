using System.Collections.Generic;
using System.Linq;
using Discord.Torch;
using NLog;
using Sandbox.Game.World;
using Utils.General;
using Utils.Torch;

namespace TorchAlert.Core
{
    public sealed class AlertableSteamIdExtractor
    {
        public interface IConfig
        {
            bool IsMuted(ulong steamId);
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly IConfig _config;
        readonly DiscordIdentityLinkDb _links;

        public AlertableSteamIdExtractor(IConfig config, DiscordIdentityLinkDb links)
        {
            _config = config;
            _links = links;
        }

        public IEnumerable<ulong> GetAlertableSteamIds(long ownerId)
        {
            var steamIds = GetFriendIds(ownerId);
            Log.Trace($"friend IDs: {ownerId} -> {steamIds.ToStringSeq()}");

            foreach (var steamId in steamIds)
            {
                Log.Trace($"checking mute: {steamId}");
                if (_config.IsMuted(steamId)) continue;

                Log.Trace($"checking discord link: {steamId}");
                if (!_links.HasDiscordLink(steamId)) continue;

                Log.Trace($"alertable steam ID: {steamId}");
                yield return steamId;
            }
        }

        static IEnumerable<ulong> GetFriendIds(long playerId)
        {
            if (MySession.Static.Factions.TryGetPlayerFaction(playerId, out var faction))
            {
                foreach (var (_, member) in faction.Members)
                {
                    var memberId = member.PlayerId;
                    if (MySession.Static.Players.TryGetSteamId(memberId, out var memberSteamId))
                    {
                        yield return memberSteamId;
                    }
                }

                yield break;
            }

            if (MySession.Static.Players.TryGetSteamId(playerId, out var steamId))
            {
                yield return steamId;
            }
        }
    }
}