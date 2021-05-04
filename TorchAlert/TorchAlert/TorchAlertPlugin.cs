using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Controls;
using Discord.Torch;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using TorchAlert.Core;
using TorchAlert.Core.Patches;
using Utils.General;
using Utils.Torch;

namespace TorchAlert
{
    public sealed class TorchAlertPlugin : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        Persistent<TorchAlertConfig> _config;
        UserControl _userControl;
        FileLoggingConfigurator _fileLoggingConfigurator;
        CancellationTokenSource _cancellationTokenSource;

        public Core.TorchAlert TorchAlert { get; private set; }
        public TorchAlertConfig Config => _config.Data;
        public UserControl GetControl() => _config.GetOrCreateUserControl(ref _userControl);

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            this.ListenOnGameLoaded(OnGameLoaded);
            this.ListenOnGameUnloading(OnGameUnloading);

            _cancellationTokenSource = new CancellationTokenSource();

            var configPath = this.MakeConfigFilePath();
            _config = Persistent<TorchAlertConfig>.Load(configPath);
            Config.PropertyChanged += OnConfigPropertyChanged;

            _fileLoggingConfigurator = new FileLoggingConfigurator(nameof(TorchAlert), new[] {"TorchAlert.*", "Discord.Net.*"}, Config.LogFilePath);
            _fileLoggingConfigurator.Initialize();
            _fileLoggingConfigurator.Configure(Config);

            var linkDbPath = this.MakeFilePath($"{nameof(DiscordIdentityLinker)}.csv");
            var splitLookup = new ParentsLookupTree<long>();
            TorchAlert = new Core.TorchAlert(Config, linkDbPath, splitLookup);
            MyCubeGridPatch.SplitLookup = splitLookup;

            Log.Info("initialized");
        }

        async void OnConfigPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            try
            {
                _fileLoggingConfigurator.Configure(Config);

                if (Config.Enable)
                {
                    if (args.PropertyName == nameof(Config.Enable) ||
                        args.PropertyName == nameof(Config.Token))
                    {
                        await TorchAlert.InitializeDiscordAsync();
                    }

                    if (args.PropertyName == nameof(Config.EnableGameText) ||
                        args.PropertyName == nameof(Config.GameText))
                    {
                        await TorchAlert.UpdateGameTextAsync();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warn(e, "failed applying config changes");
            }
        }

        void OnGameLoaded()
        {
            var chatManager = Torch.CurrentSession.Managers.GetManager<IChatManagerServer>();
            TorchAlert.Initialize(chatManager);
            TaskUtils.RunUntilCancelledAsync(TorchAlert.Main, _cancellationTokenSource.Token).Forget(Log);
        }

        void OnGameUnloading()
        {
            TorchAlert.Close();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            Config.PropertyChanged -= OnConfigPropertyChanged;
            _config.Dispose();
        }
    }
}