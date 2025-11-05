using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace ConfectioneryTale.scripts;

[GlobalClass, Tool]
public partial class WorldObject : StaticBody2D {
    [Export] private string name;
    [Export] public string objectId;
    
    [Export] public Tutorial TutorialType { get; set; }
    public enum Tutorial { None, Extract, Cracking, Bullet }
    
    private Texture2D _worldObjectTexture;
    private Sprite2D thisSprite;
    
    [Export]
    public Texture2D WorldObjectTexture {
        get { return _worldObjectTexture; }
        set {
            _worldObjectTexture = value;
            // The setter will only update the variable
        }
    }
    // [Export] public Texture2D DisplayTexture { get; set; }
    [Export] private string popupDescription;

    [Export] private bool hasCollision;
    [Export] private bool hidePopup;
    [Export] private double crackSeconds;
    
    [ExportGroup("Item Stats")]
    [Export(PropertyHint.Range, "1,5")] public int Tier { get; private set; } = 1;
    [Export] public int Damage { get; private set; } = 0;
    [Export] public int Speed { get; private set; } = 0;
    
    private Main main;
    private Variables vars;
    private UI ui;
    private ExtractManager extractManager;
    private Label infoPopup;
    
    private int spriteWidth;
    private int spriteHeight;
    private float spriteScaledHeight;
    private bool isBeingCracked;
    private bool obtained;
    private bool cracked;
    private double currentCrackTimer;
    
    private Array<SavedWorldObjectData> savedWorldObjectDataArray = new Array<SavedWorldObjectData>();
    
    private Label crackingTimerLabel;
    
    public override void _Ready() {
        main = GetNode<Main>("/root/Main");
        vars = GetNode<Variables>("/root/Variables");
        extractManager = GetNode<ExtractManager>("/root/ExtractManager");
        thisSprite = GetNode<Sprite2D>("Sprite2D");
        if (vars.SavedWorldObjectArray == null) { SaveWorldObjectData(); }
        LoadSavedWorldObjectData();
        
        SetTexture();
        RemoveIfCracked();
        
        crackingTimerLabel = GetNode<Label>("CrackingTimerLabel");
        crackingTimerLabel.Visible = false;
        
        SetCollisionBox();
        SetInteractCollisionRadius();
        if (HasNode("Popup")) { //not needed, they all have popup
            infoPopup = GetNode<Label>("Popup");
            infoPopup.SetAsTopLevel(true);
            HideInfoPopup();
        }
    }

    public override void _PhysicsProcess(double delta) {
        UpdateCrackingTimer(delta);
    }
    
    public override void _Process(double delta) {
        if (Engine.IsEditorHint()) {
            thisSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
            if (thisSprite != null && thisSprite.Texture != _worldObjectTexture) {
                thisSprite.Texture = _worldObjectTexture; //only update the sprite if the value in the inspector has changed
            }
        }
    }

    private void SetTexture() {
        if (thisSprite != null && _worldObjectTexture != null) {
            thisSprite.Texture = _worldObjectTexture;
            spriteWidth = thisSprite.Texture.GetWidth();
            spriteHeight = thisSprite.Texture.GetHeight();
            spriteScaledHeight = spriteHeight * thisSprite.Scale.Y;
        }
    }
    
    public void SetUI(UI passedUI) { ui = passedUI; }
    
    private void SetCollisionBox() {
        if(!hasCollision) { return; }
        if (thisSprite == null) { return; }
        var posY = 0;
        var collisionShape = new CollisionShape2D();
        var boundingBox = new Vector2(spriteWidth / 1f, spriteHeight); //can't use negative if sprite is too small. divide by # of frames
        var collisionRectangle = new RectangleShape2D();
        var collisionArea = GetNode<StaticBody2D>("CollisionArea");
        collisionRectangle.Size = boundingBox;
        collisionShape.Shape = collisionRectangle;
        collisionShape.Position = new Vector2(0, posY);
        // SetScaleAndAnimation(collisionShape, sprite);
        // AddChild(collisionShape);
        collisionArea.AddChild(collisionShape);
        // GD.Print("wo bounding box: " + boundingBox);
    }
    
