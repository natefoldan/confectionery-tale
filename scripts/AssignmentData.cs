using Godot;

namespace ConfectioneryTale.scripts;

[GlobalClass]
public partial class AssignmentData : Resource {
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public bool Obtained { get; set; }
    [Export] public bool Major { get; set; }
    [Export] public int Progress { get; set; }
    [Export] public int Requirement { get; set; }
    [Export] public string Description { get; set; }
    [Export] public string Location { get; set; }
    [Export] public bool Tracked { get; set; }
    [Export] public bool Complete { get; set; }
    [Export] public int PointReward { get; set; }
    // [Export] public Resource[] ItemRewards { get; set; }
    [Export] public string TrackingId { get; set; }

    public AssignmentData() {}
    
    public AssignmentData(
        string assignmentId, string name, bool obtained, bool major, int progress, int requirement, string description,
        string location, bool tracked, bool complete, int pointReward, string trackingId) {
        Id = assignmentId;
        Name = name;
        Obtained = obtained;
        Major = major;
        Progress = progress;
        Requirement = requirement;
        Description = description;
        Location = location;
        Tracked = tracked;
        Complete = complete;
        PointReward = pointReward;
        TrackingId = trackingId;
    }
}