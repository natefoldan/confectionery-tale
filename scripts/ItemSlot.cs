using Godot;

namespace ConfectioneryTale.scripts;

public partial class ItemSlot : TextureRect {
    [Signal] public delegate void ItemSlotClickedEventHandler(ItemSlot slot, BaseExtract extract, BaseMaterial material);
    [Signal] public delegate void EquippedExtractClickedEventHandler(ItemSlot slot, BaseExtract extract);
    private TooltipHandler tooltips;
    private BaseExtract extract;
    private BaseMaterial material; //probably rename to displayedMaterial for clarity
    private bool full;
    private bool isExtract;
    private bool isMaterial;
    private NinePatchRect thisSlotTooltip;
    private Label ownedLabel;
    
    private TextureButton itemButton;
    
    public override void _Ready() {
        tooltips = GetNode<TooltipHandler>("/root/TooltipHandler");
        thisSlotTooltip = GetNode<NinePatchRect>("Tooltip");
        tooltips.HideExtractTooltip(thisSlotTooltip);
        itemButton = GetNode<TextureButton>("Item");
        ownedLabel = GetNode<Label>("Owned");
        ownedLabel.Text = "";
    }
	
    public void AddNewExtract(BaseExtract what) {
        extract = what;
        full = true;
    }
    
    public void AddNewMaterial(BaseMaterial what) {
        material = what;
        full = true;
    }
    
    public void Reset() {
        extract = null;
        material = null;
        full = false;

        // Clear and hide all visual components
        if (itemButton != null) { 
            itemButton.TextureNormal = null; 
            // itemButton.Disabled = true; // Optionally disable the button when empty
        }

        if (ownedLabel != null) { 
            ownedLabel.Text = "";
            ownedLabel.Visible = false;
        }

        // GD.Print($"Slot {Name}: Reset to empty.");
    }
    
    public void DisplayExtract(BaseExtract itemToDisplay) {
        extract = itemToDisplay;
        full = (extract != null);
        if (itemButton != null) { if (extract != null) itemButton.TextureNormal = full ? extract.DisplayTexture : null; }
        isExtract = true;
    }
    
    public void DisplayMaterial(BaseMaterial itemToDisplay) {
        material = itemToDisplay;
        isMaterial = true;
        //determine if the slot should be visually "full" based on count
        full = (material != null && material.CurrentOwned > 0);

        if (full) {
            //set visual components and make them visible
            if (itemButton != null) {
                itemButton.TextureNormal = material.DisplayTexture;
                // itemButton.Disabled = false; //enable if it was disabled when empty
            }
            
            if (ownedLabel != null) {
                ownedLabel.Text = material.CurrentOwned.ToString();
                ownedLabel.Visible = true;
            }

            // GD.Print($"Slot {Name}: Displayed {material.Name} (Count: {material.CurrentOwned})");
        } else {
            // If material is null or its count is 0, reset the slot to an empty state.
            Reset(); 
        }
    }
    
    // public void DisplayMaterial(BaseMaterial itemToDisplay) { //original -works
    //     material = itemToDisplay;
    //     full = (material != null);
    //     if (itemButton != null) { if (material != null) itemButton.TextureNormal = full ? material.DisplayTexture : null; }
    //     isMaterial = true;
    //     ownedLabel.Text = itemToDisplay.CurrentOwned < 1 ? "" : itemToDisplay.CurrentOwned.ToString(); //trim
    // }
    
    public BaseExtract GetExtract() { return extract; }
    public BaseMaterial GetInventoryMaterial() { return material; } //can't use GetMaterial declaration
    public bool GetFull() { return full; }
    public bool GetIsExtract() { return isExtract; }
    public bool GetIsMaterial() { return isMaterial; }
    
    private void ClickItem() {
        if (!full) { return; }

        if (isExtract) {
            if (extract.Equipped) {
                EmitSignal(SignalName.EquippedExtractClicked, this, extract);
                return;
            }
        }
        
        EmitSignal(SignalName.ItemSlotClicked, this, extract, material);
    }
	   
    private void MouseEnterItem() {
        if (!full) {
            GetNode<TextureButton>("Item").MouseDefaultCursorShape = CursorShape.Arrow;
            return;
        }
        GetNode<TextureButton>("Item").MouseDefaultCursorShape = CursorShape.PointingHand;

        if (isExtract) {
            tooltips.ShowExtractTooltip(extract, thisSlotTooltip, GlobalPosition);
        } else if (isMaterial) {
            tooltips.ShowMaterialTooltip(material, thisSlotTooltip, GlobalPosition);
        }
    }
    
    private void MouseExitItem() {
        if (isExtract) {
            tooltips.HideExtractTooltip(thisSlotTooltip);
        } else if (isMaterial) {
            tooltips.HideMaterialTooltip(thisSlotTooltip);
        }
    }
}