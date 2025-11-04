using System;
using System.Collections.Generic;
using Godot;

namespace ConfectioneryTale.scripts;

public partial class TooltipHandler : Node {
    private List<RichTextLabel> allStatLabels;
    private List<Label> allDescriptionLabels;
    private List<(string statName, float value)> statsList;
    
    //new colors
    private string colorBlackString = "[color=#000000]";
    private string colorWhiteString = "[color=#FFFFFF]";
    private string colorGreyString = "[color=#595959]";
    private string colorYellowString = "[color=#ffef46]";
    private string colorGreenString = "[color=#367a14]";
    private string colorBlueString = "[color=#135a91]";
    private string colorPurpleString = "[color=#851e9b]";
    private string colorOrangeString = "[color=#fc7c14]";
    private string colorPinkString = "[color=#ff27de]"; //ff00b4
    
    // private string colorGreenString = "[color=#1b5e20]"; //original
    // private string colorGreenString = "[color=#59c15e]"; //prev
    // private string colorBlueString = "[color=#0d47a1]"; //prev
    // private string colorPurpleString = "[color=#620ea8]"; //prev
    // private string colorOrangeString = "[color=#e65100]"; //prev
    private string colorTealString = "[color=#00a4ae]"; //dark
    private string colorTealStringTwo = "[color=#005c65]";
    // private string colorTealString = "[color=#b3ebf1]"; //light

    private Color colorBlack = new Color(.0f, .0f, .0f);
    private Color colorGrey = new Color(.349f, .349f, .349f);
    private Color colorGreen = new Color(.212f, .478f, .078f);
    private Color colorBlue = new Color(.075f, .353f, .569f);
    private Color colorPurple = new Color(.522f, .118f, .608f);
    private Color colorOrange = new Color(.988f, .486f, .078f);
    private Color colorPink = new Color(1f, .153f, .871f);
    
    private Color colorYellow = new Color(.984f, .847f, .208f);
    private Color colorWhite = new Color(1f, 1f, 1f);
    // private Color colorGreen = new Color(.106f, .369f, .125f); //prev
    // private Color colorBlue = new Color(.051f, .278f, .631f); //prev
    // private Color colorPurple = new Color(.385f, .057f, .66f); //prev
    // private Color colorOrange = new Color(.902f, .318f, .0f); //prev
    private Color[] colorArray = new Color[5];
    
    public override void _Ready() {
        colorArray[0] = colorGrey;
        colorArray[1] = colorGreen;
        colorArray[2] = colorBlue;
        colorArray[3] = colorPurple;
        colorArray[4] = colorOrange;
        // colorArray[4] = colorPink;
    }

