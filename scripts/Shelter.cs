using Godot;

namespace ConfectioneryTale.scripts;

public partial class Shelter : StaticBody2D {
    [Export] private bool hasDistillery;
    private Variables vars;
    private UI ui;
    private string name = "shelterone";
    private Sprite2D exterior;
    private Sprite2D interior;
    private Control distillery;
    
    public override void _Ready() {
        SetupShelter();
        SetupDistillery();
    }

    private void SetupShelter() {
        vars = GetNode<Variables>("/root/Variables");
        exterior = GetNode<Sprite2D>("Exterior");
        interior = GetNode<Sprite2D>("Interior");

        if (vars.IsSheltered) { SwapToInterior(); }
        else { SwapToExterior(); }
        // interior.Visible = false; //delete
        // GD.Print($"setup interior visible: {interior.Visible} exterior: {exterior.Visible}");
    }

    private void SetupDistillery() {
        if (!hasDistillery) { return; }
        distillery = GetNode<Control>("Interior/Distillery");
        CheckDistillery();
    }
    
    public void SetUI(UI passedUI) {
        ui = passedUI;
        ui.ObjectBuilt += HandleObjectBuilt;
    }
    
    public string GetShelterName() {
        return name;
    }

    public Vector2 GetTravelPoint() {
        return GetNode<Node2D>("TravelPoint").GetPosition();
    }
    
    private void HandleObjectBuilt(string what) {
        GD.Print("built: " + what);
        CheckDistillery();
    }
    
    private void CheckDistillery() {
        // distillery.Modulate = vars.DistilleryBuilt ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, .3f);
        // distillery.SelfModulate = vars.DistilleryBuilt ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, .3f);
    }
    
    public void SwapToInterior() {
        interior.Visible = true;
        exterior.Visible = false;
        GetNode<Sprite2D>("Threshold").Visible = true;
        // GD.Print($"interior visible: {interior.Visible} exterior: {exterior.Visible}");
    }
    
    public void SwapToExterior() {
        exterior.Visible = true;
        interior.Visible = false;
        GetNode<Sprite2D>("Threshold").Visible = false;
        // GD.Print($"interior visible: {interior.Visible} exterior: {exterior.Visible}");
    }
    
    private void EnteredShelter(Area2D area) {
        GD.Print("entered");
    }
    
    private void ExitedShelter(Area2D area) {
        GD.Print("exited");
    }
}