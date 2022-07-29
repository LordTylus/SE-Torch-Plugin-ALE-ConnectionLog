using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Torch;

namespace ALE_ConnectionLog.serialize {
    public class ConnectionLogDto : ViewModel {

        private DateTime _dateTime = DateTime.Now;

        private List<CpiDto> _playerInfosAsList = new List<CpiDto>();

        public DateTime LSV { get => _dateTime; set => SetValue(ref _dateTime, value); }
        public List<CpiDto> CLE { get => _playerInfosAsList; set => SetValue(ref _playerInfosAsList, value); }
    }
}