using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRageMath;

namespace TorchAlarm
{
    [Category("da")]
    public sealed class TorchAlarmCommandModule : CommandModule
    {
        TorchAlarmPlugin Plugin => (TorchAlarmPlugin) Context.Plugin;

        [Command("link")]
        [Permission(MyPromoteLevel.None)]
        public void Link()
        {
            if (Context.Player == null)
            {
                Context.Respond("Must be called by a player", Color.Red);
                return;
            }

            var steamId = Context.Player.SteamUserId;
            var linkId = Plugin.GenerateLinkId(steamId);
            Context.Respond($"Write this code to the Discord bot: {linkId}");
        }

        [Command("mute")]
        [Permission(MyPromoteLevel.None)]
        public void Mute()
        {
            if (Context.Player == null)
            {
                Context.Respond("Must be called by a player", Color.Red);
                return;
            }

            Plugin.Config.Mute(Context.Player.SteamUserId);
            Context.Respond("Muted alarms");
        }

        [Command("unmute")]
        [Permission(MyPromoteLevel.None)]
        public void Unmute()
        {
            if (Context.Player == null)
            {
                Context.Respond("Must be called by a player", Color.Red);
                return;
            }

            Plugin.Config.Unmute(Context.Player.SteamUserId);
            Context.Respond("Unmuted alarms");
        }
    }
}