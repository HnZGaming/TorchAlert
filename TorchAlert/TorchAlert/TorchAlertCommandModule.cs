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
        TorchAlertConfig Config => Plugin.Config;
        Core.TorchAlert TorchAlert => Plugin.TorchAlert;

        [Command("commands")]
        [Permission(MyPromoteLevel.None)]
        public void Commands() => this.CatchAndReport(() =>
        {
            this.ShowCommands();
        });

        [Command("configs")]
        [Permission(MyPromoteLevel.None)]
        public void Configs() => this.CatchAndReport(() =>
        {
            this.GetOrSetProperty(Config);
        });

        [Command("link")]
        [Permission(MyPromoteLevel.None)]
        public void Link() => this.CatchAndReport(() =>
        {
            var steamId = GetArgPlayerSteamId();
            var linkId = TorchAlert.GenerateLinkId(steamId);
            Context.Respond($"Write this code to the Discord bot: {linkId}");
        });

        [Command("check")]
        [Permission(MyPromoteLevel.None)]
        public void CheckLink() => this.CatchAndReport(async () =>
        {
            var steamId = GetArgPlayerSteamId();
            var playerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
            var (linked, discordName) = await TorchAlert.TryGetLinkedDiscordUserName(steamId);

            var message = linked
                ? $"Player \"{playerName}\" is linked to \"{discordName}\""
                : $"Player \"{playerName}\" is not linked to any Discord user. Try \"!alert link\".";

            Context.Respond(message);

            if (linked)
            {
                await TorchAlert.SendDiscordMessageAsync(steamId, $"You're linked to \"{playerName}\"");
            }
        });

        [Command("mute")]
        [Permission(MyPromoteLevel.None)]
        public void Mute() => this.CatchAndReport(() =>
        {
            var steamId = GetArgPlayerSteamId();
            Config.Mute(steamId);
            Context.Respond("Muted alerts");
        });

        [Command("unmute")]
        [Permission(MyPromoteLevel.None)]
        public void Unmute() => this.CatchAndReport(() =>
        {
            var steamId = GetArgPlayerSteamId();
            Config.Unmute(steamId);
            Context.Respond("Unmuted alerts");
        });

        [Command("mock")]
        [Permission(MyPromoteLevel.Admin)]
        public void SendMockAlert() => this.CatchAndReport(async () =>
        {
            var steamId = GetArgPlayerSteamId();
            await TorchAlert.SendMockAlert(steamId);
            Context.Respond("Sent mock alerts");
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