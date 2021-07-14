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
        readonly Dictionary<long, ulong> _allPlayerIds; // identity id -> steam id
        readonly Dictionary<long, HashSet<ulong>> _cachedAlertableSteamIds;

        public AlertableSteamIdExtractor(IConfig config, DiscordIdentityLinkDb links)
        {
            _config = config;
            _links = links;
            _allPlayerIds = new Dictionary<long, ulong>();
            _cachedAlertableSteamIds = new Dictionary<long, HashSet<ulong>>();
        }

        public void Update()
        {
            _allPlayerIds.Clear();
            _cachedAlertableSteamIds.Clear();

            var newPlayerIds = MySession.Static.Players
                .GetAllPlayers()
                .Select(p => p.SteamId)
                .Select(s => (MySession.Static.Players.TryGetIdentityId(s), s))
                .ToDictionary();

            _allPlayerIds.AddRange(newPlayerIds);
        }

        public IEnumerable<ulong> GetAlertableSteamIds(long ownerId)
        {
            if (_cachedAlertableSteamIds.TryGetValue(ownerId, out var alertableSteamIds))
            {
                return alertableSteamIds;
            }

            alertableSteamIds = new HashSet<ulong>();

            var steamIds = new List<ulong>();

            if (!_allPlayerIds.TryGetValue(ownerId, out var ownerSteamId))
            {
                return Enumerable.Empty<ulong>();
            }

            steamIds.Add(ownerSteamId);

            var friendSteamIds = GetFriendSteamIds(ownerId);
            steamIds.AddRange(friendSteamIds);

            Log.Trace($"alertable steam ids: {steamIds.ToStringSeq()}");

            foreach (var steamId in steamIds)
            {
                Log.Trace($"checking mute: {steamId}");
                if (_config.IsMuted(steamId)) continue;

                Log.Trace($"checking discord link: {steamId}");
                if (!_links.HasDiscordLink(steamId)) continue;

                Log.Trace($"alertable steam ID: {steamId}");
                alertableSteamIds.Add(steamId);
            }

            _cachedAlertableSteamIds[ownerId] = alertableSteamIds;
            return alertableSteamIds;
        }

        IEnumerable<ulong> GetFriendSteamIds(long playerId)
        {
            if (!MySession.Static.Factions.TryGetFactionByPlayerId(playerId, out var faction)) yield break;

            foreach (var (_, member) in faction.Members)
            {
                var memberId = member.PlayerId;
                if (!MySession.Static.Players.TryGetSteamId(memberId, out var memberSteamId)) continue;

                yield return memberSteamId;
            }
        }
    }
}