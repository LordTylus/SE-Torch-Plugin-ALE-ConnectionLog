﻿using ALE_ConnectionLog.model;
using ALE_Core.Utils;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace ALE_ConnectionLog {
    [Category("connectlog")]
    public class ConnectionLogCommands : CommandModule {

        public ConnectionLogPlugin Plugin => (ConnectionLogPlugin)Context.Plugin;

        [Command("top", "Lists all players ordered by playtime in this world.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void Top(int top = 10) {
            TopWorld(top);
        }

        [Command("top world", "Lists all players ordered by playtime in this world.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void TopWorld(int top = 10) {

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;

            Dictionary<ConnectionPlayerInfo, long> playerDictionary = new Dictionary<ConnectionPlayerInfo, long>();

            foreach (var playerInfo in connectionLog.GetPlayerInfos())
                playerDictionary[playerInfo] = Utilities.CalcWorldPlayTime(playerInfo);

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

                sb.AppendLine((i+1) +".   "+ Utilities.FormatTime(entry.Value) + "   " + key.SteamId + " " + name + " " + lastSeen.ToString("yyyy-MM-dd  HH:mm:ss"));
            }

            Utilities.Respond(sb, "Top Playtime in World", "Shows "+ top +" players", Context);
        }

        [Command("top total", "Lists all players ordered by playtime overall.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void TopTotal(int top = 10) {

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;

            Dictionary<ConnectionPlayerInfo, long> playerDictionary = new Dictionary<ConnectionPlayerInfo, long>();

            foreach (var playerInfo in connectionLog.GetPlayerInfos())
                playerDictionary[playerInfo] = Utilities.CalcTotalPlayTime(playerInfo);

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

                sb.AppendLine((i + 1) + ".   " + Utilities.FormatTime(entry.Value) + "   " + key.SteamId + " " + name + " " + lastSeen.ToString("yyyy-MM-dd  HH:mm:ss"));
            }

            Utilities.Respond(sb, "Top Playtime on Server", "Shows " + top + " players", Context);
        }

        [Command("playtime", "Outputs the total Playtimes of the specified player.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void PlayTime(string nameIdOrSteamId) {

            Utilities.PlayerParam? playerParam = Utilities.FindPlayerParam(nameIdOrSteamId);

            if (!playerParam.HasValue) {
                Context.Respond("Player not found!");
                return;
            }

            StringBuilder sb = new StringBuilder();

            Utilities.AddLastSeenToSb(sb, playerParam.Value.SteamId);

            var connectionLog = Plugin.LogEntries;
            var playerInfo = connectionLog.GetInfoForPlayer(playerParam.Value.SteamId);

            sb.AppendLine("Last known data vs current");
            sb.AppendLine("--------------------------");
            sb.AppendLine("Name: " + playerInfo.LastName);
            Utilities.AddSessionToSb(sb, playerInfo.LastSeen, PlayerSnapshotFactory.Create(playerInfo.SteamId, DateTime.Now), "");
            sb.AppendLine();

            sb.AppendLine("Total playtime: " + Utilities.FormatTime(Utilities.CalcTotalPlayTime(playerInfo)));
            sb.AppendLine("World playtime: " + Utilities.FormatTime(Utilities.CalcWorldPlayTime(playerInfo)));
            sb.AppendLine("--------------------------");
            sb.AppendLine();

            foreach (var entry in playerInfo.GetEntries()) {
                Utilities.AddPlayTimeToSb(sb, entry);
            }

            Utilities.Respond(sb, "Playtime", "Player " + playerParam.Value.Name, Context);
        }

        [Command("ips", "Shows all known IPs of the Player.")]
        [Permission(MyPromoteLevel.Admin)]
        public void IPs(string nameIdOrSteamId) {

            Utilities.PlayerParam? playerParam = Utilities.FindPlayerParam(nameIdOrSteamId);

            if (!playerParam.HasValue) {
                Context.Respond("Player not found!");
                return;
            }

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;
            var playerInfo = connectionLog.GetInfoForPlayer(playerParam.Value.SteamId);

            foreach (var entry in playerInfo.GetEntries())
                sb.AppendLine(entry.Name + " " + entry.IP +" "+ entry.GetLastDateTime().ToString("yyyy-MM-dd  HH:mm:ss"));

            Utilities.Respond(sb, "IPs", "Player " + playerParam.Value.Name, Context);
        }

        [Command("names", "Outputs all known names of the selected Player.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void Names(string nameIdOrSteamId) {

            Utilities.PlayerParam? playerParam = Utilities.FindPlayerParam(nameIdOrSteamId);

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

            Utilities.Respond(sb, "Playernames", "All known names of Player " + playerParam.Value.Name, Context);
        }

        [Command("sessions", "Outputs the sessions of the specified player.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void Sessions(string nameIdOrSteamId) {

            Utilities.PlayerParam? playerParam = Utilities.FindPlayerParam(nameIdOrSteamId);

            if (!playerParam.HasValue) {
                Context.Respond("Player not found!");
                return;
            }

            StringBuilder sb = new StringBuilder();

            var connectionLog = Plugin.LogEntries;
            var playerInfo = connectionLog.GetInfoForPlayer(playerParam.Value.SteamId);

            var config = Plugin.Config;

            Utilities.AddLastSeenToSb(sb, playerParam.Value.SteamId);

            sb.AppendLine("Last known data vs current");
            sb.AppendLine("--------------------------");
            sb.AppendLine("Name: " + playerInfo.LastName);
            Utilities.AddSessionToSb(sb, playerInfo.LastSeen, PlayerSnapshotFactory.Create(playerInfo.SteamId, DateTime.Now), "");
            sb.AppendLine();

            sb.AppendLine("Total playtime: " + Utilities.FormatTime(Utilities.CalcTotalPlayTime(playerInfo)));
            sb.AppendLine("World playtime: " + Utilities.FormatTime(Utilities.CalcWorldPlayTime(playerInfo)));
            sb.AppendLine();

            sb.AppendLine("Sessions");
            sb.AppendLine("--------------------------");
            sb.AppendLine();
            foreach (var entry in playerInfo.GetEntries()) {

                if(config.ShowIpInSessionsCommand)
                    sb.AppendLine(entry.Name+" "+entry.IP);
                else 
                    sb.AppendLine(entry.Name);

                Utilities.AddPlayTimeToSb(sb, entry);

                Utilities.AddSessionToSb(sb, entry.Login, entry.Logout, "");

                if(entry.LogoutThroughSessionUnload)
                    sb.AppendLine("Logged off through Serverrestart");

                sb.AppendLine();
            }

            Utilities.Respond(sb, "Session", "Player " + playerParam.Value.Name, Context);
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

                    if(Utilities.Matches(knownName, namePattern)) {

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

            Utilities.Respond(sb, "Found Players", "Players whose name matches case insensitive", Context);
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
                        Utilities.AddPlayTimeToSb(sb, entry);

                        count++;

                        break;
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("Found a total of " + count + " unique players!");

            Utilities.Respond(sb, "Found Players", "Played on " + lookupDate.ToString("yyyy-MM-dd"), Context);
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
                        Utilities.AddPlayTimeToSb(sb, entry);

                        count++;

                        break;
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("Found a total of " + count + " unique players!");

            Utilities.Respond(sb, "Found Players", "Played on " + lookupDate.ToString("yyyy-MM-dd HH:mm:ss"), Context);
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

            Utilities.Respond(sb, "Potential Multiaccounts", "Shows who shared IPs and when", Context);
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
                    sb.AppendLine("   Total Playtime: " + Utilities.FormatTime(Utilities.CalcTotalPlayTime(playerInfo)));
                    sb.AppendLine("   World Playtime: " + Utilities.FormatTime(Utilities.CalcWorldPlayTime(playerInfo)));

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
                    sb.AppendLine("   Total Playtime: " + Utilities.FormatTime(Utilities.CalcTotalPlayTime(playerInfo)));
                    sb.AppendLine("   World Playtime: " + Utilities.FormatTime(Utilities.CalcWorldPlayTime(playerInfo)));

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

            Utilities.Respond(sb, "Suspicious Accounts", "Shows accounts who have higher PCU than when they were last seen.", Context);
        }
    }
}
