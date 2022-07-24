using System;

namespace ALE_ConnectionLog.model {

    public class PlayerSnapshot {

        public long IdentityId { get; }

        public int PCU { get; }

        public int BlockCount { get; }

        public int GridCount { get; }

        public string Faction { get; }

        public DateTime SnapshotTime { get; }

        public PlayerSnapshot(long IdentityId, int PCU, int BlockCount, int GridCount, string Faction) 
            : this(IdentityId, PCU, BlockCount, GridCount, Faction, DateTime.Now) {
        }

        public PlayerSnapshot(long IdentityId, int PCU, int BlockCount, int GridCount, string Faction, DateTime SnapshotTime) {
            this.IdentityId = IdentityId;
            this.PCU = PCU;
            this.BlockCount = BlockCount;
            this.GridCount = GridCount;
            this.Faction = Faction;
            this.SnapshotTime = SnapshotTime;
        }
    }
}
