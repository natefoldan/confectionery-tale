using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace ConfectioneryTale.scripts;

public partial class Assignments : Control {
    private Main main;
    private Variables vars;
    private UI ui;
    private TooltipHandler tooltips;
    private List<AssignmentData> assignmentDataList;
    private AssignmentData currentAssignmentData;
    private Label assignmentNameLabel;
    private Label assignmentDescriptionLabel;
    private Label assignmentLocationLabel;
    private Label assignmentProgressLabel;
    private Label rewardOneLabel;
    private Label rewardTwoLabel;
    // private Label assignmentStatusLabel;
    // private Label assignmentTrackedLabel;
    private TextureButton swapMajorButton;
    private TextureButton swapMinorButton;
    private TextureButton assignmentTrackerButton;
    private TextureButton MA01Button;
    private TextureButton MA02Button;
    private TextureButton MN01Button;
    private List<TextureButton> allAssignmentButtons;
    private Texture2D assignmentButtonTextureNormal;
    private Texture2D assignmentButtonTextureActive;
    private AssignmentTracker assignmentTracker;
    private bool minorTab;
    
    private Sprite2D compassArrow;
    private Vector2 compassTargetPos;
    private List<EdgeIndicator> allEdgeIndicators = new List<EdgeIndicator>();
    public Array<SavedAssignmentData> savedAssignmentDataArray = new Array<SavedAssignmentData>();
    
    public override void _Ready() {
        vars = GetNode<Variables>("/root/Variables");
        tooltips = GetNode<TooltipHandler>("/root/TooltipHandler");
        ui = GetNode<UI>("/root/Main/UI");
        main = GetNode<Main>("/root/Main");
        // main.SucroseChanged += HandleSucroseChanged;
        BuildAssignmentData();
        LoadSavedAssignmentData();
        // SetupAssignments();
        // SetCompassTarget(); //-only if using compass
    }

    public void SetupAssignments() {
        assignmentNameLabel = ui.GetNode<Label>("MainMenu/Assignments/Assignment/Name");
        assignmentDescriptionLabel = ui.GetNode<Label>("MainMenu/Assignments/Assignment/Description");
        assignmentLocationLabel = ui.GetNode<Label>("MainMenu/Assignments/Assignment/Location/Location");
        assignmentProgressLabel = ui.GetNode<Label>("MainMenu/Assignments/Assignment/Progress/Progress");
        rewardOneLabel = ui.GetNode<Label>("MainMenu/Assignments/Assignment/Reward/Rewards/RewardOne");
        rewardTwoLabel = ui.GetNode<Label>("MainMenu/Assignments/Assignment/Reward/Rewards/RewardTwo");
        // assignmentStatusLabel = ui.GetNode<Label>("MainMenu/Assignments/Assignment/Status"); //not used -delete
        // assignmentTrackedLabel = ui.GetNode<Label>("MainMenu/Assignments/Assignment/Tracked");
        swapMajorButton = ui.GetNode<TextureButton>("MainMenu/Assignments/ButtonContainer/ButtonMajor");
        swapMinorButton = ui.GetNode<TextureButton>("MainMenu/Assignments/ButtonContainer/ButtonMinor");
        assignmentTrackerButton = ui.GetNode<TextureButton>("MainMenu/Assignments/Assignment/AssignmentTracker");
        MA01Button = ui.GetNode<TextureButton>("MainMenu/Assignments/ButtonContainer/ScrollContainer/VBoxContainer/MA01");
        MA02Button = ui.GetNode<TextureButton>("MainMenu/Assignments/ButtonContainer/ScrollContainer/VBoxContainer/MA02");
        MN01Button = ui.GetNode<TextureButton>("MainMenu/Assignments/ButtonContainer/ScrollContainer/VBoxContainer/MN01");
        allAssignmentButtons = new List<TextureButton> { MA01Button, MA02Button, MN01Button };
        assignmentButtonTextureNormal = GD.Load<Texture2D>("res://assets/btn-blue-468x90.png");
        assignmentButtonTextureActive = GD.Load<Texture2D>("res://assets/btn-blue-468x90-disabled.png");
        assignmentTracker = ui.GetNode<AssignmentTracker>("MainMenu/Assignments/Assignment/AssignmentTracker");
        compassArrow = ui.GetNode<Sprite2D>("Compass/Arrow");
        SwapAssignmentTab(true);
        LoadTrackedIndicators();
    }
    
