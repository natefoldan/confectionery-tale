using Godot;

namespace ConfectioneryTale.scripts;

[GlobalClass]
public partial class SavedPortalData : Resource {
    [Export] public string PortalId { get; set; }
    [Export] public bool Cleansed { get; set; }

    public SavedPortalData() {}
    
    public SavedPortalData(string id, bool cleansed) {
        PortalId = id;
        Cleansed = cleansed;
    }
}