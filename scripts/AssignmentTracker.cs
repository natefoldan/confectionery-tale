using Godot;

namespace ConfectioneryTale.scripts;

public partial class AssignmentTracker : TextureButton {
    private Texture2D textureTracked;
    private Texture2D textureUntracked;
    private Label trackedLabel;

    public override void _Ready() {
        textureTracked = GD.Load<Texture2D>("res://assets/btn-blue-180x50-disabled.png");
        textureUntracked = GD.Load<Texture2D>("res://assets/btn-blue-180x50.png");
        trackedLabel = GetNode<Label>("Label");
    }
    
    private string trackerId { get; set; }

    public void SetTrackerId(string id) {
        trackerId = id;
    }

    public string GetTrackerId() {
        return trackerId;
    }

    public void SetTextureAndLabel(bool tracked) {
        if (tracked) {
            TextureNormal =  textureTracked;
            trackedLabel.Text = "Untrack";
            return;
        }
        TextureNormal =  textureUntracked;
        trackedLabel.Text = "Track";
    }

    public void ShowHideButton(bool hide) {
        Visible = !hide;
    }
}