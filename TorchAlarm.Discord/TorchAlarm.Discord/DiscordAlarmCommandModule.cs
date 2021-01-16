using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRageMath;

namespace TorchAlarm.Discord
{
    [Category("da")]
    public sealed class DiscordAlarmCommandModule : CommandModule
    {
        DiscordAlarmPlugin Plugin => (DiscordAlarmPlugin) Context.Plugin;

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
        }
    }
}