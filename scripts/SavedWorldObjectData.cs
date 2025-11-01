using Godot;

namespace ConfectioneryTale.scripts;

[GlobalClass]
public partial class SavedWorldObjectData : Resource {
    [Export] public string WorldObjectId { get; set; }
    [Export] public bool Obtained { get; set; }
    [Export] public bool Cracked { get; set; }

    public SavedWorldObjectData() {}
    
    public SavedWorldObjectData(string id, bool obtained, bool cracked) {
        WorldObjectId = id;
        Obtained = obtained;
        Cracked = cracked;
    }
}