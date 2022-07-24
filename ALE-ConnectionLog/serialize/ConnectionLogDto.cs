using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Torch;

namespace ALE_ConnectionLog.serialize {
    public class ConnectionLogDto : ViewModel {

        private List<CpiDto> _playerInfosAsList = new List<CpiDto>();

        public List<CpiDto> CLE { get => _playerInfosAsList; set => SetValue(ref _playerInfosAsList, value); }
    }
}