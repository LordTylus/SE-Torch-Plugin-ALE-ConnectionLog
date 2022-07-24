using Torch;

namespace ALE_ConnectionLog {
    public class ConnectionLogConfig : ViewModel {

        private int _keepLogMaxDays = 14;
        private int _keepMaxAmountEntriesPerPlayer = 100;
        private bool _saveIPs = true;
        private int _autosave_interval_minutes = 5;
        
        public int KeepLogMaxDays { get => _keepLogMaxDays; set => SetValue(ref _keepLogMaxDays, value); }
        public int KeepMaxAmountEntriesPerPlayer { get => _keepMaxAmountEntriesPerPlayer; set => SetValue(ref _keepMaxAmountEntriesPerPlayer, value); }
        public int AutosaveIntervalMinutes { get => _autosave_interval_minutes; set => SetValue(ref _autosave_interval_minutes, value); }
        public bool SaveIPs { get => _saveIPs; set => SetValue(ref _saveIPs, value); }
    }
}
