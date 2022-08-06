using ALE_ConnectionLog.model;
using ALE_Core.Cooldown;
using ALE_Core.Utils;
using Sandbox.Game.World;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace ALE_ConnectionLog {
    [Category("connectlog admin")]
    public class ConnectionLogAdminCommands : CommandModule {

        public ConnectionLogPlugin Plugin => (ConnectionLogPlugin)Context.Plugin;

        [Command("save", "Saves the log immediately.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Save() {
            Plugin.SaveLogEntriesAsync();
            Context.Respond("Log saved!");
        }

        [Command("reload", "Reloads the file from file system.")]
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

        [Command("wipe all", "Deletes all Logs.")]
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

        [Command("wipe world", "Deletes all Session and World Data (Keeps Total Playtime after server reset).")]
        [Permission(MyPromoteLevel.Owner)]
        public void CleanWorld() {

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if (!CheckConformation(steamId, "wipe world"))
                return;

            var connectionLog = Plugin.LogEntries;
            int countPlayers = connectionLog.GetPlayerCount();

            connectionLog.ClearWorld();

            LogEveryoneInAgain(connectionLog);

            Context.Respond("Deleted Sessions for " + countPlayers + " players!");
        }

        [Command("wipe sessions", "Deletes all Session Data (you will keep last login information and playtimes).")]
        [Permission(MyPromoteLevel.Owner)]
        public void CleanSessions() {

            var steamId = new SteamIdCooldownKey(PlayerUtils.GetSteamId(Context.Player));

            if (!CheckConformation(steamId, "wipe sessions"))
                return;

            var connectionLog = Plugin.LogEntries;
            int countPlayers = connectionLog.GetPlayerCount();

            connectionLog.ClearSessions();

            LogEveryoneInAgain(connectionLog);

            Context.Respond("Deleted Sessions for " + countPlayers + " players!");
        }

        [Command("logoutall", "Logs all Players out.")]
        [Permission(MyPromoteLevel.Admin)]
        public void LogoutAll() {
            Plugin.LogEveryoneOut();
            Context.Respond("Done!");
        }

        [Command("open", "Finds all open Sessions.")]
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
                        Utilities.AddPlayTimeToSb(sb, entry);

                        count++;
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine("Found a total of "+count+" sessions!");

            Utilities.Respond(sb, "Open Sessions", "Sessions without Logout", Context);
        }

        [Command("fix", "Fixes all open sessions that are not current.")]
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
    }
}
