﻿using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Torch;

namespace ALE_ConnectionLog.serialize {
    public class CpiDto : ViewModel {
        public ulong SID { get; set; }
        public long TPT { get; set; } = 0;
        public long WPT { get; set; } = 0;

        public string LN { get; set; }

        public PsDto LPS { get; set; }

        private HashSet<string> _allKnownNames = new HashSet<string>();
        private List<CeDto> _connectionEntries = new List<CeDto>();

        public HashSet<string> AKN { get => _allKnownNames; set => SetValue(ref _allKnownNames, value); }
        public List<CeDto> CE { get => _connectionEntries; set => SetValue(ref _connectionEntries, value); }
    }
}