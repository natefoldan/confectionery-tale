using Godot;

namespace ConfectioneryTale.scripts;

[GlobalClass]
public partial class SavedAssignmentData : Resource {
    [Export] public string Id { get; set; }
    [Export] public bool Obtained { get; set; }
    [Export] public int Progress { get; set; }
    [Export] public bool Tracked { get; set; }
    [Export] public bool Complete { get; set; }

    public SavedAssignmentData() {}
    
    public SavedAssignmentData(string id, bool obtained, int progress, bool tracked, bool complete) {
        Id = id;
        Obtained = obtained;
        Progress = progress;
        Tracked = tracked;
        Complete = complete;
    }
}