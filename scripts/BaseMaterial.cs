using Godot;

namespace ConfectioneryTale.scripts;

[GlobalClass]
public partial class BaseMaterial : Resource {
    [Export] public Texture2D DisplayTexture { get; set; }
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public string Description { get; set; }
    [Export] public double CurrentOwned { get; set; }
    [Export] public double TotalFound { get; set; }
    [Export] public bool InInventory { get; set; }
    
    public BaseMaterial() { }
}