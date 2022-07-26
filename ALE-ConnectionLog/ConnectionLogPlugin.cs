using ALE_ConnectionLog.model;
using ALE_ConnectionLog.serialize;
using NLog;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using VRage.GameServices;
using VRage.Steam;

namespace ALE_ConnectionLog {
    public class ConnectionLogPlugin : TorchPluginBase, IWpfPlugin {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string CONFIG_FILE_NAME = "ConnectionLogConfig.cfg";

        private ConnectionLogControl _control;
        public UserControl GetControl() => _control ?? (_control = new ConnectionLogControl(this));

        private IMultiplayerManagerBase multiplayerManagerBase;

        private Persistent<ConnectionLogConfig> _config;

        private ConnectionLogManager _connectionLogManager;
        public ConnectionLogManager ConnectionLogManager => _connectionLogManager;

        public ConnectionLogConfig Config => _config?.Data;
        public ConnectionLog LogEntries;

        private IMyNetworking networking;

        private readonly Stopwatch stopWatch = new Stopwatch();

        public override void Init(ITorchBase torch) {
            base.Init(torch);

            SetupConfig();

            _connectionLogManager = new ConnectionLogManager(StoragePath);
            LogEntries = _connectionLogManager.Init();

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");
        }

        public override void Update() {
            base.Update();

            try {

                /* stopWatch not running? Nothing to do */
                if (!stopWatch.IsRunning)
                    return;

                /* Session not loaded? Nothing to do */
                if (Torch.CurrentSession == null || Torch.CurrentSession.State != TorchSessionState.Loaded)
                    return;

                int intervalMinutes = Config.AutosaveIntervalMinutes;

                /* interval of 0 is disabled */
                if (intervalMinutes == 0)
                    return;

                /* Time not elapsed? Nothing to do */
                var elapsed = stopWatch.Elapsed;
                if (elapsed.TotalMinutes < intervalMinutes)
                    return;

                SaveLogEntriesAsync();

                stopWatch.Restart();

            } catch (Exception e) {
                Log.Error(e, "Could not run Backup");
            }
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state) {

            switch (state) {

                case TorchSessionState.Loaded:

                    multiplayerManagerBase = Torch.CurrentSession.Managers.GetManager<IMultiplayerManagerBase>();

                    stopWatch.Start();

                    if (multiplayerManagerBase != null) {

                        multiplayerManagerBase.PlayerJoined += PlayerJoined;
                        multiplayerManagerBase.PlayerLeft += PlayerLeft;

                    } else {
                        Log.Warn("No multiplayer manager loaded!");
                    }

                    networking = MySteamGameService_Networking.Value;

                    LogEntries.CloseMemorizedSessions();

                    Log.Info("Session Loaded!");
                    break;

                case TorchSessionState.Unloading:

                    stopWatch.Stop();

                    if (multiplayerManagerBase != null) {

                        multiplayerManagerBase.PlayerJoined -= PlayerJoined;
                        multiplayerManagerBase.PlayerLeft -= PlayerLeft;

                    } else {
                        Log.Warn("No multiplayer manager loaded!");
                    }

                    LogEveryoneOut();

                    Log.Info("Session Unloading!");
                    break;
            }
        }

        private void PlayerConnected(long playerId) {
            throw new NotImplementedException();
        }

        private void PlayerJoined(IPlayer obj) {

            ulong SteamId = obj.SteamId;
            string Name = obj.Name;

            string ip = "0.0.0.0";

            if(networking != null) {

                var state = new MyP2PSessionState();
                networking.Peer2Peer.GetSessionState(SteamId, ref state);
                var ipBytes = BitConverter.GetBytes(state.RemoteIP).Reverse().ToArray();
                ip = new IPAddress(ipBytes).ToString();
            }

            LogEntries.LoginPlayer(SteamId, Name, ip, Config);
            Log.Info(obj.Name + " joined.");
        }

        private void PlayerLeft(IPlayer obj) {
            LogEntries.LogoutPlayer(obj.SteamId, false);
            Log.Info(obj.Name + " left.");
        }

        internal void LogEveryoneOut() {
            
            var result = new List<MyPlayer>(MySession.Static.Players.GetOnlinePlayers()
                .Where(x => x.IsRealPlayer && !string.IsNullOrEmpty(x.DisplayName)));
            
            foreach(MyPlayer player in result) {

                var Identity = player.Identity;

                ulong SteamId = MySession.Static.Players.TryGetSteamId(Identity.IdentityId);

                LogEntries.LogoutPlayer(SteamId, true);
            }

            _connectionLogManager.SaveLogEntriesSync(LogEntries, Config);

            Log.Info("Logged everyone off.");
        }

        public void SaveLogEntriesAsync() {
            _connectionLogManager.SaveLogEntriesAsync(LogEntries, Config);
        }

        private void SetupConfig() {

            var configFile = Path.Combine(StoragePath, CONFIG_FILE_NAME);

            try {

                _config = Persistent<ConnectionLogConfig>.Load(configFile);

            } catch (Exception e) {
                Log.Warn(e);
            }

            if (_config?.Data == null) {

                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<ConnectionLogConfig>(configFile, new ConnectionLogConfig());
                _config.Save();
            }
        }

        public void Save() {
            try {
                _config.Save();
                Log.Info("Configuration Saved.");
            } catch (IOException e) {
                Log.Warn(e, "Configuration failed to save");
            }
        }

        public static class MySteamGameService_Networking {
            const string Name = "m_networking";
            const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
            static readonly Type Type = typeof(MySteamGameService);
            static readonly FieldInfo Field = Type.GetField(Name, Flags);
            public static IMyNetworking Value => (IMyNetworking)Field.GetValue(null);
        }
    }
}
