using ALE_ConnectionLog.model;
using ALE_ConnectionLog.serialize;
using NLog;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using Torch;
using static ALE_ConnectionLog.model.ConnectionPlayerInfo;

namespace ALE_ConnectionLog {

    public class ConnectionLogManager {

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string CONNECTION_LOG_FILE_NAME = "ConnectionLogEntries.xml";
        
        private readonly string storagePath;
        private Persistent<ConnectionLogDto> _logEntries;

        public ConnectionLogManager(string storagePath) {
            this.storagePath = storagePath;
        }

        internal ConnectionLog Init() {

            var logFile = Path.Combine(storagePath, CONNECTION_LOG_FILE_NAME);

            try {

                _logEntries = Persistent<ConnectionLogDto>.Load(logFile);

            } catch (Exception e) {
                Log.Warn(e);
            }

            if (_logEntries?.Data == null)
                return new ConnectionLog();

            var ConnectLog = Convert(_logEntries.Data);

            ConnectLog.MemorizeNonClosedSessions();

            return ConnectLog;
        }

        public void SaveLogEntriesAsync(ConnectionLog connectionLog, ConnectionLogConfig config) {

            MyAPIGateway.Parallel.StartBackground(() => {
                SaveLogEntriesSync(connectionLog, config);
            });
        }

        public void SaveLogEntriesSync(ConnectionLog connectionLog, ConnectionLogConfig config) {

            try {

                var logFile = Path.Combine(storagePath, CONNECTION_LOG_FILE_NAME);

                connectionLog.CleanupEntriesOlderThan(config);

                ConnectionLogDto connectionLogDto = Convert(connectionLog);

                _logEntries = new Persistent<ConnectionLogDto>(logFile, connectionLogDto);
                _logEntries.Save();

                Log.Info("Saved LogEntries.");

            } catch (Exception e) {
                Log.Error(e, "Save Failed");
            }
        }

        private ConnectionLogDto Convert(ConnectionLog connectionLog) {

            var connectionLogDto = new ConnectionLogDto();

            foreach(var connectionPlayerInfo in connectionLog.GetPlayerInfos()) {

                CpiDto cpiDto = new CpiDto();
                cpiDto.SID = connectionPlayerInfo.SteamId;
                cpiDto.AKN = new HashSet<string>(connectionPlayerInfo.GetNames());
                cpiDto.LS = connectionPlayerInfo.LastSeen;
                cpiDto.TPT = connectionPlayerInfo.TotalPlayTime;

                foreach(var entry in connectionPlayerInfo.GetEntries()) {

                    CeDto ceDto = new CeDto();

                    ceDto.IP = entry.IP;
                    ceDto.Name = entry.Name;
                    ceDto.LSU = entry.LogoutThroughSessionUnload;

                    var loginSnapshot = entry.Login;
                    PsDto loginDto = new PsDto() {
                        IId = loginSnapshot.IdentityId,
                        Blk = loginSnapshot.BlockCount,
                        GC = loginSnapshot.GridCount,
                        PCU = loginSnapshot.PCU,
                        F = loginSnapshot.Faction,
                        ST = loginSnapshot.SnapshotTime
                    };
                    ceDto.Login = loginDto;

                    var logoutSnapshot = entry.Logout;
                    
                    if (logoutSnapshot != null) {
                        
                        PsDto logoutDto = new PsDto() {
                            IId = logoutSnapshot.IdentityId,
                            Blk = logoutSnapshot.BlockCount,
                            GC = logoutSnapshot.GridCount,
                            PCU = logoutSnapshot.PCU,
                            F = logoutSnapshot.Faction,
                            ST = logoutSnapshot.SnapshotTime
                        };
                        
                        ceDto.Logout = logoutDto;
                    }

                    cpiDto.CE.Add(ceDto);
                }

                connectionLogDto.CLE.Add(cpiDto);
            }

            return connectionLogDto;
        }

        private ConnectionLog Convert(ConnectionLogDto data) {

            var connectionLog = new ConnectionLog();

            foreach(var logDto in data.CLE) {

                var infoForPlayer = connectionLog.GetInfoForPlayer(logDto.SID);

                infoForPlayer.LastSeen = logDto.LS;
                infoForPlayer.TotalPlayTime = logDto.TPT;

                infoForPlayer.SetNames(logDto.AKN);

                foreach(var ceDto in logDto.CE) {

                    var loginDto = ceDto.Login;

                    PlayerSnapshot loginSnapshot = new PlayerSnapshot(
                        loginDto.IId, loginDto.PCU, loginDto.Blk, loginDto.GC, loginDto.F, loginDto.ST);

                    var logoutDto = ceDto.Logout;

                    PlayerSnapshot logoutSnapshot = null;

                    if (logoutDto != null) {
                        
                        logoutSnapshot = new PlayerSnapshot(
                            logoutDto.IId, logoutDto.PCU, logoutDto.Blk, logoutDto.GC, logoutDto.F, logoutDto.ST);
                    }

                    var connectionEntry = new ConnectionEntry(ceDto.IP, ceDto.Name, loginSnapshot, logoutSnapshot, ceDto.LSU);

                    infoForPlayer.AddEntry(connectionEntry);
                }

                infoForPlayer.SortEntries();

                connectionLog.UpdateInfoForPlayer(infoForPlayer);
            }

            return connectionLog;
        }
    }
}
