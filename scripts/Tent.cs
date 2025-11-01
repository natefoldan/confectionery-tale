using System;
using Godot;

namespace ConfectioneryTale.scripts;

public partial class Tent : Area2D {
    private Variables vars;
    private Main main;
    private Area2D _playerDetectionZone;
    private bool _isPlayerInside = false;
    private double tentBuildTimer = 0;
    private Label tentBuildTimerLabel;
    private bool built;
    
    public override void _Ready() {
        vars = GetNode<Variables>("/root/Variables");
        main = GetNode<Main>("/root/Main");
        
        tentBuildTimerLabel = GetNode<Label>("BuildTimerLabel");
        _playerDetectionZone = GetNode<Area2D>("PlayerDetection");
        _playerDetectionZone.AreaEntered += OnPlayerEntered;
        _playerDetectionZone.AreaExited += OnPlayerExited;
    }

    public override void _Process(double delta) {
        if (built) { return; }
        UpdateTentBuildTimer(delta);
    }
    
    public void BeginTentBuild() {
        tentBuildTimer = main.GetTentBuildTime();
        vars.TentCooldownTimer = main.GetTentBuildCooldown();
    }
    
    private void UpdateTentBuildTimer(double delta) {
        // if(tentBuildTimer <= 0) { return; }
        //timer decrement, must be in seconds
        tentBuildTimer -= Math.Min(delta, 0.1);
        
        if (tentBuildTimer < 1) {
            tentBuildTimer = 0.0; //clamp the value to 0 to reset the global cooldown
            CompleteBuild(); 
            tentBuildTimerLabel.Text = $"{00:00}:{00:00}";
        } else { //update the display only if the timer is still running
            int displaySeconds = (int)Math.Floor(tentBuildTimer % 60.0);
            tentBuildTimerLabel.Text = $"{00:00}:{displaySeconds:00}";
        }
    }

    private void CompleteBuild() {
        Modulate = new Color(1, 1, 1, 1);
        tentBuildTimerLabel.Visible = false;
        built = true;
    }
    
    private void OnPlayerEntered(Area2D area) {
        if (!built) { return; }
        if (area.IsInGroup("player")) {
            _isPlayerInside = true;
            GD.Print("Player has entered the tent!");
        }
    }

    private void OnPlayerExited(Area2D area) {
        if (!built) { return; }
        if (area.IsInGroup("player")) {
            _isPlayerInside = false;
            GD.Print("Player has left the tent.");
        }
    }
}