    public void RefreshAssignmentsPanel() {
        if (currentAssignmentData == null) { currentAssignmentData = GetAssignmentDataById("MA01"); }
        assignmentNameLabel.Text = "ASSIGNMENT\n" + currentAssignmentData.Name;
        assignmentDescriptionLabel.Text = currentAssignmentData.Description;
        assignmentLocationLabel.Text = currentAssignmentData.Location;
        assignmentProgressLabel.Text = currentAssignmentData.Progress + "/" + currentAssignmentData.Requirement;
        if (currentAssignmentData.Complete) { assignmentProgressLabel.Text = "COMPLETE"; }
        
        rewardOneLabel.Text = currentAssignmentData.RewardString;
        rewardTwoLabel.Text = $"+{currentAssignmentData.PointReward} Skill Point{(currentAssignmentData.PointReward > 1 ? "s" : "")}";
        rewardTwoLabel.Visible = currentAssignmentData.PointReward >= 1;
        
        // assignmentStatusLabel.Text = "NOT COMPLETE"; //not used
        // if (currentAssignmentData.Complete) { assignmentStatusLabel.Text = "complete"; }

        UpdateAssignmentTrackerButton();
        CheckAssignmentButtons();
    }

    public void ShowAssignmentInfo(string assignment) {
        currentAssignmentData = GetAssignmentDataById(assignment);
        RefreshAssignmentsPanel();
    }
    
    private void CheckAssignmentButtons() {
        foreach (var b in allAssignmentButtons) {
            var currentData = GetAssignmentDataById(b.Name);
            
            var nameLabel = b.GetNode<Label>("Name");
            var locationLabel = b.GetNode<Label>("Location");
            nameLabel.Text = currentData.Name;
            locationLabel.Text = currentData.Location;
            nameLabel.Set("theme_override_colors/font_color", tooltips.GetDecimalColor("black"));
            locationLabel.Set("theme_override_colors/font_color", tooltips.GetDecimalColor("black"));
            b.TextureNormal = assignmentButtonTextureNormal;
            
            if (currentAssignmentData != null && b.Name.Equals(currentAssignmentData.Id)) {
                b.TextureNormal = assignmentButtonTextureActive;
                nameLabel.Set("theme_override_colors/font_color", tooltips.GetDecimalColor("white"));
                locationLabel.Set("theme_override_colors/font_color", tooltips.GetDecimalColor("white"));
            }
            
            GD.Print($"button name {b.Name} obtained: {currentData.Obtained}");
            
            b.Visible = currentData.Obtained && (minorTab != currentData.Major);
        }
    }

    private void SwapAssignmentTab(bool major) {
        minorTab = !major;
        swapMajorButton.Disabled = major;
        swapMinorButton.Disabled = !major;
        CheckAssignmentButtons();
    }
    
    public void GainAssignmentDataOnly(string assignmentId) {
        var newAssignmentData = GetAssignmentDataById(assignmentId);
        newAssignmentData.Obtained = true; 
        UpdateSavedAssignmentData();
    }

    public void ShowGainedAssignmentPopup(string assignmentId) {
        var newAssignmentData = GetAssignmentDataById(assignmentId);
        if (newAssignmentData == null) return;
        // This calls your existing UI function to show the "New" popup
        ui.ShowPopupBox(false, -1, newAssignmentData); 
    }

    public void GainAssignment(string assignmentId) {
        GainAssignmentDataOnly(assignmentId);
        ShowGainedAssignmentPopup(assignmentId);
    }

    public void CompleteAssignmentDataOnly(string assignmentId) {
        var assignment = GetAssignmentDataById(assignmentId);
        assignment.Complete = true;
        if (assignment.Tracked) {
            assignment.Tracked = false;
            RemoveEdgeIndicator(assignment.TrackingId);
        }
        UpdateSavedAssignmentData();
        vars.SaveGameData();
    }

    public void ShowCompleteAssignmentPopup(string assignmentId) {
        var assignment = GetAssignmentDataById(assignmentId);
        if (assignment == null) return;
        // This calls your existing UI function to show the "Complete" popup
        ui.ShowPopupBox(false, -1, assignment); 
    }

    public void CompleteAssignment(string assignmentId) {
        CompleteAssignmentDataOnly(assignmentId);
        ShowCompleteAssignmentPopup(assignmentId);
    }
    
