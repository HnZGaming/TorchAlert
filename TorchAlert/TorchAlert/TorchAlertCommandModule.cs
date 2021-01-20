using System;
using Sandbox.Game.World;
using Torch;
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

        [Command("check")]
        [Permission(MyPromoteLevel.None)]
        public void CheckLinked() => this.CatchAndReport(async () =>
        {
            var steamId = GetArgPlayerSteamId();
            var playerName = MySession.Static.Players.TryGetIdentityNameFromSteamId(steamId);
            var (linked, discordName) = await Plugin.TryGetLinkedDiscordUserName(steamId);

            var message = linked
                ? $"Player \"{playerName}\" is linked to \"{discordName}\""
                : $"Player \"{playerName}\" is not linked to any Discord user. Try \"!alert link\".";

            Context.Respond(message);

            if (linked)
            {
                await Plugin.SendDiscordMessageAsync(steamId, $"You're linked to \"{playerName}\"");
            }
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

        [Command("mock")]
        [Permission(MyPromoteLevel.Admin)]
        public void SendMockAlert() => this.CatchAndReport(async () =>
        {
            var steamId = GetArgPlayerSteamId();
            await Plugin.SendMockAlert(steamId);
            Context.Respond("Sent mock alerts");
        });

        [Command("enable")]
        [Permission(MyPromoteLevel.Admin)]
        public void Enable() => this.CatchAndReport(() =>
        {
            Plugin.Config.Enable = true;
        });

        [Command("disable")]
        [Permission(MyPromoteLevel.Admin)]
        public void Disable() => this.CatchAndReport(() =>
        {
            Plugin.Config.Enable = false;
        });

        [Command("token")]
        [Permission(MyPromoteLevel.Admin)]
        public void SetToken(string token) => this.CatchAndReport(() =>
        {
            Plugin.Config.Token = token;
        });

        [Command("scan_interval")]
        [Permission(MyPromoteLevel.Admin)]
        public void SetScanInterval(int scanInterval) => this.CatchAndReport(() =>
        {
            Plugin.Config.ScanInterval = scanInterval;
        });

        [Command("scan_distance")]
        [Permission(MyPromoteLevel.Admin)]
        public void SetScanDistance(int scanDistance) => this.CatchAndReport(() =>
        {
            Plugin.Config.ProximityThreshold = scanDistance;
        });

        [Command("buffer")]
        [Permission(MyPromoteLevel.Admin)]
        public void SetBuffer(int buffer) => this.CatchAndReport(() =>
        {
            Plugin.Config.BufferCount = buffer;
        });

        [Command("format")]
        [Permission(MyPromoteLevel.Admin)]
        public void SetFormat(string format) => this.CatchAndReport(() =>
        {
            Plugin.Config.ProximityAlertFormat = format;
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