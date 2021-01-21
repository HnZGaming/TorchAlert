using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.World;
using TorchAlert.Core;
using Utils.General;
using Utils.Torch;

namespace TorchAlert.Damage
{
    public sealed class DamageAlertCreator
    {
        readonly AlertableSteamIdExtractor _steamIdExtractor;

        public DamageAlertCreator(AlertableSteamIdExtractor steamIdExtractor)
        {
            _steamIdExtractor = steamIdExtractor;
        }

        public IEnumerable<DamageAlert> CreateDamageAlerts(IEnumerable<DamageInfo> damageInfos)
        {
            var alerts = new Dictionary<ulong, List<DamageAlert>>();
            foreach (var damageInfo in damageInfos)
            {
                var steamIds = _steamIdExtractor.GetAlertableSteamIds(damageInfo.DefenderId);
                foreach (var steamId in steamIds)
                {
                    var alert = CreateDamageAlert(steamId, damageInfo);
                    alerts.Add(steamId, alert);
                }
            }

            return alerts.Values.SelectMany(v => v);
        }

        static DamageAlert CreateDamageAlert(ulong steamId, DamageInfo damageInfo)
        {
            var gridName = damageInfo.DefenderGridName;
            var isPlayer = MySession.Static.Players.TryGetPlayerById(damageInfo.OffenderId, out var offender);
            var offenderName = isPlayer ? offender.DisplayName : null;
            var hasFaction = MySession.Static.Factions.TryGetPlayerFaction(damageInfo.OffenderId, out var faction);
            var factionTag = hasFaction ? faction.Tag : null;
            return new DamageAlert(steamId, gridName, offenderName, factionTag);
        }
    }
}