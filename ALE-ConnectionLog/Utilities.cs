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

    public class Utilities {

        public static void AddSessionToSb(StringBuilder sb, PlayerSnapshot referenceSnapshot, PlayerSnapshot nowSnapshot, string prefix) {

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

        public static void AddLastSeenToSb(StringBuilder sb, ulong steamId) {

            long identityId = MySession.Static.Players.TryGetIdentityId(steamId);
            var identity = MySession.Static.Players.TryGetIdentity(identityId);

            if (identity == null)
                return;

            sb.AppendLine("Last Login: "+ identity.LastLoginTime.ToString("yyyy-MM-dd  HH:mm:ss"));
            sb.AppendLine("Last Logout: " + identity.LastLoginTime.ToString("yyyy-MM-dd  HH:mm:ss"));
            sb.AppendLine();
        }

        public static void AddPlayTimeToSb(StringBuilder sb, model.ConnectionPlayerInfo.ConnectionEntry entry) {
            
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

        public static void Respond(StringBuilder sb, string title, string subtitle, CommandContext Context) {

            if (Context.Player == null) {

                Context.Respond(title);
                Context.Respond(subtitle);
                Context.Respond(sb.ToString());

            } else {

                ModCommunication.SendMessageTo(new DialogMessage(title,
                    subtitle, sb.ToString()), Context.Player.SteamUserId);
            }
        }

        public static PlayerParam? FindPlayerParam(string nameIdOrSteamId) {

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

        public struct PlayerParam {
            
            public string Name;
            public ulong SteamId;

            public PlayerParam(string name, ulong steamId) {
                Name = name;
                SteamId = steamId;
            }
        }
    }
}
