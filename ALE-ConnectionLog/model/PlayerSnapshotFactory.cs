using ALE_Core.Utils;
using Sandbox.Game.World;
using System;

namespace ALE_ConnectionLog.model {
    
    public class PlayerSnapshotFactory {

        public static PlayerSnapshot CreateEmpty() {

            long IdentityID = 0;

            int PCU = 0;
            int Blocks = 0;
            int GridCount = 0;

            string FactionTag = "";

            return new PlayerSnapshot(IdentityID, PCU, Blocks, GridCount, FactionTag);
        }

        public static PlayerSnapshot Create(ulong SteamId) {

            long IdentityID = MySession.Static.Players.TryGetIdentityId(SteamId);

            if (IdentityID == 0)
                return null;

            MyIdentity Identity = MySession.Static.Players.TryGetIdentity(IdentityID);
            int PCU = Identity.BlockLimits.PCUBuilt;
            int Blocks = Identity.BlockLimits.BlocksBuilt;
            int GridCount = Identity.BlockLimits.BlocksBuiltByGrid.Count;

            string FactionTag = FactionUtils.GetPlayerFactionTag(IdentityID);

            return new PlayerSnapshot(IdentityID, PCU, Blocks, GridCount, FactionTag);
        }

        internal static PlayerSnapshot Create(PlayerSnapshot login) {

            if (login == null)
                return null;

            long IdentityID = login.IdentityId;

            int PCU = login.PCU;
            int Blocks = login.BlockCount;
            int GridCount = login.GridCount;

            string FactionTag = login.Faction;

            return new PlayerSnapshot(IdentityID, PCU, Blocks, GridCount, FactionTag);
        }
    }
}
