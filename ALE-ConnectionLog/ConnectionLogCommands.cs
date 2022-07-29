using ALE_ConnectionLog.model;
using ALE_Core.Cooldown;
using ALE_Core.Utils;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;

namespace ALE_ConnectionLog {
    [Category("connectlog")]
    public class ConnectionLogCommands : CommandModule {

        public ConnectionLogPlugin Plugin => (ConnectionLogPlugin)Context.Plugin;

        [Command("admin save", "Saves the log immediately.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Save() {
            Plugin.SaveLogEntriesAsync();
            Context.Respond("Log saved!");
        }

        [Command("admin reload", "Reloads the file from file system.")]
        [Permission(MyPromoteLevel.Owner)]
        public void Reload() {

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if (!CheckConformation(steamId, "reload"))
                return;

            Plugin.Reload();

            var connectionLog = Plugin.LogEntries;
            
            LogEveryoneInAgain(connectionLog);

            Context.Respond("Done!");
        }

        [Command("admin wipe all", "Deletes all Logs.")]
        [Permission(MyPromoteLevel.Owner)]
        public void Wipe() {

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if (!CheckConformation(steamId, "wipe all"))
                return;

            var connectionLog = Plugin.LogEntries;
            int countPlayers = connectionLog.GetPlayerCount();

            connectionLog.Clear();

            LogEveryoneInAgain(connectionLog);

            Context.Respond("Deleted Logs for "+ countPlayers + " players!");
        }

        [Command("admin wipe sessions", "Deletes all Session Data (good if you want to keep playtime after server reset).")]
        [Permission(MyPromoteLevel.Owner)]
        public void CleanSessions() {

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if (!CheckConformation(steamId, "wipe sessions"))
                return;

            var connectionLog = Plugin.LogEntries;
            int countPlayers = connectionLog.GetPlayerCount();

            connectionLog.ClearSessions();

            LogEveryoneInAgain(connectionLog);

            Context.Respond("Deleted Logs for " + countPlayers + " players!");
        }

        [Command("admin logoutall", "Logs all Players out.")]
        [Permission(MyPromoteLevel.Admin)]
        public void LogoutAll() {
            Plugin.LogEveryoneOut();
            Context.Respond("Done!");
        }

        [Command("admin open", "Finds all open Sessions.")]
        [Permission(MyPromoteLevel.Owner)]
        public void OpenSessions() {

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;

            int count = 0;

            foreach (var playerInfo in connectionLog.GetPlayerInfos()) {

                foreach (var entry in playerInfo.GetEntries()) {

                    if(entry.Logout == null) { 

                        long identityId = MySession.Static.Players.TryGetIdentityId(playerInfo.SteamId);
                        var identity = MySession.Static.Players.TryGetIdentity(identityId);

                        sb.AppendLine(playerInfo.SteamId + " " + entry.Name);

                        if (identity != null) {

                            string faction = FactionUtils.GetPlayerFactionTag(identity.IdentityId);

                            if (faction == "")
                                sb.AppendLine("   #" + identity.IdentityId + "   " + identity.DisplayName);
                            else
                                sb.AppendLine("   #" + identity.IdentityId + "   " + identity.DisplayName + " [" + faction + "]");
                        }

                        sb.Append("   ");
                        AddPlayTimeToSb(sb, entry);

                        count++;
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("Found a total of "+count+" sessions!");

            Respond(sb, "Open Sessions", "Sessions without Logout");
        }

        [Command("admin fix", "Fixes all open sessions that are not current.")]
        [Permission(MyPromoteLevel.Owner)]
        public void FixSessions() {

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if (!CheckConformation(steamId, "fix"))
                return;

            var connectionLog = Plugin.LogEntries;

            int count = 0;

            foreach (var playerInfo in connectionLog.GetPlayerInfos()) {

                var latestEntry = playerInfo.GetLatestEntry();

                if (latestEntry == null)
                    continue;

                foreach (var entry in playerInfo.GetEntries()) {

                    /* skip current */
                    if (entry == latestEntry)
                        continue;

                    if (entry.Logout == null) {
                        entry.ForceLogout();
                        count++;
                    }
                }
            }

            Context.Respond("Fixed "+ count+ " sessions.");
        }

        [Command("top", "Lists all players ordered by playtime.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void Top(int top = 10) {

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;

            Dictionary<ConnectionPlayerInfo, long> playerDictionary = new Dictionary<ConnectionPlayerInfo, long>();

            foreach (var playerInfo in connectionLog.GetPlayerInfos()) {

                var latestEntry = playerInfo.GetLatestEntry();

                int playtimeCorrection = 0;

                /* Add playtime since last login, if player is logged in */
                if(latestEntry != null && latestEntry.Logout == null)
                    playtimeCorrection = (int) (DateTime.Now - latestEntry.Login.SnapshotTime).TotalSeconds;
                
                playerDictionary[playerInfo] = playerInfo.TotalPlayTime + playtimeCorrection;
            }

            var playerList = playerDictionary.ToList();

            playerList.Sort((o1, o2) => {
                return o2.Value.CompareTo(o1.Value);
            });

            if (top > playerList.Count)
                top = playerList.Count;

            for (int i = 0; i < top; i++) {

                var entry = playerList[i];
                var key = entry.Key;

                long identityId = MySession.Static.Players.TryGetIdentityId(key.SteamId);
                var identity = MySession.Static.Players.TryGetIdentity(identityId);

                string name;

                DateTime lastSeen;

                if (identity != null) {

                    string faction = FactionUtils.GetPlayerFactionTag(identity.IdentityId);

                    if (faction == "")
                        name = identity.DisplayName;
                    else
                        name = identity.DisplayName + " [" + faction + "]";

                    lastSeen = PlayerUtils.GetLastSeenDate(identity);

                } else {

                    name = key.LastName;
                    lastSeen = key.LastSeen.SnapshotTime;
                }

                sb.AppendLine(FormatTime(entry.Value) + " " + key.SteamId + " " + name + " " + lastSeen.ToString("yyyy-MM-dd  HH:mm:ss"));
            }

            Respond(sb, "Top Playtime", "Shows "+ top +" players");
        }

        [Command("playtime", "Outputs the total Playtimes of the specified player.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void PlayTime(string nameIdOrSteamId) {

            PlayerParam? playerParam = FindPlayerParam(nameIdOrSteamId);

            if (!playerParam.HasValue) {
                Context.Respond("Player not found!");
                return;
            }

            StringBuilder sb = new StringBuilder();

            AddLastSeenToSb(sb, playerParam.Value.SteamId);

            var connectionLog = Plugin.LogEntries;
            var playerInfo = connectionLog.GetInfoForPlayer(playerParam.Value.SteamId);

            sb.AppendLine("Last known data vs current");
            sb.AppendLine("--------------------------");
            sb.AppendLine("Name: " + playerInfo.LastName);
            AddSessionToSb(sb, playerInfo.LastSeen, PlayerSnapshotFactory.Create(playerInfo.SteamId), "");
            sb.AppendLine();

            sb.AppendLine("Total playtime: " + FormatTime(playerInfo.TotalPlayTime));
            sb.AppendLine("--------------------------");
            sb.AppendLine();

            foreach (var entry in playerInfo.GetEntries()) {
                AddPlayTimeToSb(sb, entry);
            }

            Respond(sb, "Playtime", "Player " + playerParam.Value.Name);
        }

        [Command("ips", "Shows all known IPs of the Player.")]
        [Permission(MyPromoteLevel.Admin)]
        public void IPs(string nameIdOrSteamId) {

            PlayerParam? playerParam = FindPlayerParam(nameIdOrSteamId);

            if (!playerParam.HasValue) {
                Context.Respond("Player not found!");
                return;
            }

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;
            var playerInfo = connectionLog.GetInfoForPlayer(playerParam.Value.SteamId);

            foreach (var entry in playerInfo.GetEntries())
                sb.AppendLine(entry.Name + " " + entry.IP +" "+ entry.GetLastDateTime().ToString("yyyy-MM-dd  HH:mm:ss"));

            Respond(sb, "IPs", "Player " + playerParam.Value.Name);
        }

        [Command("names", "Outputs all known names of the selected Player.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void Names(string nameIdOrSteamId) {

            PlayerParam? playerParam = FindPlayerParam(nameIdOrSteamId);

            if (!playerParam.HasValue) {
                Context.Respond("Player not found!");
                return;
            }

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;
            var playerInfo = connectionLog.GetInfoForPlayer(playerParam.Value.SteamId);

            var names = new List<string>(playerInfo.GetNames());
            names.Sort();

            foreach(var name in names)
            sb.AppendLine("--> "+name);

            Respond(sb, "Playernames", "All known names of Player " + playerParam.Value.Name);
        }

        [Command("sessions", "Outputs the sessions of the specified player.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void Sessions(string nameIdOrSteamId) {

            PlayerParam? playerParam = FindPlayerParam(nameIdOrSteamId);

            if (!playerParam.HasValue) {
                Context.Respond("Player not found!");
                return;
            }

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;
            var playerInfo = connectionLog.GetInfoForPlayer(playerParam.Value.SteamId);

            var config = Plugin.Config;

            AddLastSeenToSb(sb, playerParam.Value.SteamId);

            sb.AppendLine("Last known data vs current");
            sb.AppendLine("--------------------------");
            sb.AppendLine("Name: " + playerInfo.LastName);
            AddSessionToSb(sb, playerInfo.LastSeen, PlayerSnapshotFactory.Create(playerInfo.SteamId), "");
            sb.AppendLine();

            sb.AppendLine("Total playtime: " + FormatTime(playerInfo.TotalPlayTime));
            sb.AppendLine("--------------------------");
            sb.AppendLine();

            sb.AppendLine();
            sb.AppendLine("Sessions");
            sb.AppendLine("--------------------------");
            sb.AppendLine();
            foreach (var entry in playerInfo.GetEntries()) {

                if(config.ShowIpInSessionsCommand)
                    sb.AppendLine(entry.Name+" "+entry.IP);
                else 
                    sb.AppendLine(entry.Name);

                AddPlayTimeToSb(sb, entry);

                AddSessionToSb(sb, entry.Login, entry.Logout, "");

                if(entry.LogoutThroughSessionUnload)
                    sb.AppendLine("Logged off through Serverrestart");

                sb.AppendLine();
            }

            Respond(sb, "Session", "Player " + playerParam.Value.Name);
        }

        [Command("find", "Looks for the Data of a specific player by namePattern.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void Find(string namePattern) {

            StringBuilder sb = new StringBuilder();

            namePattern = namePattern.ToLower();

            var connectionLog = Plugin.LogEntries;

            int count = 0;

            foreach (var playerInfo in connectionLog.GetPlayerInfos()) {

                foreach(var knownName in playerInfo.GetNames()) {

                    if(Matches(knownName, namePattern)) {

                        sb.AppendLine("Found Steam User: "+playerInfo.SteamId+" with name "+ knownName);

                        long identityId = MySession.Static.Players.TryGetIdentityId(playerInfo.SteamId);
                        var identity = MySession.Static.Players.TryGetIdentity(identityId);

                        if (identity != null) {

                            string faction = FactionUtils.GetPlayerFactionTag(identity.IdentityId);

                            if(faction == "")
                                sb.AppendLine("    Player: " + identity.IdentityId + "   " + identity.DisplayName);
                            else
                                sb.AppendLine("    Player: " + identity.IdentityId + "   " + identity.DisplayName + " ["+ faction + "]");
                        }

                        var entry = playerInfo.GetLatestEntry();

                        if(entry != null)
                            sb.AppendLine("    Session: " + entry.Name + "   " + entry.GetLastDateTime().ToString("yyyy-MM-dd HH:mm:ss"));

                        sb.AppendLine();

                        count++;

                        break;
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("Found a total of " + count + " matches!");

            Respond(sb, "Found Players", "Players whose name matches case insensitive");
        }

        [Command("online date", "Outputs which players logged in at the same date.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void Date(int day = 0, int month = 0, int year = 0) {

            var now = DateTime.Now;

            if (day <= 0)
                day = now.Day;

            if (month <= 0)
                month = now.Month;

            if (year <= 0)
                year = now.Year;

            DateTime lookupDate = new DateTime(year, month, day);

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;

            int count = 0;

            foreach (var playerInfo in connectionLog.GetPlayerInfos()) {

                foreach (var entry in playerInfo.GetEntries()) {

                    DateTime loginDate = entry.Login.SnapshotTime.Date;
                    DateTime logoutDate = loginDate;

                    if (entry.Logout != null)
                        logoutDate = entry.Logout.SnapshotTime.Date;

                    if (loginDate == lookupDate.Date || logoutDate == lookupDate.Date) {

                        long identityId = MySession.Static.Players.TryGetIdentityId(playerInfo.SteamId);
                        var identity = MySession.Static.Players.TryGetIdentity(identityId);

                        sb.AppendLine(playerInfo.SteamId + " " + entry.Name);

                        if (identity != null) {

                            string faction = FactionUtils.GetPlayerFactionTag(identity.IdentityId);

                            if (faction == "")
                                sb.AppendLine("   #" + identity.IdentityId + "   " + identity.DisplayName);
                            else
                                sb.AppendLine("   #" + identity.IdentityId + "   " + identity.DisplayName + " [" + faction + "]");
                        }

                        sb.Append("   ");
                        AddPlayTimeToSb(sb, entry);

                        count++;

                        break;
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("Found a total of " + count + " unique players!");

            Respond(sb, "Found Players", "Played on " + lookupDate.ToString("yyyy-MM-dd"));
        }

        [Command("online time", "Outputs which players logged in at the same date.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void Date(int hour = -1, int minute = -1, int second = -1, int day = 0, int month = 0, int year = 0) {

            var now = DateTime.Now;

            if (hour < 0) {

                hour = now.Hour;

                if (minute < 0) {

                    minute = now.Minute;

                    if (second < 0)
                        second = now.Second;

                } else {

                    if (second < 0)
                        second = 0;
                }

            } else {

                if (minute < 0)
                    minute = 0;

                if (second < 0)
                    second = 0;
            }

            if (day <= 0)
                day = now.Day;

            if (month <= 0)
                month = now.Month;

            if (year <= 0)
                year = now.Year;

            DateTime lookupDate = new DateTime(year, month, day, hour, minute, second);

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;

            int count = 0;

            foreach (var playerInfo in connectionLog.GetPlayerInfos()) {

                foreach (var entry in playerInfo.GetEntries()) {

                    var loginDate = entry.Login.SnapshotTime;

                    var logoutDate = now;
                    if (entry.Logout != null)
                        logoutDate = entry.Logout.SnapshotTime;

                    if(loginDate <= lookupDate && lookupDate <= logoutDate) { 

                        long identityId = MySession.Static.Players.TryGetIdentityId(playerInfo.SteamId);
                        var identity = MySession.Static.Players.TryGetIdentity(identityId);

                        sb.AppendLine(playerInfo.SteamId + " " + entry.Name);

                        if (identity != null) {

                            string faction = FactionUtils.GetPlayerFactionTag(identity.IdentityId);

                            if (faction == "")
                                sb.AppendLine("   #" + identity.IdentityId + "   " + identity.DisplayName);
                            else
                                sb.AppendLine("   #" + identity.IdentityId + "   " + identity.DisplayName + " [" + faction + "]");
                        }

                        sb.Append("   ");
                        AddPlayTimeToSb(sb, entry);

                        count++;

                        break;
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("Found a total of " + count + " unique players!");

            Respond(sb, "Found Players", "Played on " + lookupDate.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [Command("multis", "Outputs which Players had the same IP adress and when.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Multis(bool showMissingIPs = false) {

            StringBuilder sb = new StringBuilder();

            Dictionary<string, Dictionary<string, HashSet<ulong>>> ipToDateToSteamDict = new Dictionary<string, Dictionary<string, HashSet<ulong>>>();

            var connectionLog = Plugin.LogEntries;
            var config = Plugin.Config;

            foreach (var playerInfo in connectionLog.GetPlayerInfos()) {

                foreach(var entry in playerInfo.GetEntries()) {

                    if (entry.IP == "0.0.0.0" && config.IgnoreMissingIpsForConflicts && !showMissingIPs)
                        continue;

                    Dictionary<string, HashSet<ulong>> dateToNameDict;

                    if (!ipToDateToSteamDict.ContainsKey(entry.IP)) {
                        dateToNameDict = new Dictionary<string, HashSet<ulong>>();
                        ipToDateToSteamDict[entry.IP] = dateToNameDict;
                    } else {
                        dateToNameDict = ipToDateToSteamDict[entry.IP];
                    }

                    string date = entry.Login.SnapshotTime.ToString("yyyy-MM-dd");

                    HashSet<ulong> steamSet;

                    if (!dateToNameDict.ContainsKey(date)) {
                        steamSet = new HashSet<ulong>();
                        dateToNameDict[date] = steamSet;
                    } else {
                        steamSet = dateToNameDict[date];
                    }

                    steamSet.Add(playerInfo.SteamId);
                }
            }

            foreach (var ipEntry in ipToDateToSteamDict) {

                string ip = ipEntry.Key;

                HashSet<ulong> allKnownSteamIdsToIp = new HashSet<ulong>();

                foreach (var dateEntry in ipEntry.Value) 
                    allKnownSteamIdsToIp.UnionWith(dateEntry.Value);

                if (allKnownSteamIdsToIp.Count <= 1)
                    continue;

                sb.AppendLine(ip);

                foreach (var dateEntry in ipEntry.Value) {

                    string date = dateEntry.Key;
                    HashSet<ulong> steamIds = dateEntry.Value;

                    sb.AppendLine("   " + date);

                    foreach (ulong steamId in steamIds) {

                        long identityId = MySession.Static.Players.TryGetIdentityId(steamId);
                        MyIdentity identity = PlayerUtils.GetIdentityById(identityId);

                        if (identity == null)
                            sb.AppendLine("      " + steamId);
                        else
                            sb.AppendLine("      " + steamId + " " + identity.DisplayName + " #" + identity.IdentityId);
                    }
                }
            }

            Respond(sb, "Potential Multiaccounts", "Shows who shared IPs and when");
        }

        [Command("sus", "Shows suspicious players, which have now much higher PCU than when they last logged in.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Sus(int marginOfError = 0, int ignoreDays = 0, bool ignoreMissingIdentities = true) {

            StringBuilder sb = new StringBuilder();

            Dictionary<string, Dictionary<string, HashSet<ulong>>> ipToDateToSteamDict = new Dictionary<string, Dictionary<string, HashSet<ulong>>>();

            var nowWithMargin = DateTime.Now.AddDays(-ignoreDays);

            var connectionLog = Plugin.LogEntries;

            int count = 0;

            foreach (var playerInfo in connectionLog.GetPlayerInfos()) {

                long identityId = MySession.Static.Players.TryGetIdentityId(playerInfo.SteamId);
                MyIdentity identity = PlayerUtils.GetIdentityById(identityId);

                bool identityPresent = identity != null;

                var lastSeen = playerInfo.LastSeen;

                if (!identityPresent) {

                    if (ignoreMissingIdentities)
                        continue;

                    sb.AppendLine(playerInfo.SteamId + " " + playerInfo.LastName);
                    sb.AppendLine("   Last Seen: " + lastSeen.SnapshotTime.ToString("yyyy-MM-dd  HH:mm:ss"));
                    sb.AppendLine("   Total Playtime: " + FormatTime(playerInfo.TotalPlayTime));

                    sb.AppendLine("   Has no Identity anymore.");
                    sb.AppendLine();

                    continue;
                }
                    
                int currentPcu = identity.BlockLimits.PCUBuilt;
                int lastSeenPcu = lastSeen.PCU;

                int pcuDifference = currentPcu - lastSeenPcu - marginOfError;

                bool pcuFine = pcuDifference <= 0;

                if (!pcuFine) {

                    var lastEntry = playerInfo.GetLatestEntry();

                    /* Ignore logged in players lul */
                    if (lastEntry != null && lastEntry.Logout == null)
                        continue;

                    var lastSeenDate = PlayerUtils.GetLastSeenDate(identity);

                    if (nowWithMargin < lastSeenDate)
                        continue;

                    sb.AppendLine(playerInfo.SteamId + " " + playerInfo.LastName);

                    sb.AppendLine("   Last Seen: " + lastSeenDate.ToString("yyyy-MM-dd  HH:mm:ss"));
                    sb.AppendLine("   Total Playtime: " + FormatTime(playerInfo.TotalPlayTime));

                    string faction = FactionUtils.GetPlayerFactionTag(identity.IdentityId);

                    if (faction == "")
                        sb.AppendLine("   #" + identity.IdentityId + "   " + identity.DisplayName);
                    else
                        sb.AppendLine("   #" + identity.IdentityId + "   " + identity.DisplayName + " [" + faction + "]");

                    sb.AppendLine("   Used to have " + lastSeenPcu + " but now has " + currentPcu + " (" + (currentPcu - lastSeenPcu) + ")");
                    sb.AppendLine();

                    count++;
                }
            }

            sb.AppendLine();
            sb.AppendLine("Found a total of " + count + " suspicious accounts!");

            Respond(sb, "Suspicious Accounts", "Shows accounts who have higher PCU than when they were last seen.");
        }

        private static void AddSessionToSb(StringBuilder sb, PlayerSnapshot referenceSnapshot, PlayerSnapshot nowSnapshot, string prefix) {

            if (nowSnapshot == null) {
                
                sb.AppendLine(prefix + "identity: " + referenceSnapshot.IdentityId);
                sb.AppendLine(prefix + "faction: " + referenceSnapshot.Faction);
                sb.AppendLine(prefix + "blocks: " + referenceSnapshot.BlockCount);
                sb.AppendLine(prefix + "PCU: " + referenceSnapshot.PCU);
                sb.AppendLine(prefix + "Grids: " + referenceSnapshot.GridCount);
                
                return;
            }

            if (referenceSnapshot.IdentityId != nowSnapshot.IdentityId)
                sb.AppendLine(prefix + "Identity: " + referenceSnapshot.IdentityId + " -> " + nowSnapshot.IdentityId);
            else
                sb.AppendLine(prefix + "Identity: " + referenceSnapshot.IdentityId);

            if (referenceSnapshot.Faction != nowSnapshot.Faction)
                sb.AppendLine(prefix + "Faction: " + referenceSnapshot.Faction + " -> " + nowSnapshot.Faction);
            else
                sb.AppendLine(prefix + "Faction: " + referenceSnapshot.Faction);

            if (referenceSnapshot.BlockCount != nowSnapshot.BlockCount)
                sb.AppendLine(prefix + "Blocks: " + referenceSnapshot.BlockCount + " -> " + nowSnapshot.BlockCount);
            else
                sb.AppendLine(prefix + "Blocks: " + referenceSnapshot.BlockCount);

            if (referenceSnapshot.PCU != nowSnapshot.PCU)
                sb.AppendLine(prefix + "PCU: " + referenceSnapshot.PCU + " -> " + nowSnapshot.PCU);
            else
                sb.AppendLine(prefix + "PCU: " + referenceSnapshot.PCU);

            if (referenceSnapshot.GridCount != nowSnapshot.GridCount)
                sb.AppendLine(prefix + "Grids: " + referenceSnapshot.GridCount + " -> " + nowSnapshot.GridCount);
            else
                sb.AppendLine(prefix + "Grids: " + referenceSnapshot.GridCount);
        }

        private static void AddLastSeenToSb(StringBuilder sb, ulong steamId) {

            long identityId = MySession.Static.Players.TryGetIdentityId(steamId);
            var identity = MySession.Static.Players.TryGetIdentity(identityId);

            if (identity == null)
                return;

            sb.AppendLine("Last Login: "+ identity.LastLoginTime.ToString("yyyy-MM-dd  HH:mm:ss"));
            sb.AppendLine("Last Logout: " + identity.LastLoginTime.ToString("yyyy-MM-dd  HH:mm:ss"));
            sb.AppendLine();
        }

        private static void AddPlayTimeToSb(StringBuilder sb, model.ConnectionPlayerInfo.ConnectionEntry entry) {
            
            string dateStringLogin = entry.Login.SnapshotTime.ToString("yyyy-MM-dd  HH:mm:ss");

            sb.Append(dateStringLogin);

            if (entry.Logout != null) {

                string dateStringLogout = entry.Logout.SnapshotTime.ToString("yyyy-MM-dd HH:mm:ss");
                int duration = entry.GetDurationMinutes();

                sb.Append(" -> " + dateStringLogout + " -> ");
                sb.Append(duration.ToString("0"));

            } else {

                sb.Append(" -> now -> ");
                sb.Append((DateTime.Now - entry.Login.SnapshotTime).TotalMinutes.ToString("0"));
            }

            sb.AppendLine(" minutes");
        }

        public static string FormatTime(long timeInSeconds) {

            long timeInMinutes = timeInSeconds / 60;

            long hours = timeInMinutes / 60;
            long minutes = timeInMinutes % 60;

            if (hours == 0 && minutes == 0)
                return "none";

            string returnString = "";

            if (hours > 0)
                returnString += hours + "h ";

            if(minutes > 0)
                returnString += minutes + "m ";

            return returnString;
        }

        public static bool Matches(string input, string pattern) {

            var regex = WildCardToRegular(pattern);

            return Regex.IsMatch(input.ToLower(), regex);
        }

        private static string WildCardToRegular(string value) {
            return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
        }

        public void Respond(StringBuilder sb, string title, string subtitle) {

            if (Context.Player == null) {

                Context.Respond(title);
                Context.Respond(subtitle);
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage(title,
                    subtitle, sb.ToString()), Context.Player.SteamUserId);
            }
        }

        private bool CheckConformation(ICooldownKey cooldownKey, string command) {

            var cooldownManager = Plugin.ConfirmationCooldownManager;

            if (!cooldownManager.CheckCooldown(cooldownKey, command, out _)) {
                cooldownManager.StopCooldown(cooldownKey);
                return true;
            }

            cooldownManager.StartCooldown(cooldownKey, command, Plugin.CooldownConfirmation);

            Context.Respond("Are you sure you want to continue? Enter the command again within " + Plugin.CooldownConfirmationSeconds + " seconds to confirm.");

            return false;
        }

        private void LogEveryoneInAgain(ConnectionLog connectionLog) {

            /* Now log everyone that is online in again */

            var result = new List<MyPlayer>(MySession.Static.Players.GetOnlinePlayers()
                .Where(x => x.IsRealPlayer && !string.IsNullOrEmpty(x.DisplayName)));

            foreach (MyPlayer player in result) {

                var Identity = player.Identity;
                ulong SteamId = MySession.Static.Players.TryGetSteamId(Identity.IdentityId);
                string ip = Plugin.GetIpOfSteamId(SteamId);

                connectionLog.LoginPlayer(SteamId, player.DisplayName, ip, Plugin.Config);
            }
        }

        private PlayerParam? FindPlayerParam(string nameIdOrSteamId) {

            MyIdentity identity = PlayerUtils.GetIdentityByNameOrId(nameIdOrSteamId);

            ulong steamId;

            if (identity != null) {

                steamId = MySession.Static.Players.TryGetSteamId(identity.IdentityId);

                return new PlayerParam(identity.DisplayName, steamId);
            }

            if (ulong.TryParse(nameIdOrSteamId, out steamId)) {

                return new PlayerParam(steamId.ToString(), steamId);
            }

            return null;
        }

        private struct PlayerParam {
            
            public string Name;
            public ulong SteamId;

            public PlayerParam(string name, ulong steamId) {
                Name = name;
                SteamId = steamId;
            }
        }
    }
}
