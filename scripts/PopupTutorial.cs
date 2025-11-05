using Godot;

namespace ConfectioneryTale.scripts;

public partial class PopupTutorial : Button {

    public void SetHeader(string header) {
        GetNode<Label>("Label").Text = header;
    }
    
    public void SetDescription(string description) {
        Text = description;
    }
    
    private void OnClicked() {
        QueueFree();
        GetTree().Paused = false;
    }
    
}