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

        [Command("admin save", "Saves the log immediately")]
        [Permission(MyPromoteLevel.Admin)]
        public void Save() {
            Plugin.SaveLogEntriesAsync();
            Context.Respond("Log saved!");
        }

        [Command("admin wipe", "Deletes all Logs.")]
        [Permission(MyPromoteLevel.Owner)]
        public void Wipe() {

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if (!CheckConformation(steamId, "wipe"))
                return;

            var connectionLog = Plugin.LogEntries;
            int countPlayers = connectionLog.GetPlayerCount();

            connectionLog.Clear();

            /* Now log everyone that is online in again */

            var result = new List<MyPlayer>(MySession.Static.Players.GetOnlinePlayers()
                .Where(x => x.IsRealPlayer && !string.IsNullOrEmpty(x.DisplayName)));

            foreach (MyPlayer player in result) {

                var Identity = player.Identity;
                ulong SteamId = MySession.Static.Players.TryGetSteamId(Identity.IdentityId);
                string ip = Plugin.GetIpOfSteamId(SteamId);

                connectionLog.LoginPlayer(SteamId, player.DisplayName, ip, Plugin.Config);
            }

            Context.Respond("Deleted Logs for "+ countPlayers + " players!");
        }

        [Command("admin logoutall", "Logs all Players out.")]
        [Permission(MyPromoteLevel.Admin)]
        public void LogoutAll() {
            Plugin.LogEveryoneOut();
            Context.Respond("Done!");
        }

        [Command("playtime", "Outputs the total Playtimes of the specified player.")]
        [Permission(MyPromoteLevel.Moderator)]
        public void PlayTime(string nameIdOrSteamId) {

            PlayerParam? playerParam = FindPlayerParam(nameIdOrSteamId);

            if(!playerParam.HasValue) {
                Context.Respond("Player not found!");
                return;
            }

            StringBuilder sb = new StringBuilder();

            AddLastSeenToSb(sb, playerParam.Value.SteamId);

            var connectionLog = Plugin.LogEntries;
            var playerInfo = connectionLog.GetInfoForPlayer(playerParam.Value.SteamId);

            sb.AppendLine("Total play time: " + (int) (playerInfo.TotalPlayTime/60) +" minutes.");
            sb.AppendLine();

            foreach (var entry in playerInfo.GetEntries()) {
                AddPlayTimeToSb(sb, entry);
            }

            Respond(sb, "Play Time", "Player " + playerParam.Value.Name);
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

            foreach (var entry in playerInfo.GetEntries()) {

                if(config.ShowIpInSessionsCommand)
                    sb.AppendLine(entry.Name+" "+entry.IP);
                else 
                    sb.AppendLine(entry.Name);

                AddPlayTimeToSb(sb, entry);
                
                if(entry.Logout == null) {

                    sb.AppendLine("Identity: " + entry.Login.IdentityId);
                    sb.AppendLine("Faction: " + entry.Login.Faction);
                    sb.AppendLine("Blocks: " + entry.Login.BlockCount);
                    sb.AppendLine("PCU: " + entry.Login.PCU);
                    sb.AppendLine("Grids: " + entry.Login.GridCount);

                } else {

                    if(entry.Login.IdentityId != entry.Logout.IdentityId)
                        sb.AppendLine("Identity: " + entry.Login.IdentityId + " -> " + entry.Logout.IdentityId);
                    else
                        sb.AppendLine("Identity: " + entry.Login.IdentityId);

                    if (entry.Login.Faction != entry.Logout.Faction)
                        sb.AppendLine("Faction: " + entry.Login.Faction + " -> " + entry.Logout.Faction);
                    else
                        sb.AppendLine("Faction: " + entry.Login.Faction);

                    if (entry.Login.BlockCount != entry.Logout.BlockCount)
                        sb.AppendLine("Blocks: " + entry.Login.BlockCount + " -> " + entry.Logout.BlockCount);
                    else
                        sb.AppendLine("Blocks: " + entry.Login.BlockCount);

                    if (entry.Login.PCU != entry.Logout.PCU)
                        sb.AppendLine("PCU: " + entry.Login.PCU + " -> " + entry.Logout.PCU);
                    else
                        sb.AppendLine("PCU: " + entry.Login.PCU);

                    if (entry.Login.GridCount != entry.Logout.GridCount)
                        sb.AppendLine("Grids: " + entry.Login.GridCount + " -> " + entry.Logout.GridCount);
                    else
                        sb.AppendLine("Grids: " + entry.Login.GridCount);
                }

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

            foreach (var playerInfo in connectionLog.GetPlayerInfos()) {

                foreach(var knownName in playerInfo.GetNames()) {

                    if(Matches(knownName, namePattern)) {

                        sb.AppendLine("Found Steam User: "+playerInfo.SteamId+" with name "+ knownName);

                        MyIdentity identity = PlayerUtils.GetIdentityByNameOrId(playerInfo.SteamId.ToString());
                        
                        if(identity != null) {

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
                        break;
                    }
                }
            }

            Respond(sb, "Found Players", "Players whose name matches case insensitive");
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
                bool hasOutputIp = false;

                foreach(var dateEntry in ipEntry.Value) {

                    string date = dateEntry.Key;
                    HashSet<ulong> steamIds = dateEntry.Value;

                    if (steamIds.Count > 1) {

                        if(!hasOutputIp) {
                            sb.AppendLine(ip);
                            sb.AppendLine("   "+date);

                            foreach(ulong steamId in steamIds) {

                                long identityId = MySession.Static.Players.TryGetIdentityId(steamId);

                                MyIdentity identity = PlayerUtils.GetIdentityById(identityId);

                                if(identity == null)
                                    sb.AppendLine("      " + steamId);
                                else
                                    sb.AppendLine("      " + steamId +" "+identity.DisplayName+" #"+identity.IdentityId);
                            }
                        }
                    }
                }
            }

            Respond(sb, "Potential Multiaccounts", "Shows who shared IPs and when");
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

            Context.Respond("Are you sure you want to continue? Enter the command again within " + Plugin.CooldownConfirmationSeconds + " seconds to confirm WIPE of ALL connection logs.");

            return false;
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
