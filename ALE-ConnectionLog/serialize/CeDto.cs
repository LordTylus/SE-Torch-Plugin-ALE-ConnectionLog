using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Torch;

namespace ALE_ConnectionLog.serialize {
    public class CeDto : ViewModel {
        
        public string IP { get; set; }
        public string Name { get; set; }
        
        public bool LSU { get; set; }

        public PsDto Login { get; set; }
        public PsDto Logout { get; set; }
    }
}