    public void ShowExtractTooltip(BaseExtract extract, NinePatchRect tooltip, Vector2 pos) {
        var statcontainer = tooltip.GetNode<VBoxContainer>("VBoxContainer");
        var tooltipGlobalPosition = pos;
        
        var baseLabel = new RichTextLabel();
        baseLabel.BbcodeEnabled = true;
        baseLabel.CustomMinimumSize = new Vector2(0, 35);
        baseLabel.AddThemeColorOverride("default_color", new Color(0f, 0f, 0f, 1f));
        baseLabel.Set("theme_override_font_sizes/normal_font_size", 22);
        
        allStatLabels = new List<RichTextLabel>();
        statsList = new List<(string statName, float value)>();

        AddStatToTooltip("Damage", extract.ExtractBaseDamage, extract.ExtractBaseDamageQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Pierce", extract.ExtractBasePierce, extract.ExtractBasePierceQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Crit Chance", extract.ExtractBaseCritChance, extract.ExtractBaseCritChanceQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Crit Damage", extract.ExtractBaseCritDamage, extract.ExtractBaseCritDamageQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Shield", extract.ExtractBaseShield, extract.ExtractBaseShieldQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Shield Regen", extract.ExtractBaseShieldRegen, extract.ExtractBaseShieldRegenQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Health", extract.ExtractBaseHealth, extract.ExtractBaseHealthQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Pickup Range", extract.ExtractBasePickupRange, extract.ExtractBasePickupRangeQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Speed", extract.ExtractBaseSpeed, extract.ExtractBaseSpeedQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Extract Drop", extract.ExtractBaseExtractDrop * .1f, extract.ExtractBaseExtractDropQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Sucrose Drop", extract.ExtractBaseSucroseDrop, extract.ExtractBaseSucroseDropQuality, baseLabel, statcontainer, statsList, allStatLabels);
        AddStatToTooltip("Exp Gain", extract.ExtractBaseExpGain, extract.ExtractBaseExpGainQuality, baseLabel, statcontainer, statsList, allStatLabels);
        
        var tierLabel = tooltip.GetNode<Label>("Tier");
        tierLabel.Show();
        // tierLabel.Text = "Tier " + extract.ModTier;
        tierLabel.Text = "T" + extract.ExtractTier;
        tierLabel.Set("theme_override_colors/font_color", colorArray[extract.ExtractTier - 1]);
        
        var potencyLabel = tooltip.GetNode<RichTextLabel>("Potency");
        potencyLabel.Show();
        var roundedPotency = Math.Ceiling(extract.ExtractQuality);
        
        // if (roundedPotency >= 100) { potencyLabel.Text = $"{colorOrangeString}PERFECT"; } //ORIGINAL
        // else if (roundedPotency >= 75) { potencyLabel.Text = $"Potency: {colorPurpleString}{roundedPotency}%"; }
        // else if (roundedPotency >= 50) { potencyLabel.Text = $"Potency: {colorBlueString}{roundedPotency}%"; }
        // else { potencyLabel.Text = $"Potency: {colorGreenString}{roundedPotency}%"; }
        
        // if (roundedPotency >= 100) { potencyLabel.Text = $"{colorPinkString}PERFECT"; }
        if (roundedPotency >= 100) { potencyLabel.Text = $"{colorOrangeString}PERFECT"; }
        else if (roundedPotency >= 75) { potencyLabel.Text = $"Potency: {colorPurpleString}{roundedPotency}%"; }
        else if (roundedPotency >= 50) { potencyLabel.Text = $"Potency: {colorGreenString}{roundedPotency}%"; }
        else { potencyLabel.Text = $"Potency: {colorGreyString}{roundedPotency}%"; }
        
        var nameLabel = tooltip.GetNode<Label>("Name");
        nameLabel.Text = extract.Name + " Extract";
        nameLabel.Set("theme_override_colors/font_color", colorArray[extract.ExtractTier - 1]);
        
        var sizeY = 120 + (statsList.Count * 28); //not a great way to do it, tier might have a different number of stats
        
        tooltip.Size = new Vector2(350, sizeY);
        
        var finalX = 37;
        var finalY = 37;
        // GD.Print(tooltipGlobalPosition.Y);
        if (tooltipGlobalPosition.X > 1478) { finalX = -297; } //probably needs adjustment
        if (tooltipGlobalPosition.Y > 782) { finalY += -sizeY; } 
        tooltip.SetPosition(new Vector2(finalX, finalY));
        
        tooltip.Show();
    }
    
    private void AddStatToTooltip(string statName, float statValue, float statQuality, RichTextLabel baseLabel, VBoxContainer statContainer, List<(string statName, float value)> statsList, List<RichTextLabel> allStatLabels) {
        if (statValue > 0) {
            if (baseLabel.Duplicate() is RichTextLabel statLabel) { //rename to statLabel for generality
                statsList.Add((statName, statValue)); //add to the list used for GetHighestStat
                
                // var qualityColor = colorBlackString; //ORIGINAL -delete
                // if (statQuality >= 100) { qualityColor = colorOrangeString; }
                // else if (statQuality >= 75) { qualityColor = colorPurpleString; }
                // else if (statQuality >= 50) { qualityColor = colorBlueString; }
                // else { qualityColor = colorGreenString; }
                
                var qualityColor = colorGreyString;
                // if (statQuality >= 100) { qualityColor = colorPinkString; }
                if (statQuality >= 100) { qualityColor = colorOrangeString; }
                else if (statQuality >= 75) { qualityColor = colorPurpleString; }
                else if (statQuality >= 50) { qualityColor = colorGreenString; }
                else { qualityColor = qualityColor; }
                
                // statLabel.Text = $"{statName}: {colorGreenString}+{statValue}"; //without quality percentage
                statLabel.Text = $"{statName}: {qualityColor}+{statValue} ({statQuality}%)";
                statContainer.AddChild(statLabel);
                allStatLabels.Add(statLabel); //add to the list for later freeing
            }
        }
    }

