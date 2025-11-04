using Godot;

namespace ConfectioneryTale.scripts;

public partial class DistillerySlot : TextureRect {
    [Signal] public delegate void DistilleryExtractClickedEventHandler(BaseExtract item, DistillerySlot slot);
    private TooltipHandler tooltips;
    private BaseExtract item;
    private bool full;
    private NinePatchRect thisSlotTooltip;
    private TextureButton itemButton;
    public bool outputSlot { get; set; }
    public int slotTier { get; set; }
    
    public override void _Ready() {
        tooltips = GetNode<TooltipHandler>("/root/TooltipHandler");
        thisSlotTooltip = GetNode<NinePatchRect>("Tooltip");
        tooltips.HideExtractTooltip(thisSlotTooltip);
        itemButton = GetNode<TextureButton>("Item");
    }
	
    public void AddExtractToSlot(BaseExtract what) {
        // GD.Print("adding: " + what.Name);
        item = what;
        full = true;
        DisplayItem(what);
    }
    
    public void Reset() {
        GetNode<TextureButton>("Item").TextureNormal = null;
        item = null;
        full = false;
    }
	   
    private void DisplayItem(BaseExtract itemToDisplay) {
        item = itemToDisplay;
        full = (item != null);
        if (itemButton != null) { if (item != null) itemButton.TextureNormal = full ? item.DisplayTexture : null; }
    }
    
    public BaseExtract GetItem() { return item; }
    public bool GetFull() { return full; }
    
    private void ClickItem() {
        if (!full) { return; }
        EmitSignal(SignalName.DistilleryExtractClicked, item, this);
    }
	   
    private void MouseEnterItem() {
        if (!full) {
            GetNode<TextureButton>("Item").MouseDefaultCursorShape = CursorShape.Arrow;
            return;
        }
        GetNode<TextureButton>("Item").MouseDefaultCursorShape = CursorShape.PointingHand;
        tooltips.ShowExtractTooltip(item, thisSlotTooltip, GlobalPosition);
    }
    
    private void MouseExitItem() {
        tooltips.HideExtractTooltip(thisSlotTooltip);
    }
}