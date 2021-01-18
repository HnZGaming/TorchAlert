using System;
using Sandbox.Game.World;
using Torch.Commands;
using Torch.Commands.Permissions;
using Utils.General;
using Utils.Torch;
using VRage.Game.ModAPI;

namespace TorchAlert
{
    [Category("alert")]
    public sealed class TorchAlertCommandModule : CommandModule
    {
        TorchAlertPlugin Plugin => (TorchAlertPlugin) Context.Plugin;

        [Command("link")]
        [Permission(MyPromoteLevel.None)]
        public void Link() => this.CatchAndReport(() =>
        {
            var steamId = GetArgPlayerSteamId();
            var linkId = Plugin.GenerateLinkId(steamId);
            Context.Respond($"Write this code to the Discord bot: {linkId}");
        });

        [Command("mute")]
        [Permission(MyPromoteLevel.None)]
        public void Mute() => this.CatchAndReport(() =>
        {
            var steamId = GetArgPlayerSteamId();
            Plugin.Config.Mute(steamId);
            Context.Respond("Muted alerts");
        });

        [Command("unmute")]
        [Permission(MyPromoteLevel.None)]
        public void Unmute() => this.CatchAndReport(() =>
        {
            var steamId = GetArgPlayerSteamId();
            Plugin.Config.Unmute(steamId);
            Context.Respond("Unmuted alerts");
        });

        ulong GetArgPlayerSteamId()
        {
            if (Context.Player != null)
            {
                return Context.Player.SteamUserId;
            }

            if (Context.Args.TryGetFirst(out var arg))
            {
                if (ulong.TryParse(arg, out var steamId))
                {
                    return steamId;
                }

                var player = MySession.Static.Players.GetPlayerByName(arg);
                if (player == null)
                {
                    throw new Exception("unknown player name");
                }

                return player.SteamId();
            }

            throw new Exception("Must have a player");
        }
    }
}