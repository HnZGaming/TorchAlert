using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NLog;
using Sandbox.Game.World;
using Utils.General;

namespace TorchAlarm.Discord
{
    public sealed class DiscordIdentityLinker
    {
        const string DbTableName = "links";
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly StupidDb _db;
        readonly Dictionary<int, ulong> _linkIds;
        int _nextLinkId;

        public DiscordIdentityLinker(StupidDb db)
        {
            _db = db;
            _linkIds = new Dictionary<int, ulong>();
            _nextLinkId = new Random().Next(0, int.MaxValue / 2);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool TryGetLinkedSteamUser(ulong discordId, out ulong steamId)
        {
            if (TryGetLinkByDiscordId(discordId, out var link))
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
            if (TryGetLinkBySteamId(steamId, out var link))
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
                MakeLink(steamId, discordId);
                return true;
            }

            return false;
        }

        void MakeLink(ulong steamId, ulong discordId)
        {
            if (!TryGetLinkBySteamId(steamId, out var link))
            {
                link = new DiscordIdentityLink();
            }

            link.SteamId = steamId;
            link.DiscordId = discordId;

            _db.Insert(DbTableName, new[] {link});

            Log.Info($"Made link: {link}");
        }

        bool TryGetLinkBySteamId(ulong steamId, out DiscordIdentityLink link)
        {
            var links = _db.Query<DiscordIdentityLink>(DbTableName);
            foreach (var lk in links)
            {
                if (lk.SteamId == steamId)
                {
                    link = lk;
                    return true;
                }
            }

            link = null;
            return false;
        }

        bool TryGetLinkByDiscordId(ulong discordId, out DiscordIdentityLink link)
        {
            var links = _db.Query<DiscordIdentityLink>(DbTableName);
            foreach (var lk in links)
            {
                if (lk.DiscordId == discordId)
                {
                    link = lk;
                    return true;
                }
            }

            link = null;
            return false;
        }
    }
}