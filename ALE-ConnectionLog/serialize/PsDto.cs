using System;

namespace ALE_ConnectionLog {

    public class PsDto {

        //IdentityID
        public long IId { get; set; }

        //PCU
        public int PCU { get; set; }

        //Block Count
        public int Blk { get; set; }

        //Grid Count
        public int GC { get; set; }

        //Faction
        public string F { get; set; }

        //Snapshot Time
        public DateTime ST { get; set; }
    }
}