    public void HideExtractTooltip(NinePatchRect tooltip) {
        tooltip.Hide();
        if (allStatLabels == null) { return; }
        
        foreach (var statLabel in allStatLabels) {
            // if (statLabel == null) { continue; }
            if (!IsInstanceValid(statLabel)) { continue; }
            statLabel.QueueFree();
        }
        allStatLabels.Clear();
    }

    public void ShowPlayerStatTooltip(string stat, (string stat, TextureRect tooltip) t) {
        return;
        var levelLabel = t.tooltip.GetNode<Label>("VBoxContainer/Base");
        var weaponLabel = t.tooltip.GetNode<Label>("VBoxContainer/PlayerOrWeapon"); //weapon here
        var extractsLabel = t.tooltip.GetNode<Label>("VBoxContainer/Extracts");
        
        var statsList = PlayerStatTooltipHelper(stat)[0];

        levelLabel.Text = "+" + statsList.levelStat + " Base";
        levelLabel.Visible = !(statsList.levelStat <= 0);
        
        // extractsLabel.Text = "+" + statsList.extractStat + " Extracts"; //prev
        extractsLabel.Text = "+" + statsList.extractStat + " Extracts";
        if (stat.Equals("ExtractDrop")) { extractsLabel.Text = "+" + statsList.extractStat.ToString("F1") + " Extracts"; }
        extractsLabel.Visible = !(statsList.extractStat <= 0);
        
        weaponLabel.Text = "+" + statsList.weaponStat + " Weapon";
        weaponLabel.Visible = !(statsList.weaponStat <= 0);

        if (stat.Equals("ShieldRegen")) { levelLabel.Text = "Not moddable"; }
        
        t.tooltip.Show();
    }
    
    private List<(float levelStat, float extractStat, float weaponStat)> PlayerStatTooltipHelper (string stat) {
        var main = GetNode<Main>("/root/Main");
        
        double baseWeaponDamage = main.GetCurrentWeaponData().Damage;
        float skillMultiplier = main.GetSkillDamageEffect(main.GetCurrentWeaponData().Id);
        
        switch (stat) {
            case "Damage":
                return [(
                    (float) Math.Ceiling(main.GetPlayerLevelDamage()),
                    (float) Math.Ceiling(main.GetPlayerExtractDamage()),
                    (float) Math.Ceiling(baseWeaponDamage * skillMultiplier))];
            case "Health":
                return [(main.GetPlayerLevelHealth(), main.GetPlayerModHealth(), 0)];
            case "Pierce":
                return [(0, main.GetPlayerExtractPierce(), main.GetPlayerEquippedWeaponPierce())];
            case "Range":
                return [(main.GetPlayerLevelPickupRange(), main.GetPlayerModPickupRange(), 0)];
            case "CritChance":
                return [(0, main.GetPlayerExtractCritChance(), main.GetPlayerEquippedWeaponCritChance())];
            case "CritDamage":
                return [(0, main.GetPlayerExtractCritDamage(), main.GetPlayerEquippedWeaponCritDamage())];
            case "Speed":
                return [(main.GetPlayerLevelSpeed() / 100, main.GetPlayerModSpeed() / 100, 0)];
            case "Shield":
                return [(main.GetPlayerLevelShield(), main.GetPlayerModShield(), 0)];
            case "ShieldRegen":
                return [(main.GetPlayerLevelShieldRegen(), main.GetPlayerModShieldRegen(), 0)];
            case "ExtractDrop":
                // return [(main.GetPlayerLevelModDropChance() * .1, main.GetPlayerModModDropChance() * .1, 0)]; //prev -delete
                return [(main.GetPlayerLevelExtractDropChance(), main.GetEquippedExtractsDropChance(), 0)];
            case "SucroseDrop":
                return [(main.GetPlayerLevelSucroseDrop(), main.GetPlayerModSucroseDrop(), 0)];
            case "ExpDrop":
                return [(main.GetPlayerLevelExpDrop(), main.GetPlayerModExpDrop(), 0)];
            case "Slow":
                return [(main.GetPlayerLevelExpDrop(), main.GetPlayerModExpDrop(), 0)];
            case "Melt":
                return [(main.GetPlayerLevelExpDrop(), main.GetPlayerModExpDrop(), 0)];
            case "SlowResist":
                return [(main.GetPlayerLevelExpDrop(), main.GetPlayerModExpDrop(), 0)];
            case "MeltResist":
                return [(main.GetPlayerLevelExpDrop(), main.GetPlayerModExpDrop(), 0)];
        }
        return new List<(float levelStat, float extractStat, float weaponStat)>();
    }

