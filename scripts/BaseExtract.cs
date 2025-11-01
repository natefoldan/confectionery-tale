using Godot;

namespace ConfectioneryTale.scripts;

[GlobalClass]
public partial class BaseExtract : Resource {
    [Export] public Texture2D DisplayTexture { get; set; }
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    
    public enum Type { Mod, Resource };
    [Export] public Type DropType { get; set; } //must be used to export enum
    
    [Export] public int CurrentOwned { get; set; }
    [Export] public bool OtherDrop { get; set; }
    [Export] public bool InInventory { get; set; }
    [Export] public bool Equipped { get; set; }
    
    [Export] public string ModName { get; set; }
    [Export] public int ModTier { get; set; }
    [Export] public float ModQuality { get; set; }
    [Export] public int ModDropChance { get; set; }
    
    [Export] public int ModBaseDamage { get; set; }
    [Export] public int ModBasePierce { get; set; }
    [Export] public int ModBaseCritChance { get; set; }
    [Export] public int ModBaseCritDamage { get; set; }
    [Export] public int ModBaseShield { get; set; }
    [Export] public int ModBaseShieldRegen { get; set; }
    [Export] public int ModBaseHealth { get; set; }
    [Export] public int ModBasePickupRange { get; set; }
    [Export] public int ModBaseSpeed { get; set; }
    [Export] public int ModBaseExtractDrop { get; set; }
    [Export] public int ModBaseSucroseDrop { get; set; }
    [Export] public int ModBaseExpGain { get; set; }

    //quality values (calculated percentages)
    [Export] public float ModBaseDamageQuality { get; set; }
    [Export] public float ModBasePierceQuality { get; set; }
    [Export] public float ModBaseCritChanceQuality { get; set; }
    [Export] public float ModBaseCritDamageQuality { get; set; }
    [Export] public float ModBaseShieldQuality { get; set; }
    [Export] public float ModBaseShieldRegenQuality { get; set; }
    [Export] public float ModBaseHealthQuality { get; set; }
    [Export] public float ModBasePickupRangeQuality { get; set; }
    [Export] public float ModBaseSpeedQuality { get; set; }
    [Export] public float ModBaseExtractDropQuality { get; set; }
    [Export] public float ModBaseSucroseDropQuality { get; set; }
    [Export] public float ModBaseExpGainQuality { get; set; }
    
    public void SetModStatAndValue(string statName, int value, float qualityPercentage) {
        switch (statName) {
            case "tier": ModTier = value; break;
            case "Damage":
                ModBaseDamage = value;
                ModBaseDamageQuality = qualityPercentage;
                break;
            case "Pierce":
                ModBasePierce = value;
                ModBasePierceQuality = qualityPercentage;
                break;
            case "Crit Chance":
                ModBaseCritChance = value;
                ModBaseCritChanceQuality = qualityPercentage;
                break;
            case "Crit Damage":
                ModBaseCritDamage = value;
                ModBaseCritDamageQuality = qualityPercentage;
                break;
            case "Shield":
                ModBaseShield = value;
                ModBaseShieldQuality = qualityPercentage;
                break;
            case "Shield Regen":
                ModBaseShieldRegen = value;
                ModBaseShieldRegenQuality = qualityPercentage;
                break;
            case "Health":
                ModBaseHealth = value;
                ModBaseHealthQuality = qualityPercentage;
                break;
            case "Pickup Range":
                ModBasePickupRange = value;
                ModBasePickupRangeQuality = qualityPercentage;
                break;
            case "Speed":
                ModBaseSpeed = value;
                ModBaseSpeedQuality = qualityPercentage;
                break;
            case "Extract Drop":
                ModBaseExtractDrop = value;
                ModBaseExtractDropQuality = qualityPercentage;
                break;
            case "Sucrose Drop":
                ModBaseSucroseDrop = value;
                ModBaseSucroseDropQuality = qualityPercentage;
                break;
            case "Exp Gain":
                ModBaseExpGain = value;
                ModBaseExpGainQuality = qualityPercentage;
                break;
            default:
                GD.PushWarning($"BaseExtract: Attempted to set unknown mod stat '{statName}' with value {value}.");
                break;
        }
    }
    
    public BaseExtract() { }
}