    private void SetInteractCollisionRadius() {
        // if(!hasCollision) { return; }
        if (thisSprite == null) { return; }
        var interactArea = GetNode<Area2D>("InteractArea");
        var collisionShape = new CollisionShape2D();
        var collisionCircle = new CircleShape2D();

        //the radius is half the diameter. Mathf.Max gives the diameter.
        float baseRadius = Mathf.Max(spriteWidth, spriteHeight) / 2.0f;
        float interactMargin = 10.0f;
        float finalRadius = baseRadius + interactMargin;
        
        collisionCircle.Radius = finalRadius;
        collisionShape.Shape = collisionCircle;
        collisionShape.Position = Vector2.Zero; //center the circle on the Area2D's origin
        interactArea.AddChild(collisionShape);
    }
    
    private void ShowInfoPopup() {
        if (hidePopup) { return; }
        if (infoPopup == null) { return; }

        //GD.Print($"interacting with {objectId} has popup: {!hidePopup}");
        // switch (objectId) { //not used -delete
        //     case "woDistillery":
        //         PopulateDistilleryPopup();
        //         break;
        //     case "woSoftener":
        //         PopulateSoftenerPopup();
        //         break;
        // }

        PopulatePopupText();
        
        //get the popup size
        var popupRect = infoPopup.GetRect();
        var spriteLocalRect = thisSprite.GetRect();

        //find the local top-center point. this finds the X-center and the top Y edge of the sprite's texture
        var spriteLocalTopCenter = new Vector2(
            spriteLocalRect.Position.X + (spriteLocalRect.Size.X / 2.0f),
            spriteLocalRect.Position.Y
        );

        //convert that local point to a global position
        var spriteGlobalTopCenter = thisSprite.ToGlobal(spriteLocalTopCenter);

        infoPopup.GlobalPosition = new Vector2(
            //center the popup over the sprite's top-center
            spriteGlobalTopCenter.X - (popupRect.Size.X / 2.0f), 
            //place the popup 32 pixels *above* the sprite's top
            spriteGlobalTopCenter.Y - popupRect.Size.Y - 32
        );
    
        infoPopup.Visible = true;
    }
    
    private void HideInfoPopup() {
        if (infoPopup == null) { return; }
        infoPopup.Visible = false;
    }

    private void PopulatePopupText() {
        //show hotkey here
        var hotkey = "E";
        
        infoPopup.Text = $"[{hotkey}] {popupDescription}";
        
        // vars.CondenserBuilt
        // vars.RefinerBuilt
    }
    
    private void UpdateCrackingTimer(double delta) {
        if (!isBeingCracked) { return; }
        //timer decrement, must be in seconds
        currentCrackTimer -= Math.Min(delta, 0.1); //ensure crack time decreases
        currentCrackTimer = Math.Max(0.0, currentCrackTimer); //prevent from going negative

        //time calculation for display
        int displayHours = (int)Math.Floor(currentCrackTimer / 3600.0);
        int displayMinutes = (int)Math.Floor((currentCrackTimer % 3600.0) / 60.0);
        int displaySeconds = (int)Math.Floor(currentCrackTimer % 60.0); //get remaining whole seconds

        if (displayHours > 0) { //if there are hours, display HH:MM:SS
            crackingTimerLabel.Text = $"{displayHours:0}:{displayMinutes:00}:{displaySeconds:00}";
        } else { //if there are no hours, display MM:SS
            crackingTimerLabel.Text = $"{displayMinutes:00}:{displaySeconds:00}";
        }

        if (currentCrackTimer <= 0.0) { CompleteCrack(); }
    }

    private void CompleteCrack() {
        // GD.Print("crack complete");
        cracked = true;
        main.GainCrackingExp(GetCrackingExp());
        // SaveWorldObjectData(); //uncomment
        QueueFree();
    }

