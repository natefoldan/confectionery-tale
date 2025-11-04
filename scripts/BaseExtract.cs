using Godot;

namespace ConfectioneryTale.scripts;

[GlobalClass]
public partial class BaseExtract : Resource {
    [Export] public Texture2D DisplayTexture { get; set; }
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    
    // public enum Type { Extract, Resource };
    // [Export] public Type DropType { get; set; } //must be used to export enum
    
    [Export] public int CurrentOwned { get; set; }
    [Export] public bool OtherDrop { get; set; }
    [Export] public bool InInventory { get; set; }
    [Export] public bool Equipped { get; set; }
    
    [Export] public string ExtractName { get; set; }
    [Export] public int ExtractTier { get; set; }
    [Export] public float ExtractQuality { get; set; }
    [Export] public int ExtractDropChance { get; set; }
    
    [Export] public int ExtractBaseDamage { get; set; }
    [Export] public int ExtractBasePierce { get; set; }
    [Export] public int ExtractBaseCritChance { get; set; }
    [Export] public int ExtractBaseCritDamage { get; set; }
    [Export] public int ExtractBaseShield { get; set; }
    [Export] public int ExtractBaseShieldRegen { get; set; }
    [Export] public int ExtractBaseHealth { get; set; }
    [Export] public int ExtractBasePickupRange { get; set; }
    [Export] public int ExtractBaseSpeed { get; set; }
    [Export] public int ExtractBaseExtractDrop { get; set; }
    [Export] public int ExtractBaseSucroseDrop { get; set; }
    [Export] public int ExtractBaseExpGain { get; set; }

    //quality values (calculated percentages)
    [Export] public float ExtractBaseDamageQuality { get; set; }
    [Export] public float ExtractBasePierceQuality { get; set; }
    [Export] public float ExtractBaseCritChanceQuality { get; set; }
    [Export] public float ExtractBaseCritDamageQuality { get; set; }
    [Export] public float ExtractBaseShieldQuality { get; set; }
    [Export] public float ExtractBaseShieldRegenQuality { get; set; }
    [Export] public float ExtractBaseHealthQuality { get; set; }
    [Export] public float ExtractBasePickupRangeQuality { get; set; }
    [Export] public float ExtractBaseSpeedQuality { get; set; }
    [Export] public float ExtractBaseExtractDropQuality { get; set; }
    [Export] public float ExtractBaseSucroseDropQuality { get; set; }
    [Export] public float ExtractBaseExpGainQuality { get; set; }
    
    public void SetExtractStatAndValue(string statName, int value, float qualityPercentage) {
        switch (statName) {
            case "tier": ExtractTier = value; break;
            case "Damage":
                ExtractBaseDamage = value;
                ExtractBaseDamageQuality = qualityPercentage;
                break;
            case "Pierce":
                ExtractBasePierce = value;
                ExtractBasePierceQuality = qualityPercentage;
                break;
            case "Crit Chance":
                ExtractBaseCritChance = value;
                ExtractBaseCritChanceQuality = qualityPercentage;
                break;
            case "Crit Damage":
                ExtractBaseCritDamage = value;
                ExtractBaseCritDamageQuality = qualityPercentage;
                break;
            case "Shield":
                ExtractBaseShield = value;
                ExtractBaseShieldQuality = qualityPercentage;
                break;
            case "Shield Regen":
                ExtractBaseShieldRegen = value;
                ExtractBaseShieldRegenQuality = qualityPercentage;
                break;
            case "Health":
                ExtractBaseHealth = value;
                ExtractBaseHealthQuality = qualityPercentage;
                break;
            case "Pickup Range":
                ExtractBasePickupRange = value;
                ExtractBasePickupRangeQuality = qualityPercentage;
                break;
            case "Speed":
                ExtractBaseSpeed = value;
                ExtractBaseSpeedQuality = qualityPercentage;
                break;
            case "Extract Drop":
                ExtractBaseExtractDrop = value;
                ExtractBaseExtractDropQuality = qualityPercentage;
                break;
            case "Sucrose Drop":
                ExtractBaseSucroseDrop = value;
                ExtractBaseSucroseDropQuality = qualityPercentage;
                break;
            case "Exp Gain":
                ExtractBaseExpGain = value;
                ExtractBaseExpGainQuality = qualityPercentage;
                break;
            default:
                GD.PushWarning($"BaseExtract: Attempted to set unknown extract stat '{statName}' with value {value}.");
                break;
        }
    }
    
    public BaseExtract() { }
}