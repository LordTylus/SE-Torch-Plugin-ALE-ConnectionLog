using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace ALE_ConnectionLog {
    [Category("ConnectionLog")]
    public class ConnectionLogCommands : CommandModule {

        public ConnectionLogPlugin Plugin => (ConnectionLogPlugin)Context.Plugin;

        [Command("save", "Saves the log immediately")]
        [Permission(MyPromoteLevel.Moderator)]
        public void Save() {
            Plugin.SaveLogEntriesAsync();
            Context.Respond("Log saved!");
        }
    }
}
