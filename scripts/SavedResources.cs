using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace ConfectioneryTale.scripts;

[GlobalClass]
public partial class SavedResources : Resource {
    [Export] public int TotalSaves { get; set; }
    [Export] public int PlayerLevel { get; set; }
    [Export] public double CurrentPlayerExp { get; set; }
    [Export] public double CurrentSucrose { get; set; }
    [Export] public int CurrentPlayerHealth { get; set; }
    [Export] public int CurrentFireMode { get; set; }
    [Export] public bool AutoFire { get; set; }
    [Export] public bool CondenserBuilt { get; set; }
    [Export] public bool RefinerBuilt { get; set; }
    
    [Export] public bool SoftenerOwned { get; set; }
    [Export] public bool SpreaderOwned { get; set; }
    [Export] public bool SniperOwned { get; set; }
    [Export] public bool SlowerOwned { get; set; }
    [Export] public bool SmasherOwned { get; set; }
    
    //skills
    // [Export] public int SkillCrackingLevel { get; set; }
    // [Export] public int SkillCrackingCurrentExp { get; set; }
    
    [Export] public int SkillSprayerDamage { get; set; }
    [Export] public int SkillSprayerCritChance { get; set; }
    [Export] public int SkillSprayerKnockback { get; set; }
    [Export] public int SkillSprayerMelt { get; set; }
    
    [Export] public int SkillSoftenerDamage { get; set; }
    [Export] public int SkillSoftenerPierce { get; set; }
    [Export] public int SkillSoftenerSize { get; set; }
    [Export] public int SkillSoftenerChill { get; set; }
    
    [Export] public int SkillSpreaderDamage { get; set; }
    [Export] public int SkillSpreaderBullets { get; set; }
    [Export] public int SkillSpreaderKnockback { get; set; }
    [Export] public int SkillSpreaderReload { get; set; }
    
    [Export] public int SkillSlowerDamage { get; set; }
    [Export] public int SkillSlowerSize { get; set; }
    [Export] public int SkillSlowerMelt { get; set; }
    [Export] public int SkillSlowerReload { get; set; }
    
    [Export] public int SkillSniperDamage { get; set; }
    [Export] public int SkillSniperInstant { get; set; }
    [Export] public int SkillSniperReload { get; set; }
    [Export] public int SkillSniperRestore { get; set; }
    
    [Export] public int SkillSmasherDamage { get; set; }
    [Export] public int SkillSmasherRadius { get; set; }
    [Export] public int SkillSmasherKnockback { get; set; }
    [Export] public int SkillSmasherMelt { get; set; }
    
    // Settings
    [Export] public bool MusicOn { get; set; }
    [Export] public bool SoundOn { get; set; }
    [Export] public float MusicVolume { get; set; }
    [Export] public float MusicVolumeSlider { get; set; }
    [Export] public float SoundVolume { get; set; }
    [Export] public float SoundVolumeSlider { get; set; }
    
    [Export] public string CurrentCompassTarget { get; set; }
    [Export] public string CurrentArea { get; set; }
    [Export] public int PlainRank { get; set; }
    [Export] public int GroveRank { get; set; }
    [Export] public int FallsRank { get; set; }
    [Export] public int CakeRank { get; set; }
    [Export] public int SwampRank { get; set; }
    [Export] public int WoodsRank { get; set; }
    [Export] public int PointRank { get; set; }
    
    //items
    [Export] public Array<SavedItemData> SavedItemDataArray { get; set; }
    [Export] public Array<SavedPortalData> SavedPortalDataArray { get; set; }
    [Export] public Array<SavedWorldObjectData> SavedWorldObjectArray { get; set; }
    [Export] public Array<SavedAssignmentData> SavedAssignmentDataArray { get; set; }
    
    //the actual lists that gets saved/loaded.
    [Export] public Array<BaseExtract> inventoryExtracts { get; set; } = new Array<BaseExtract>();
    [Export] public Array<BaseExtract> equippedExtracts { get; set; } = new Array<BaseExtract>();
    [Export] public Array<string> DiscoveredAreasList { get; set; } = new Array<string>();
    [Export] public Array<string> SeenTutorialsList { get; set; } = new Array<string>();
    
    public SavedResources() { }

    //used by Variables or directly if needed
    // public void AddInventoryItemToResource(BaseExtract item) { inventoryItems.Add(item); } //not used
    // public void AddEquippedModsToResource(BaseExtract item) { equippedMods.Add(item); } //not used
}