using Godot;

namespace ConfectioneryTale.scripts;

public partial class Shelter : StaticBody2D {
    [Export] private bool hasDistillery;
    private Variables vars;
    private UI ui;
    private Sprite2D exterior;
    private Sprite2D interior;
    private Sprite2D threshold;
    private Control distillery;
    
    public override void _Ready() {
        SetupShelter();
        SetupDistillery();
        
        //connect the signal from variables
        vars.ShelteredStateChanged += OnShelteredStateChanged;
    }

    //CLEANUP (Crucial for game stability)
    public override void _ExitTree() {
        if (vars != null) {
            vars.ShelteredStateChanged -= OnShelteredStateChanged;
        }
    }
    
    private void OnShelteredStateChanged() {
        if (vars.IsSheltered) {
            SwapToInterior();
        } else {
            SwapToExterior();
        }
    }
    
    private void SetupShelter() {
        vars = GetNode<Variables>("/root/Variables");
        exterior = GetNode<Sprite2D>("Exterior");
        interior = GetNode<Sprite2D>("Interior");
        threshold = GetNode<Sprite2D>("Threshold");
        
        if (vars.IsSheltered) { SwapToInterior(); }
        else { SwapToExterior(); }
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
    
    public Vector2 GetTravelPoint() {
        return GetNode<Node2D>("TeleportPoint").GetPosition();
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
        threshold.Visible = true;
        // GD.Print($"interior: {interior.Visible} exterior: {exterior.Visible}");
    }
    
    public void SwapToExterior() {
        exterior.Visible = true;
        interior.Visible = false;
        threshold.Visible = false;
        // GD.Print($"interior: {interior.Visible} exterior: {exterior.Visible}");
    }
}