    private int GetCrackingExp() {
        return (int) Math.Ceiling(crackSeconds / 10);
    }
    
    private void StartCracking() {
        if (crackSeconds < 1) { return; }
        if (!vars.IsCracking) { ResetCracking(); }
        vars.IsCracking = true;
        isBeingCracked = true;
        crackingTimerLabel.Visible = true;
    }

    private void ResetCracking() {
        vars.IsCracking = false;
        isBeingCracked = false;
        currentCrackTimer = GetFinalCrackingTime();
        crackingTimerLabel.Visible = false;
    }

    private double GetFinalCrackingTime() {
        double speedMultiplier = main.GetCrackingSpeedPercent();
        return crackSeconds / speedMultiplier;
    }
    
    private void RemoveIfCracked() {
        if (!cracked) { return; }
        QueueFree();
    }
    
    public void PickupFixedExtract() {
        if (ui.GetExtractsInventoryFull()) { return; }

        //bundle the stats from the Inspector into a dictionary
        var fixedStats = new Godot.Collections.Dictionary<string, int>();
        if (Damage > 0) fixedStats.Add("Damage", Damage);
        if (Speed > 0) fixedStats.Add("Speed", Speed);
        // if (Pierce > 0) fixedStats.Add("Pierce", Pierce);
        // if (ExpGain > 0) fixedStats.Add("Exp Gain", ExpGain);

        BaseExtract newItem = extractManager.GenerateFixedExtract(Tier, fixedStats);
        ui.AddExtractToInventory(newItem);
        QueueFree();
    }
    
    private void PlayerEnteredRange(Area2D body) {
        // vars.CurrentWorldObject = objectId; //old way
        vars.CurrentWorldObject = this;
        StartCracking();
        // GD.Print($"interacting with {objectId}");
        ShowInfoPopup();
        
        if (TutorialType != Tutorial.None) { ui.ShowPopupTutorial(TutorialType.ToString()); }
        
    }
    
    private void PlayerExitedRange(Area2D body) {
        // vars.CurrentWorldObject = ""; //old way
        vars.CurrentWorldObject = this;
        ResetCracking();
        // vars.IsInteracting = false;
        HideInfoPopup();
    }
    
    // private void PlayerCollided(Area2D body) { //not used?
    //     GD.Print("player entered range");
    // }
    
    public string GetObjectId() { return objectId; }

    public void Remove() {
        QueueFree();
    }
    
    private void SaveWorldObjectData() {
        var savedWorldObjectArray = vars.SavedWorldObjectArray;
        if (savedWorldObjectArray == null) {
            savedWorldObjectArray = new Array<SavedWorldObjectData>();
            vars.SavedWorldObjectArray = savedWorldObjectArray;
        }

        //try to find an existing entry for this portal's ID
        var existingData = savedWorldObjectArray.FirstOrDefault(p => p.WorldObjectId.Equals(objectId));

        if (existingData != null) {
            existingData.Obtained = obtained;
            GD.Print($"Updated world object data for ID: {objectId}. Obtained is now {obtained}");
        } else {
            //if the data does not exist, create a new entry and add it
            var newWorldObjectData = new SavedWorldObjectData {
                WorldObjectId = objectId,
                Obtained = obtained,
                Cracked = cracked,
            };
            savedWorldObjectArray.Add(newWorldObjectData);
            GD.Print($"Added new world object data with ID: {objectId}");
        }

        // GD.Print($"Saving wo ID: {id} with Obtained: {obtained}");
        vars.SaveGameData(); //uncomment
    }
    
    private void LoadSavedWorldObjectData() {
        if (vars.SavedWorldObjectArray == null) {
            GD.Print("No saved world object data to load.");
            return;
        }
    
        foreach (var loadedData in vars.SavedWorldObjectArray) {
            if (!loadedData.WorldObjectId.Equals(objectId)) { continue; }
            GD.Print("loaded: " + loadedData.WorldObjectId);
            obtained = loadedData.Obtained;
            cracked = loadedData.Cracked;
        }
        // GD.Print(cleansed);
    }
}