    public void ShowMaterialTooltip(BaseMaterial material, NinePatchRect tooltip, Vector2 pos) {
        tooltip.GetNode<Label>("Tier").Hide();
        tooltip.GetNode<RichTextLabel>("Potency").Text = "Crafting Material";
        var nameLabel = tooltip.GetNode<Label>("Name");
        nameLabel.Text = material.Name;
        
        var statcontainer = tooltip.GetNode<VBoxContainer>("VBoxContainer");
        var descriptionLabel = new Label();

        descriptionLabel.Set("theme_override_colors/font_color", GetDecimalColor("black"));
        descriptionLabel.Text = material.Description;
        
        statcontainer.AddChild(descriptionLabel);
        allDescriptionLabels = new List<Label>();
        allDescriptionLabels.Add(descriptionLabel); //add to the list for later freeing
        var tooltipGlobalPosition = pos;

        var width = 300;
        var height = 150;
        
        tooltip.Size = new Vector2(width, height);
        
        var finalX = 37;
        var finalY = 37;
        // GD.Print(tooltipGlobalPosition.Y);
        if (tooltipGlobalPosition.X > 1478) { finalX = -width; } //probably needs adjustment
        if (tooltipGlobalPosition.Y > 782) { finalY += -height; } 
        tooltip.SetPosition(new Vector2(finalX, finalY));
        
        tooltip.Visible = true;
    }

    public void HideMaterialTooltip(NinePatchRect tooltip) {
        // GD.Print("hide tooltip");
        tooltip.Visible = false;
        if (allDescriptionLabels == null) { return; }
        
        foreach (var statLabel in allDescriptionLabels) {
            // if (statLabel == null) { continue; }
            if (!IsInstanceValid(statLabel)) { continue; }
            statLabel.QueueFree();
        }
        allDescriptionLabels.Clear();
    }
    
    public Color GetDecimalColor(string color) {
        switch (color) {
            case "black": return colorBlack;
            case "white": return colorWhite;
            case "green": return colorGreen;
            case "blue": return colorBlue;
            case "purple": return colorPurple;
            case "orange": return colorOrange;
            case "yellow": return colorYellow;
            case "pink": return colorPink;
        }
        return colorBlack;
    }
    
    public string GetStringColor(string color) {
        switch (color) {
            case "black": return colorBlackString;
            case "white": return colorWhiteString;
            case "grey": return colorGreyString;
            case "green": return colorGreenString;
            case "blue": return colorBlueString;
            case "purple": return colorPurpleString;
            case "orange": return colorOrangeString;
            case "pink": return colorPinkString;
            case "teal": return colorTealString;
            case "tealtwo": return colorTealStringTwo;
            case "yellow": return colorYellowString;
        }
        return colorBlackString;
    }
    
}