    // public void CompleteAssignment(string assignmentId) { //prev -WORKS
    //     // var assignment1 = GetAssignmentDataById("AssignmentOne");
    //     var assignment = GetAssignmentDataById(assignmentId);
    //     assignment.Complete = true;
    //     
    //     //if was being tracked, stop tracking it now
    //     if (assignment.Tracked) {
    //         assignment.Tracked = false;
    //         RemoveEdgeIndicator(assignment.TrackingId);
    //     }
    //     
    //     ui.ShowPopupBox(false, -1, assignment);
    //     UpdateSavedAssignmentData(); //IMPORTANT
    //     vars.SaveGameData();
    // }    
    //
    // public void GainAssignment(string assignmentId) { //prev -WORKS
    //     var newAssignmentData = GetAssignmentDataById(assignmentId);
    //     newAssignmentData.Obtained = true; 
    //     ui.ShowPopupBox(false, -1, newAssignmentData);
    //     UpdateSavedAssignmentData();
    //     CheckAssignmentButtons();
    //     // var itemReward = main.GetWorldObjectById(newAssignmentData.RewardId);
    // }

    private void UpdateAssignmentTrackerButton() {
        assignmentTracker.SetTrackerId(currentAssignmentData.TrackingId);
        assignmentTracker.SetTextureAndLabel(currentAssignmentData.Tracked);
        assignmentTracker.ShowHideButton(currentAssignmentData.Complete);
    }
    
    public void TrackAssignment() {
        //tracks the currently selected assignment in the panel
        ToggleAssignmentTracking(currentAssignmentData);
    }
    
    public void ToggleAssignmentTracking(AssignmentData assignmentToToggle) {
        if (assignmentToToggle == null) return;

        // 1. Get the ID from the assignment *passed into the function*
        var trackerId = assignmentToToggle.TrackingId;
    
        // 2. Toggle the "Tracked" status on the data object itself
        assignmentToToggle.Tracked = !assignmentToToggle.Tracked;

        // 3. Find the indicator
        EdgeIndicator existingIndicator = allEdgeIndicators.FirstOrDefault(e => e.GetObjectId().Equals(trackerId));

        // 4. Add or Remove the indicator based on the new status
        if (assignmentToToggle.Tracked) {
            // We want to track it
            if (existingIndicator == null) {
                AddEdgeIndicator(trackerId);
            }
        } else {
            // We want to untrack it
            if (existingIndicator != null) {
                RemoveEdgeIndicator(trackerId);
            }
        }

        // 5. Update the button on the main assignment screen *if* we are currently looking at that assignment
        if (currentAssignmentData != null && currentAssignmentData.Id == assignmentToToggle.Id) {
            UpdateAssignmentTrackerButton();
        }
        UpdateSavedAssignmentData();
    }
    
    // public void SetCompassTarget() { //not used
    //     if (vars.currentCompassTarget == "") {
    //         compassArrow.Visible = false;
    //         return;
    //     }
    //     compassArrow.Visible = true;
    //     compassTargetPos = main.GetWorldObjectById(vars.currentCompassTarget).GlobalPosition;
    // }
    
    // private void SetCurrentCompassTarget() { //for testing -delete
    //     vars.currentCompassTarget = "navTestOne";
    //     // ui.SetCompassTarget();
    //     AddEdgeIndicator("navTestOne");
    //     AddEdgeIndicator("navTestTwo");
    // }
    
    public void AddEdgeIndicator(string targetObjectId) {
        var indicatorScene = GD.Load<PackedScene>("res://scenes/edge_indicator.tscn");
        var newIndicator = indicatorScene.Instantiate<EdgeIndicator>();
        newIndicator.Setup(main, targetObjectId);
        allEdgeIndicators.Add(newIndicator);
        ui.AddChild(newIndicator);
        
        //set the assignment as tracked
        var currentData = GetAssignmentDataByTrackingId(targetObjectId);
        currentData.Tracked = true;
        vars.SaveGameData();
    }

    public void RemoveEdgeIndicator(string targetObjectId) {
        //find the specific EdgeIndicator to remove using LINQ's FirstOrDefault
        EdgeIndicator indicatorToRemove = allEdgeIndicators.FirstOrDefault(e => e.GetObjectId() == targetObjectId);

        if (indicatorToRemove != null) {
            allEdgeIndicators.Remove(indicatorToRemove);
            indicatorToRemove.DeleteIndicator();
        } else {
            GD.PushWarning($"Edge indicator for object ID '{targetObjectId}' not found. Cannot remove.");
        }
        
        //set the assignment as untracked
        var currentData = GetAssignmentDataByTrackingId(targetObjectId);
        currentData.Tracked = false;
        vars.SaveGameData();
    }
    
