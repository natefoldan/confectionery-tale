using Godot;

namespace ConfectioneryTale.scripts;

[GlobalClass]
public partial class BaseEnemy : Resource {
    [Export] public Texture2D DisplayTexture { get; set; }
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public Area SpawnArea { get; set; }
    [Export] public Type DropType { get; set; }
    [Export] public int Rank { get; set; }
    [Export] public int CollideDamage { get; set; }
    [Export] public string animation { get; set; }

    // [Export] public BaseMaterial[] ItemDrops;
    [Export] public Godot.Resource[] ItemDrops;
    
    public enum Type { None, Chewy, Soft, Hard }
    public enum Area { Plain, Grove, Falls, Cake, Swamp, Woods, Point, Other }
    
    public BaseEnemy() { } //important to always include parameterless constructor
}