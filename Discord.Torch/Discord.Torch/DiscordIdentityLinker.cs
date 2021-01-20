using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NLog;
using Sandbox.Game.World;

namespace Discord.Torch
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
            _nextLinkId = new Random().Next(0, short.MaxValue / 2);
        }

        public bool TryGetSteamId(ulong discordId, out ulong steamId)
        {
            return _db.TryGetSteamId(discordId, out steamId);
        }

        public bool TryGetDiscordId(ulong steamId, out ulong discordId)
        {
            return _db.TryGetDiscordId(steamId, out discordId);
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
                _db.Update(steamId, discordId);
                _db.Write();
                _linkIds.Remove(linkId);
                return true;
            }

            return false;
        }
    }
}