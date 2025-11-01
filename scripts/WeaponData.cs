using Godot;

namespace ConfectioneryTale.scripts;

public partial class WeaponData : Node {
    public Texture2D Texture;
    public Texture2D Crosshairs;
    public string Id;
    public string Name;
    public string Description;
    public int FireMode;
    public string AltEffect;
    public int Damage;
    public float ReloadSpeed;
    public int Range;
    public float Speed;
    public int Pierce;
    public int Knockback;
    public int Crit;
    public int CritDamage;
    public float MeltDuration;
    public float ChillDuration;
    public int Size;
    
    public WeaponData() { }

    public WeaponData(Texture2D texture, Texture2D crosshairs, string id, string name, string description, int fireMode, string altEffect,
        int damage, float reloadSpeed, int range, float speed, int pierce, int knockback, int crit, int critDamage, float meltDuration, float chillDuration, int size) {
        Texture = texture;
        Crosshairs = crosshairs;
        Id = id;
        Name = name;
        Description = description;
        FireMode = fireMode;
        AltEffect = altEffect;
        Damage = damage;
        ReloadSpeed = reloadSpeed;
        Range = range;
        Speed = speed;
        Pierce = pierce;
        Knockback = knockback;
        Crit = crit;
        CritDamage = critDamage;
        MeltDuration = meltDuration;
        ChillDuration = chillDuration;
        Size = size;
    }
}