using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Torch;

namespace ALE_ConnectionLog.model {
    public class ConnectionLog {

        private Dictionary<ulong, ConnectionPlayerInfo> _playerInfos = new Dictionary<ulong, ConnectionPlayerInfo>();

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