    private void LoadTrackedIndicators() {
        // 1. Get the list of ALL possible assignments
        // This is the full list from your data source, not the saved data array.
        var allAssignments = GetAllAssignmentData(); 

        // 2. Iterate through all assignments
        foreach (var assignment in allAssignments) {
            // 3. Check if the assignment is tracked in the loaded save data
            
            if (assignment.Tracked) {
                // 4. If it is, add the edge indicator back
                // The AddEdgeIndicator method will handle creating the node and adding it to your allEdgeIndicators list.
                // AddEdgeIndicator(assignment.TrackingId); //delete
                
                // Only track if it's marked tracked AND it's NOT complete.
                if (assignment.Tracked && !assignment.Complete) {
                    AddEdgeIndicator(assignment.TrackingId);
                }
            }
        }
    }
    
    private void UpdateCompassArrow() {
        if (vars.currentCompassTarget == "") { return; }
        
        //calculate the direction vector from the arrow to the target
        Vector2 directionToTarget = compassTargetPos - main.GetPlayerPosition();

        //get the angle of this direction vector. Vector2.Angle() returns the angle in radians relative to the positive X-axis (right)
        float angleToTargetRadians = directionToTarget.Angle();

        //apply this angle to the arrow's rotation. if sprite points right (0 degrees) by default, directly setting Rotation works.
        compassArrow.Rotation = angleToTargetRadians;
    }

    //assignment data
    private void UpdateSavedAssignmentData() {
        savedAssignmentDataArray.Clear();
        savedAssignmentDataArray = new Array<SavedAssignmentData>();
		  
        var updateList = new List<AssignmentData>();
		  
        foreach (var assignment in GetAllAssignmentData()) {
            updateList.Add(assignment);
        }
		  
        foreach (var assignment in updateList) {
            savedAssignmentDataArray.Add(new SavedAssignmentData(
                assignment.Id, assignment.Obtained, assignment.Progress, assignment.Tracked, assignment.Complete)); //updates every assignment regardless of if it changed or not
        }
        
        vars.SavedAssignmentDataArray = savedAssignmentDataArray;
        vars.SaveGameData(); //UNCOMMENT
    }
	   
    private void LoadSavedAssignmentData() {
        var loadedAssignmentData = vars.SavedAssignmentDataArray;
        var loadList = new List<AssignmentData>();
    
        foreach (var assignment in GetAllAssignmentData()) { loadList.Add(assignment); }
    
        foreach (var loadedAssignment in loadedAssignmentData) {
            // GD.Print("checking: " + loadedAssignment.Id);
            foreach (var assignment in loadList) {
                if (loadedAssignment.Id == assignment.Id) {
                    assignment.Obtained = loadedAssignment.Obtained;
                    assignment.Progress = loadedAssignment.Progress;
                    assignment.Tracked = loadedAssignment.Tracked;
                    assignment.Complete = loadedAssignment.Complete;
                    // GD.Print("loaded: " + assignment.Id + " complete: " +  loadedAssignment.Complete);
                }
            }
        }
    }
    
    private void BuildAssignmentData() {
        assignmentDataList = new List<AssignmentData>();
        //string assignmentId, string name, bool obtained, bool major, int progress, int requirement, string description, string location, bool tracked, bool complete, int pointReward, Resource[] itemRewards
        assignmentDataList.Add(new AssignmentData("MA01", "Access Your Base", false, true,
            0, 1, "Stand close to the door to unseal it", "Sugarcane Plain",
            false, false, "Fast Travel - Sugarcane Plain", 0, "ShelterPlainsDoor"));
		
        
        assignmentDataList.Add(new AssignmentData("MA02", "Assignment Two", false, true,
            0, 10, "assignment two description", "two location",
            false, false, "Condenser Part", 2, "woPartCondenser"));
        
        
        assignmentDataList.Add(new AssignmentData("MN01", "Assignment MINOR", false, false,
            0, 10, "assignment three description", "three location", false, false, "reward", 3, "worldExtractDamage"));
    }

    public AssignmentData GetAssignmentDataById(string id) {
        //ASSIGNMENT ID MUST MATCH BUTTON NAME
        foreach (AssignmentData assignmentData in assignmentDataList) {
            // GD.Print($"id: {id} data id: {assignmentData.Id}");
            if (!id.Equals(assignmentData.Id)) { continue; }
            // GD.Print($"found match: {assignmentData.Id}");
            return assignmentData;
        }
        return null;
    }
	
    public AssignmentData GetAssignmentDataByTrackingId(string reward) {
        foreach (AssignmentData assignmentData in assignmentDataList) {
            if (!reward.Equals(assignmentData.TrackingId)) { continue; }
            return assignmentData;
        }
        return null;
    }

    public List<AssignmentData> GetAllAssignmentData() { return assignmentDataList; }
}