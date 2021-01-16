using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Utils.Torch;

namespace TorchDiscordAlarm
{
    public sealed class DiscordAlarmPlugin : TorchPluginBase, IWpfPlugin
    {
        Persistent<DiscordAlarmConfig> _config;
        UserControl _userControl;

        DiscordAlarmConfig Config => _config.Data;

        public UserControl GetControl()
        {
            return _config.GetOrCreateUserControl(ref _userControl);
        }

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            var configPath = this.MakeConfigFilePath();
            _config = Persistent<DiscordAlarmConfig>.Load(configPath);
        }
    }
}