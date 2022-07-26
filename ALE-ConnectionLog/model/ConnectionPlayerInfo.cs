using System;
using System.Collections.Generic;
using VRage.Collections;

namespace ALE_ConnectionLog.model {
    public class ConnectionPlayerInfo {

        public ulong SteamId { get; }
        public DateTime LastSeen { get; set; }
        public long TotalPlayTime { get; set; }

        private MyConcurrentHashSet<string> _allKnownNames = new MyConcurrentHashSet<string>();
        private MyConcurrentList<ConnectionEntry> _connectionEntries = new MyConcurrentList<ConnectionEntry>();
        
        public ConnectionPlayerInfo(ulong SteamId) {
            this.SteamId = SteamId;
        }

        internal void Login(string name, string ip, ConnectionLogConfig config) {

            _allKnownNames.Add(name);

            LastSeen = DateTime.Now;

            if (!config.SaveIPs)
                ip = "0.0.0.0";

            var snapshot = PlayerSnapshotFactory.Create(SteamId);

            ConnectionEntry entry = new ConnectionEntry(ip, name, snapshot);

            _connectionEntries.Insert(0, entry);
        }

        internal void Logout(bool sessionUnloading) {

            LastSeen = DateTime.Now;

            ConnectionEntry entry = GetLatestEntry();

            if (entry == null)
                return;

            if (entry.Login != null)
                TotalPlayTime += (long) (DateTime.Now - entry.Login.SnapshotTime).TotalSeconds;

            var snapshot = PlayerSnapshotFactory.Create(SteamId);

            entry.SetLogout(snapshot, sessionUnloading);
        }

        private ConnectionEntry GetLatestEntry() {
            return _connectionEntries.Count > 0 ? _connectionEntries[0] : null;
        }

        internal void CleanupEntriesOlderThan(ConnectionLogConfig config) {

            var today = DateTime.Now;

            if(config.KeepLogMaxDays > 0) {

                for (int i = _connectionEntries.Count - 1; i >= 0; i--) {

                    var lastSeenDate = _connectionEntries[i].GetLastDateTime();
                    var daysSince = (today - lastSeenDate).TotalDays;

                    if (daysSince > config.KeepLogMaxDays)
                        _connectionEntries.RemoveAt(i);
                }
            }

            if (config.KeepMaxAmountEntriesPerPlayer > 0) 
                for (int i = _connectionEntries.Count - 1; i >= config.KeepMaxAmountEntriesPerPlayer; i--) 
                    _connectionEntries.RemoveAt(i);
        }

        internal IEnumerable<string> GetNames() {
            return _allKnownNames;
        }

        internal void SetNames(HashSet<string> names) {
            foreach (string name in names)
                _allKnownNames.Add(name);
        }

        internal IEnumerable<ConnectionEntry> GetEntries() {
            return _connectionEntries;
        }

        internal void AddEntry(ConnectionEntry connectionEntry) {
            _connectionEntries.Add(connectionEntry);
        }

        internal void SortEntries() {
            _connectionEntries.Sort(new SortByLoginDateDescending());
        }

        public class SortByLoginDateDescending : IComparer<ConnectionEntry> {
            public int Compare(ConnectionEntry o1, ConnectionEntry o2) {
                return o2.Login.SnapshotTime.CompareTo(o1.Login.SnapshotTime);
            }
        }

        public class ConnectionEntry {

            public string IP { get; }
            public string Name { get; }

            public PlayerSnapshot Login { get; }

            public bool LogoutThroughSessionUnload { get; private set; } = false;
            public PlayerSnapshot Logout { get; private set; }

            public ConnectionEntry(string IP, string Name, PlayerSnapshot Login)
                : this(IP, Name, Login, null, false) { 
            }

            public ConnectionEntry(string IP, string Name, PlayerSnapshot Login, PlayerSnapshot Logout, bool LogoutThroughSessionUnload) {
                this.IP = IP;
                this.Name = Name;
                this.Login = Login;
                this.Logout = Logout;
                this.LogoutThroughSessionUnload = LogoutThroughSessionUnload;
            }

            public void SetLogout(PlayerSnapshot Logout, bool sessionUnloading) {
                this.Logout = Logout;
                this.LogoutThroughSessionUnload = sessionUnloading;
            }

            public DateTime GetLastDateTime() {

                if (Logout != null)
                    return Logout.SnapshotTime;

                return Login.SnapshotTime;
            }
        }
    }
}
