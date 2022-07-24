using System.Windows;
using System.Windows.Controls;

namespace ALE_ConnectionLog {
    public partial class ConnectionLogControl : UserControl {

        private ConnectionLogPlugin Plugin { get; }

        private ConnectionLogControl() {
            InitializeComponent();
        }

        public ConnectionLogControl(ConnectionLogPlugin plugin) : this() {
            Plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e) {
            Plugin.Save();
        }
    }
}
