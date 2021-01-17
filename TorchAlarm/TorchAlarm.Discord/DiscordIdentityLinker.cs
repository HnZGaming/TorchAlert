using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NLog;
using Sandbox.Game.World;

namespace TorchAlarm.Discord
{
    public sealed class DiscordIdentityLinker
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly DiscordIdentityLinkDb _db;
        readonly Dictionary<int, ulong> _linkIds;
        int _nextLinkId;

        public DiscordIdentityLinker(DiscordIdentityLinkDb db)
        {
            _db = db;
            _linkIds = new Dictionary<int, ulong>();
            _nextLinkId = new Random().Next(0, int.MaxValue / 2);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryGetLinkedSteamUser(ulong discordId, out ulong steamId)
        {
            if (_db.TryGetLinkByDiscordId(discordId, out var link))
            {
                steamId = link.SteamId;
                return true;
            }

            steamId = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryGetLinkedDiscordId(ulong steamId, out ulong discordId)
        {
            if (_db.TryGetLinkBySteamId(steamId, out var link))
            {
                discordId = link.DiscordId;
                return true;
            }

            discordId = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int GenerateLinkId(ulong steamId)
        {
            var linkId = _nextLinkId++;
            _linkIds[linkId] = steamId;

            var playerName = MySession.Static?.Players?.TryGetIdentityNameFromSteamId(steamId);
            Log.Info($"generated link ID: {linkId} by steam user {playerName ?? "<none>"} ({steamId})");

            return linkId;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryMakeLink(int linkId, ulong discordId, out ulong steamId)
        {
            if (_linkIds.TryGetValue(linkId, out steamId))
            {
                _db.MakeLink(steamId, discordId);
                return true;
            }

            return false;
        }
    }
}