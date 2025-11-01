using Godot;

namespace ConfectioneryTale.scripts;

[GlobalClass]
public partial class SavedItemData : Resource {
    [Export] public string ItemId { get; set; }
    [Export] public double CurrentOwned { get; set; }
    [Export] public double TotalFound { get; set; }

    public SavedItemData() {}
    
    public SavedItemData(string id, double owned, double found) {
        ItemId = id;
        CurrentOwned = owned;
        TotalFound = found;
    }
}