using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Torch;
using static ALE_ConnectionLog.model.ConnectionPlayerInfo;

namespace ALE_ConnectionLog.model {
    public class ConnectionLog {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<ulong, ConnectionPlayerInfo> _playerInfos = new Dictionary<ulong, ConnectionPlayerInfo>();
        private readonly Dictionary<ulong, ConnectionEntry> memorizedEntries = new Dictionary<ulong, ConnectionEntry>();

        public IEnumerable<ConnectionPlayerInfo> GetPlayerInfos() {
            return _playerInfos.Values;
        }

        public ConnectionPlayerInfo GetInfoForPlayer(ulong steamId) {

            if(!_playerInfos.TryGetValue(steamId, out ConnectionPlayerInfo info)) {
                info = new ConnectionPlayerInfo(steamId);
            }

            return info;
        }

        public void UpdateInfoForPlayer(ConnectionPlayerInfo connectionInfo) {

            if (_playerInfos.ContainsKey(connectionInfo.SteamId))
                _playerInfos.Remove(connectionInfo.SteamId);

            _playerInfos.Add(connectionInfo.SteamId, connectionInfo);
        }

        public void LoginPlayer(ulong steamId, string name, string ip, ConnectionLogConfig config) {

            ConnectionPlayerInfo playerInfo = GetInfoForPlayer(steamId);

            playerInfo.Login(name, ip, config);

            UpdateInfoForPlayer(playerInfo);
        }

        internal void MemorizeNonClosedSessions() {

            memorizedEntries.Clear();

            foreach (var playerInfo in _playerInfos.Values) {

                var latestEntry = playerInfo.GetLatestEntry();

                if (latestEntry != null && latestEntry.Logout == null)
                    memorizedEntries.Add(playerInfo.SteamId, latestEntry);
            }

            if (memorizedEntries.Count > 0)
                Log.Warn("Found " + memorizedEntries.Count + " unclosed sessions!");
        }

        internal void CloseMemorizedSessions() {

            foreach (var entry in memorizedEntries) {

                ulong steamId = entry.Key;

                var playerInfo = GetInfoForPlayer(steamId);
                var rememeredEntry = entry.Value;

                playerInfo.ForceLogout(rememeredEntry, true);
            }

            if(memorizedEntries.Count > 0)
                Log.Info(memorizedEntries.Count + " sessions closed.");

            memorizedEntries.Clear();
        }

        internal void LogoutPlayer(ulong steamId, bool sessionUnloading) {
            
            ConnectionPlayerInfo playerInfo = GetInfoForPlayer(steamId);

            playerInfo.Logout(sessionUnloading);

            UpdateInfoForPlayer(playerInfo);
        }

        internal void CleanupEntriesOlderThan(ConnectionLogConfig config) {

            var playerInfoList = new List<ConnectionPlayerInfo>(_playerInfos.Values);

            foreach (var playerInfo in playerInfoList) 
                playerInfo.CleanupEntriesOlderThan(config);
        }
    }
}