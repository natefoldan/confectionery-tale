using System.Collections.Generic;
using Godot;

namespace ConfectioneryTale.scripts;

public partial class PlayerStats : TextureRect {
    // [Signal] public delegate void PlayerStatMouseEnterEventHandler(string stat); //delete
    
    private TooltipHandler tooltips;
    private List<(string stat, TextureRect tooltip)> allPlayerStatTooltips;
    
    public override void _Ready() {
        tooltips = GetNode<TooltipHandler>("/root/TooltipHandler");
        BuildAllPlayerStatTooltipsList();
    }

    private void BuildAllPlayerStatTooltipsList() {
        var statsContainer = GetNode<GridContainer>("Stats");
        if (statsContainer == null) { return; }
        allPlayerStatTooltips = new List<(string stat, TextureRect tooltip)>();
        
        //GetChildren returns a Godot.Collections.Array
        foreach (Node child in statsContainer.GetChildren()) {
            if (child is Control statControl) {
                if (statControl.HasNode("StatTooltip") && statControl.GetNode<TextureRect>("StatTooltip") is TextureRect tooltip) {
                    var parentName = tooltip.GetParent<Control>().Name;
                    allPlayerStatTooltips.Add((parentName, tooltip));
                }
            }
        }
        HideAllPlayerStatTooltips();
    }
    
    private void HideAllPlayerStatTooltips() {
        foreach (var t in allPlayerStatTooltips) { t.tooltip.Hide(); }
    }
    
    private void MouseEnterStat(string stat) {
        foreach (var t in allPlayerStatTooltips) {
            if (!t.stat.Equals(stat)) { continue; }
            tooltips.ShowPlayerStatTooltip(stat, t);
        }
    }
    
    private void MouseExitStat() {
        HideAllPlayerStatTooltips();
    }
}