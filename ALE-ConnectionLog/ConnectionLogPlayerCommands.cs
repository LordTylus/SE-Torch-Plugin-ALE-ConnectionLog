using ALE_ConnectionLog.model;
using System;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace ALE_ConnectionLog {
    [Category("connectlog")]
    public class ConnectionLogPlayerCommands : CommandModule {

        public ConnectionLogPlugin Plugin => (ConnectionLogPlugin)Context.Plugin;

        [Command("status", "Shows your Playtime, and Connections with optionally IP.")]
        [Permission(MyPromoteLevel.None)]
       public void PlayTime(bool showIP = false) {

            if(Context.Player == null) {
                Context.Respond("Not from console!");
                return;
            }

            StringBuilder sb = new StringBuilder();

            Utilities.AddLastSeenToSb(sb, Context.Player.SteamUserId);

            var connectionLog = Plugin.LogEntries;
            var playerInfo = connectionLog.GetInfoForPlayer(Context.Player.SteamUserId);

            sb.AppendLine("Last known data vs current");
            sb.AppendLine("--------------------------");
            sb.AppendLine("Name: " + playerInfo.LastName);
            Utilities.AddSessionToSb(sb, playerInfo.LastSeen, PlayerSnapshotFactory.Create(playerInfo.SteamId, DateTime.Now), "");
            sb.AppendLine();

            sb.AppendLine("Total playtime: " + Utilities.FormatTime(Utilities.CalcTotalPlayTime(playerInfo)));
            sb.AppendLine("--------------------------");
            sb.AppendLine();

            foreach (var entry in playerInfo.GetEntries()) {
                Utilities.AddPlayTimeToSb(sb, entry, showIP);
            }

            Utilities.Respond(sb, "Playtime", "Player " + Context.Player.DisplayName, Context);
        }
    }
}
