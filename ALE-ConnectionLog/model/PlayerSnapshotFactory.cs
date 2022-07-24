using ALE_Core.Utils;
using Sandbox.Game.World;

namespace ALE_ConnectionLog.model {
    
    public class PlayerSnapshotFactory {

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
    }
}
