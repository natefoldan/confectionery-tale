using Godot;

namespace ConfectioneryTale.scripts;

public partial class PopupBox : TextureRect {
    [Signal] public delegate void PopupShowAssignmentEventHandler(AssignmentData assignmentData);
    [Signal] public delegate void PopupTrackAssignmentEventHandler(AssignmentData assignmentData);

    private Texture2D buttonNormal;
    private Texture2D buttonActive;
    private TextureButton viewAssignmentButton;
    private TextureButton trackAssignmentButton;
    private Label header;
    private Label subHeader;
    private Label description;
    private Label hotkey;
    private AssignmentData assignmentData;
    
    public override void _Ready() {
        buttonNormal = GD.Load<Texture2D>("res://assets/btn-blue-180x50.png");
        buttonActive = GD.Load<Texture2D>("res://assets/btn-blue-180x50-disabled.png");
        viewAssignmentButton = GetNode<TextureButton>("HBoxContainer/View");
        trackAssignmentButton = GetNode<TextureButton>("HBoxContainer/Track");
        header = GetNode<Label>("Header");
        subHeader = GetNode<Label>("SubHeader");
        description = GetNode<Label>("Description");
        hotkey = GetNode<Label>("Hotkey");
        HideButtons();
        AddToGroup("popups");
    }
    
    public void HeaderText(string text) {
        header.Text = text;
    }
    
    public void SubHeaderText(string text) {
        subHeader.Text = text;
    }
    
    public void Description(string text, int fontSize) {
        Set("theme_override_font_sizes/font_size", fontSize);
        description.Text = text;
    }
    
    public void Hotkey(string text) {
        hotkey.Text = text;
    }

    public void SetAssignment(AssignmentData assignment) {
        assignmentData = assignment;
    }
    
    private void HideButtons() {
        viewAssignmentButton.Visible = false;
        trackAssignmentButton.Visible = false;
    }
    
    public void ToggleButtons(string type) {
        HideButtons();
        
        switch (type) {
            case "bullet":
                break;
            case "assignment":
                viewAssignmentButton.Visible = true;
                trackAssignmentButton.Visible = true;
                break;
            case "tutorial":
                break;
        }
    }

    private void ShowAssignment() {
        EmitSignal(SignalName.PopupShowAssignment, assignmentData);
        QueueFree();
    }

    private void TrackAssignment() {
        EmitSignal(SignalName.PopupTrackAssignment, assignmentData);
        trackAssignmentButton.TextureNormal = assignmentData.Tracked ? buttonActive : buttonNormal;
    }
    
    private void Close() {
        RemoveFromGroup("popups"); //remove self from group *before* checking
    
        //only unpause if this is the lasst popup on screen
        if (GetTree().GetNodesInGroup("popups").Count == 0) {
            GetTree().Paused = false;
        }
    
        QueueFree();
    }
    
    // private void Close() { //original
    //     GetTree().Paused = false;
    //     QueueFree();
    // }
}