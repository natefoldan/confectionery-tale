using Godot;

namespace ConfectioneryTale.scripts;

public partial class InventoryExtracts : Control {
    [Signal] public delegate void InventorySortedEventHandler(int sortType);
    
    private void SelectSortOption(int index) {
        EmitSignal(SignalName.InventorySorted, index);
    }
}