using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace ConfectioneryTale.scripts;

public partial class Main : Node2D {
	[Signal] public delegate void SucroseChangedEventHandler();
	[Signal] public delegate void MaterialsChangedEventHandler();
	[Signal] public delegate void GainedPlayerLevelEventHandler();
	[Export] public Array<BaseMaterial> GlobalMaterialDrops { get; private set; }
	private Variables vars;
	private UI ui;
	private TooltipHandler tooltips;
	private Skills skills;
	private Assignments assignments;
	private Player player;
	private Camera2D playerCamera;
	private PackedScene playerScene;
	private PackedScene cutsceneManagerScene;
	private PackedScene currencySucroseScene;
	private PackedScene popupTextScene;
	private WeaponData weaponData;
	private WeaponData currentWeaponData;
	private List<WeaponData> weaponDataList;
	public Vector2 PlayerGlobalPosition { get; private set; }
	
	private Sprite2D playerSpawnPortal;
	private StaticBody2D shelterPlain;
	private StaticBody2D shelterCake;
	private StaticBody2D shelterWoods;
	private Shelter shelterPlainScript;
	private Shelter shelterCakeScript;
	private Shelter shelterWoodsScript;
	private List<Shelter> allShelters;
	private List<WorldObject> allWorldObjects;
	private List<Node2D> placementCollisions = new List<Node2D>();
	private readonly Color placementValidColor = new Color(0.5f, 1f, 0.5f, 0.7f); //green tint
	private readonly Color placementInvalidColor = new Color(1f, 0.5f, 0.5f, 0.7f); //red tint
	private bool isPlacementValid => placementCollisions.Count == 0;
	
	private TileMapLayer tileMapWorld;
	
	//camping
	[Export] private Texture2D tentTexture;
	private Area2D tentPlacementPreview;
	private Sprite2D tentPreviewSprite;
	private bool isPlacing = false;
	private PackedScene tentScene;
	private Tent tent;

	private int tinctureSpeedIncrease = 0;
	
	private Timer enemyCullingTimer;
	private double despawnDistanceSquared;

	public override void _Ready() {
		SetupGame();
		// TESTCUTSCENE();
	}
	
    private void SetupGame() {
        vars = GetNode<Variables>("/root/Variables");
        if (vars == null) { GD.PushError("Main.SetupGame: Variables singleton not found!"); return; }

        ui = GetNode<UI>("UI");
        skills = GetNode<Skills>("UI/MainMenu/Skills");
        assignments = GetNode<Assignments>("UI/MainMenu/Assignments");
        if (ui == null) { GD.PushError("Main.SetupGame: UI node not found!"); return; }

        tooltips = GetNode<TooltipHandler>("/root/TooltipHandler");
        
        tileMapWorld = GetNode<TileMapLayer>("World/TileMapWorld");
        playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
        cutsceneManagerScene = GD.Load<PackedScene>("res://scenes/cutscene_manager.tscn");
        currencySucroseScene = GD.Load<PackedScene>("res://scenes/currency_sucrose.tscn");
        popupTextScene = GD.Load<PackedScene>("res://scenes/popup_text.tscn");
        
        playerSpawnPortal = GetNode<Sprite2D>("World/PlayerSpawnPointOne/PlayerSpawnPortal");
        playerSpawnPortal.Visible = false;
        
        weaponData = GetNode<WeaponData>("/root/WeaponData");
        BuildWeaponData();
        ui.InitializeUIComponents(this); //pass 'this' (the Main instance) to UI for its data
        
        InstantiatePlayer();
        SetCurrentWeaponData(vars.CurrentFireMode);
        player.SetMouseCursor();
		GetAllShelters();
		GetAllWorldObjects();
		RemoveOwnedWorldObjects();
		SetupSpawners();
		assignments.SetupAssignments(); //must run after getting world objects
		//camping
		tentScene = GD.Load<PackedScene>("res://scenes/tent.tscn");
		tentPlacementPreview = GetNode<Area2D>("World/Tent");
		tentPreviewSprite = tentPlacementPreview.GetNode<Sprite2D>("Sprite2D");
		tentPlacementPreview.Visible = false;
		tentPlacementPreview.AreaEntered += OnPreviewAreaEntered;
		tentPlacementPreview.AreaExited += OnPreviewAreaExited;
		
		//enemy distance despawn logic
		// set the distance (in pixels). using squared distance is much faster for checks.
		// e.g., 3000 pixels. 3000 * 3000 = 9,000,000
		double despawnDistance = 40000.0; 
		despawnDistanceSquared = despawnDistance * despawnDistance;

		enemyCullingTimer = new Timer();
		enemyCullingTimer.WaitTime = 5.0; // Check every 5 seconds
		enemyCullingTimer.Timeout += OnEnemyCullingTimerTimeout;
		AddChild(enemyCullingTimer);
		enemyCullingTimer.Start();
		
		//dev
		if (vars.HideDev) { GetNode<GridContainer>("UI/dev").Visible = false; }
		if (vars.HideHud) { GetNode<Control>("UI/WorldHud").Visible = false; }
    }

    private void OnEnemyCullingTimerTimeout() {
	    //get the player's position once.
	    Vector2 playerPos = PlayerGlobalPosition; 

	    var enemies = GetTree().GetNodesInGroup("enemies");
    
	    // GD.Print($"Culling check: {enemies.Count} enemies on screen.");

	    foreach (var node in enemies) {
		    if (node is CharacterBody2D enemy) {
			    // 5. Check squared distance (it's faster than DistanceTo)
			    if (enemy.GlobalPosition.DistanceSquaredTo(playerPos) > despawnDistanceSquared) {
				    enemy.QueueFree();
				    // GD.Print("ENEMY CULLED");
			    }
		    }
	    }
    }
    
    public override void _Process(double delta) {
	    if (isPlacing) {
		    tentPlacementPreview.GlobalPosition = GetGlobalMousePosition();
		    tentPreviewSprite.SelfModulate = isPlacementValid ? placementValidColor : placementInvalidColor;
	    }
    }

    public override void _PhysicsProcess(double delta) {
	    PlayerGlobalPosition = player.GlobalPosition;
    }

    
    
    private void TESTCUTSCENE() {
	    GetTree().Paused = true; // Pause the game
	    var cutscene = cutsceneManagerScene.Instantiate<CutsceneManager>();
	    AddChild(cutscene);
    
	    // cutscene.StartTextOnlyCutscene("Welcome to Confectionery Tale...");
	    cutscene.StartTextOnlyCutscene(
		    "Recruit: KANE",
		    "Training Session: 238",
		    "Day of departure.."
	    );
    }
    
    public override void _Input(InputEvent @event) {
	    if (Input.IsActionPressed("use_slot_one")) { UseTinctureHealth(); GetTree().GetRoot().SetInputAsHandled(); return; }
	    if (Input.IsActionPressed("use_slot_two")) { UseTinctureSpeed(); GetTree().GetRoot().SetInputAsHandled(); return; }
	    if (Input.IsActionPressed("use_slot_three")) { UseTinctureConceal(); GetTree().GetRoot().SetInputAsHandled(); return; }
	    if (Input.IsActionPressed("use_tent")) { BuildTent(); GetTree().GetRoot().SetInputAsHandled(); return; }
	    
	    //tent placement
	    if (isPlacing) {
		    //check for Left Click (Confirm Placement)
		    if (@event is InputEventMouseButton lmbEvent && lmbEvent.ButtonIndex == MouseButton.Left && lmbEvent.IsPressed()) {
			    if (isPlacementValid) { PlaceTent(); }
			    // else { GD.Print("Cannot place here: Location is obstructed."); }
			    GetTree().GetRoot().SetInputAsHandled(); //consume the event. Player.cs will never see this click
			    return;
		    }

		    //check for Right Click (Cancel Placement)
		    if (@event is InputEventMouseButton rmbEvent && rmbEvent.ButtonIndex == MouseButton.Right && rmbEvent.IsPressed()) {
			    // GD.Print("Placement cancelled.");
			    isPlacing = false;
			    tentPlacementPreview.Visible = false;
			    GetTree().GetRoot().SetInputAsHandled(); 
			    return;
		    }
	    }
    }
    
    public TileMapLayer GetTileMapWorld() {
	    return tileMapWorld;
    }
    
    private void GetAllShelters() {
	    shelterPlain = GetNode<StaticBody2D>("World/ShelterPlain");
		shelterCake = GetNode<StaticBody2D>("World/ShelterCake");
		shelterWoods = GetNode<StaticBody2D>("World/ShelterWoods");
		
		allShelters = new List<Shelter>();
		
		if (shelterPlain is Shelter plainScriptInstance) {
			shelterPlainScript = plainScriptInstance;
			allShelters.Add(shelterPlainScript);
		}
		
		if (shelterCake is Shelter cakeScriptInstance) {
			shelterCakeScript = cakeScriptInstance;
			allShelters.Add(shelterCakeScript);
		}
		
		if (shelterWoods is Shelter woodsScriptInstance) {
			shelterWoodsScript = woodsScriptInstance;
			allShelters.Add(shelterWoodsScript);
		}
		
		foreach (var s in allShelters) {
			s.SetUI(ui);
	    }
    }
    
    private void SetupSpawners() {
	    var allSpawners = new List<EnemySpawner>();
	    foreach (var spawner in GetTree().GetNodesInGroup("spawners")) {
		    
		    if (spawner is EnemySpawner enemySpawnerScript) { // 'worldObjectScript' is now of type WorldObject
			    allSpawners.Add(enemySpawnerScript);
		    } else {
			    GD.PushWarning($"Node '{spawner.Name}' in group 'worldobject' does not have a 'WorldObject.cs' script attached.");
		    }
	    }
	    foreach (var es in allSpawners) { es.SetupSpawner(GetPlayerCamera()); }
    }
    
    private void GetAllWorldObjects() {
	    allWorldObjects = new List<WorldObject>();
	    
	    foreach (var wo in GetTree().GetNodesInGroup("worldobject")) {
		    if (wo is WorldObject worldObjectScript) { // 'worldObjectScript' is now of type WorldObject
			    allWorldObjects.Add(worldObjectScript);
		    } else {
			    GD.PushWarning($"Node '{wo.Name}' in group 'worldobject' does not have a 'WorldObject.cs' script attached.");
		    }
	    }
	    foreach (var wo in allWorldObjects) { wo.SetUI(ui); }
    }
    
    public WorldObject GetWorldObjectById(string objectId) {
	    foreach (var wo in allWorldObjects) {
		    if (!objectId.Equals(wo.GetObjectId())) { continue; }
		    return wo;
	    }
	    return null;
    }

    private void RemoveOwnedWorldObjects() {
	    //this is a very hmm way of doing this, maybe figure out a better way
	    // if (vars.SoftenerOwned) { RemoveWorldObject("woSoftener"); }
	    
	    foreach (var worldObject in allWorldObjects) {
		    // if (!wo.GetObjectId().Equals(objectId)) { continue; }
		    // // GD.Print("removing " + wo.GetInteractId());
		    // wo.Remove();
		    // Check if its ID is in our "collected" list
		    // GD.Print("checking " + worldObject.objectId);
		    if (vars.CollectedWorldObjects.Contains(worldObject.objectId)) {
			    // GD.Print("found owned object");
			    worldObject.Remove();
		    }
	    }
    }
    
    public void RemoveWorldObject(string objectId) {
	    foreach (var wo in allWorldObjects) {
		    if (!wo.GetObjectId().Equals(objectId)) { continue; }
		    // GD.Print("removing " + wo.GetInteractId());
		    wo.Remove();
	    }
    }

    public void CheckAssignments() {
	    var assignmentToComplete = vars.CurrentWorldObject.assignmentId;
	    var assignmentToGain = vars.CurrentWorldObject.newAssignmentId;
            
	    //gaining and completing assignment
	    if (!string.IsNullOrEmpty(assignmentToComplete) && !string.IsNullOrEmpty(assignmentToGain)) {
		    assignments.GainAssignmentDataOnly(assignmentToGain);
		    assignments.CompleteAssignmentDataOnly(assignmentToComplete);
		    assignments.ShowGainedAssignmentPopup(assignmentToGain);
		    assignments.ShowCompleteAssignmentPopup(assignmentToComplete);
	    } else if (!string.IsNullOrEmpty(assignmentToComplete)) { //only completing
		    assignments.CompleteAssignment(assignmentToComplete);
	    } else if (!string.IsNullOrEmpty(assignmentToGain)) { //only gaining
		    assignments.GainAssignment(assignmentToGain);
	    }
    }
    
    [Export] private Texture2D sucroseTextureOne; //MOVE THESE
    [Export] private Texture2D sucroseTextureTen;
    [Export] private Texture2D sucroseTextureHundred;
    
    public void SpawnCurrencySucrose(Vector2 pos, double value) {
	    // 1. Calculate the final *total* value first.
	    // (Your original calculation had a logic issue: Math.Round(GetPlayerFinalSucroseDrop() * .01)
	    // If GetPlayerFinalSucroseDrop() is 150 (for 150%), this would be value * 2.
	    // A better way is (GetPlayerFinalSucroseDrop() / 100.0)
    
	    // Let's assume GetPlayerFinalSucroseDrop() returns 150 for +150%
	    double finalDropValue = value * (GetPlayerFinalSucroseDrop() / 100.0);
	    int totalSucroseToSpawn = (int)Math.Ceiling(finalDropValue); // Round up

	    int goldValue = 100;
	    int purpleValue = 10;
	    int pinkValue = 1; //this must be the last one

	    //use division and modulo to find out how many of each to spawn
	    int numGold = totalSucroseToSpawn / goldValue;       // Example: 152 / 100 = 1
	    int remainder = totalSucroseToSpawn % goldValue;     // Example: 152 % 100 = 52

	    int numPurples = remainder / purpleValue;            // Example: 52 / 10 = 5
	    int numPinks = remainder % purpleValue;              // Example: 52 % 10 = 2

	    for (int i = 0; i < numGold; i++) { SpawnSingleSucroseCoin(pos, goldValue); }
	    for (int i = 0; i < numPurples; i++) { SpawnSingleSucroseCoin(pos, purpleValue); }
	    for (int i = 0; i < numPinks; i++) { SpawnSingleSucroseCoin(pos, pinkValue); }
    }
    
    //helper to spawn one sucrose currency
    private void SpawnSingleSucroseCoin(Vector2 pos, int value) {
	    var currencySucrose = currencySucroseScene.Instantiate<CurrencySucrose>();

	    //small random offset so they don't all stack perfectly
	    Vector2 spawnPos = pos + new Vector2(GD.RandRange(-128, 128), GD.RandRange(-128, 128));
    
	    currencySucrose.SetPos(spawnPos);
	    currencySucrose.SetValue(value); 

	    Texture2D textureToPass;
	    if (value >= 100) { textureToPass = sucroseTextureHundred; }
	    else if (value >= 10) { textureToPass = sucroseTextureTen; }
	    else { textureToPass = sucroseTextureOne; }
	    currencySucrose.SetTexture(textureToPass);
	    CallDeferred("add_child", currencySucrose);
    }

    private void testspawnsucrose() {
	    var playerPosX = GetPlayerPosition().X;
	    var playerPosY = GetPlayerPosition().Y;
	    var spawnLoc = new Vector2(playerPosX + 1500, playerPosY);
	    SpawnCurrencySucrose(spawnLoc, 138);
    }
    
	// public void SpawnCurrencySucrose(Vector2 pos, double value) { //original -delete
	// 	var currencySucrose = currencySucroseScene.Instantiate<CurrencySucrose>();
	// 	value *= Math.Round((GetPlayerFinalSucroseDrop() * .01));
	// 	currencySucrose.SetPos(pos);
	// 	currencySucrose.SetValue(value);
	// 	CallDeferred("add_child", currencySucrose);
	// }
	
	public void GainAssignment(string assignment) {
		assignments.GainAssignment(assignment);
	}
	
	public void GainMaterial(BaseMaterial material, int amount) {
		GD.Print($"picking up {material.Id}");
		material.CurrentOwned += amount;
		material.TotalFound += amount;

		// ui.UpdateMaterialInventoryItem(material); //original -works
		ShowPopupInfo(PopupEventType.MaterialGained, amount: amount, specificItemName: material.Name);
		ui.ProcessMaterialForDisplay(material);
		EmitSignal(SignalName.MaterialsChanged);
		//if (!ui.UpdateMaterialInventoryItem(material)) { if (material.CurrentOwned - amount < 1) { ShowInventoryNotificationDot(item.ObjectType.ToString()); } } //maybe
	}
	
	public void LoseMaterial(BaseMaterial material, int amount) {
		material.CurrentOwned -= amount;
		// ui.UpdateMaterialInventoryItem(material); //original -works
		ui.ProcessMaterialForDisplay(material);
		EmitSignal(SignalName.MaterialsChanged);
	}
	
	public void EquipWeapon(int what) {
		if (what.Equals(vars.CurrentFireMode)) { return; }
		//foreach (var b in GetTree().GetNodesInGroup("bullets")) { b.QueueFree(); } //clear current bullets when swapping

		var owned = false;
		switch (what) { //wonky way to do this
			case 0: owned = true; break;
			case 1: owned = vars.SoftenerOwned; break;
			case 2: owned = vars.SpreaderOwned; break;
			case 3: owned = vars.SniperOwned; break;
			case 4: owned = vars.SlowerOwned; break;
			case 5: owned = vars.SmasherOwned; break;
		}
		if (!owned) { return; }
		
		SetCurrentWeaponData(what);
		ui.SetEquippedWeaponPanel();
		ui.UpdatePlayerSheet();
		// ui.UpdateGameHud(); //if using selectable slots at bottom of hud
		SetWeaponDataOnPlayer(); //needs to bet set before setting crosshairs -not ideal
		SetWeaponCrosshairs();
	}
	
	public void ChangeSucrose(double amount) {
		amount *= GetPlayerFinalSucroseDrop() * .01;
		var final = Math.Ceiling(amount);
		// GD.Print("gained sucrose: " + final);
		vars.CurrentSucrose += final;
		EmitSignal(SignalName.SucroseChanged);
	}
	
	private void InstantiatePlayer() {
		// TogglePlayerCamera(false);
		player = playerScene.Instantiate<Player>();
		//multiply tile coord by 256
		player.SetPos(new Vector2(27560, 59680)); //52 116 -test start
		// player.SetPos(new Vector2(-21600, 122400)); //-45, 255 -training area
		// player.SetPos(new Vector2(122880, 47616)); //480, 186 cake
		// player.SetPos(new Vector2(245760, 138240)); //960 540 bottom right corner
		
		playerCamera = player.GetPlayerCamera();
		// main.SucroseChanged += HandleSucroseChanged;
		player.ShelteredChanged += HandleShelteredChanged;
		AddChild(player);
		// player.FallTween();
	}

	public void SetPlayerVisible() { //this is called by the portal animation
		player.Visible = true;
		player.StartFallTween();
	}
	
	private void ManualSpawnNewEnemy() {
		var jellyData = GD.Load<BaseEnemy>("res://resources/enemy_jellybean.tres");
		var gumdropData = GD.Load<BaseEnemy>("res://resources/enemy_gumdrop.tres");
		var enemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn");

		var enemies = new List<BaseEnemy>();
		enemies.Add(jellyData);
		enemies.Add(gumdropData);
		var spawn = enemies[GD.RandRange(0, enemies.Count - 1)];
		
		Enemy enemy = enemyScene.Instantiate<Enemy>();
		enemy.SetThisEnemy(spawn);
		enemy.GlobalPosition = new Vector2(27560, 59680);
		GetTree().Root.AddChild(enemy);
	}
	
	private void HandleShelteredChanged() {
		foreach (var shelter in allShelters) {
			if (vars.IsSheltered) { shelter.SwapToInterior(); }
			else { shelter.SwapToExterior(); }
		}
	}
	
	public Sprite2D GetPlayerSpawnPortal() { return playerSpawnPortal; }

	private void PortalAnimationFinished(string anim) {
		playerSpawnPortal.Visible = false;
	}
	
	public void TeleportPlayer(string where) {
		var globalTravelPoint = new Vector2();
		var playTween = false;
		var whatAssignment = "";
		
		switch (where) {
			case "startingSpot": //RENAME (use capitals)
				vars.CutsceneActive = true;
				whatAssignment = "MA01";
				var start = GetNode<Node2D>("World/PlayerSpawnPointOne");
				globalTravelPoint = start.GetPosition();
				
				//tell the player to start the cutscene, passing the destination.
				player.StartTeleportCutscene(globalTravelPoint, whatAssignment);
				break;
			case "plain":
				globalTravelPoint = shelterPlainScript.ToGlobal(shelterPlainScript.GetTravelPoint()); 
				break;
			case "cake":
				globalTravelPoint = shelterCakeScript.ToGlobal(shelterCakeScript.GetTravelPoint()); 
				break;
			case "woods":
				globalTravelPoint = shelterWoodsScript.ToGlobal(shelterWoodsScript.GetTravelPoint()); 
				break;
		}
		player.SetPos(globalTravelPoint);
		ui.CloseWorldMap();
	}
	
	public void EnemyCollidedWithPlayer(BaseEnemy enemy) {
		// GD.Print("collided with: " + enemy.Id);
		player.PlayerGotHit(enemy.CollideDamage);
	}

	public void PlayerRestoreHealthOnHit(float percentHealth) {
		player.PlayerRestoreHealthOnHit(percentHealth);
	}
	
	public enum PopupEventType {
		PlayerLevelUp,
		PlayerSkillPointGained,
		SkillLevelUp,
		SkillExpGained,
		MaterialGained,
		ExtractGained,
	}
	
	public void ShowPopupInfo(PopupEventType eventType, string subjectName = null, int amount = 0, string specificItemName = null) {
		var popupText = popupTextScene.Instantiate<PopupText>();
		var text = "";
		var color = tooltips.GetDecimalColor("pink");
		var size = 128;
		var playerPos = GetPlayerPosition();
		var gpx = playerPos.X;
		var gpy = playerPos.Y - 350;

		switch (eventType) {
			case PopupEventType.PlayerLevelUp:
				text = $"REACHED LEVEL {amount}";
				if (amount % 2 == 0) {
					var timer = GetTree().CreateTimer(1.0f);
					timer.Timeout += OnLevelPopupTextTimerTimeout;
				}
				break;

			case PopupEventType.SkillLevelUp:
				text = $"REACHED {subjectName.ToUpper()} LVL {amount}";
				break;
            
			case PopupEventType.SkillExpGained:
				text = $"+ {amount} {subjectName.ToUpper()} EXP";
				break;

			case PopupEventType.MaterialGained:
				color = tooltips.GetDecimalColor("green");
				text = $"+{amount} {specificItemName}";
				break;

			case PopupEventType.ExtractGained:
				color = tooltips.GetDecimalColor("green");
				text = $"+{amount} {specificItemName} Extract";
				break;

			case PopupEventType.PlayerSkillPointGained:
				color = tooltips.GetDecimalColor("green");
				text = $"+{amount} Skill Point{(amount > 1 ? "s" : "")}";
				break;
		}

		popupText.RemoveStroke();
		popupText.SetColor(color);
		popupText.SetTextAndSize(text, size);
		popupText.SetWorldPosition(new Vector2(gpx, gpy));
		GetWorld().AddChild(popupText);
		popupText.FadeTween();
	}
	
	private void OnLevelPopupTextTimerTimeout() {
		ShowPopupInfo(PopupEventType.PlayerSkillPointGained, amount: 1);
	}

	private void CheckPlayerLevel() {
		if (vars.PlayerLevel >= 100) { return; }
		var maxExp = GetPlayerExpNext();
		if (vars.CurrentPlayerExp >= maxExp) {
			// settings.PlayLevelSound();
			vars.PlayerLevel += 1;
			// progress.CheckAchievement(36);
			ShowPopupInfo(PopupEventType.PlayerLevelUp, amount: vars.PlayerLevel);
			vars.CurrentPlayerExp -= maxExp;
			CheckPlayerLevel();
			ui.UpdateExpBar();
			ui.UpdateHealthBar(GetPlayerMaxHealth());
			ui.UpdateShieldBar(GetPlayerFinalShield());
			EmitSignal(SignalName.GainedPlayerLevel);
			vars.SaveGameData();
		}
	}

	public void GainPlayerExp(double amount) {
		if (vars.PlayerLevel >= 100) { return; }
		amount *= GetPlayerFinalExpDrop() * .01;
		var final = Math.Ceiling(amount);
		vars.CurrentPlayerExp += final;
		ui.UpdateExpBar();
		CheckPlayerLevel();
	}
	
	public double GetPlayerExpNext() {
		// return 5;
		if (vars.PlayerLevel >= 100) { return Math.Ceiling(Math.Pow((vars.PlayerLevel + 1), 2.6) * 100); } //100 is soft cap
		return Math.Ceiling(Math.Pow((vars.PlayerLevel + 1), 1.6) * 100);
	}

	//areas
	public int GetTotalDiscoveredAreas() {
		string[] areaNames = ["Plain", "Grove", "Falls", "Bridge", "Mountain", "Cake", "Point", "Swamp", "Woods", "Sucrose"];
		var total = 0;
		foreach (var areaName in areaNames) { if (GetIsAreaDiscovered(areaName)) { total += 1; } }
		return total;
	}

	public bool GetIsAreaDiscovered(string area) {
		var discovered = vars.DiscoveredAreas.Contains(area);
		// GD.Print($"{area} Discovered: {discovered}");
		return discovered;
	}
	
	//world skills
	
	//cracking
	private void CheckCrackingLevel() {
		var maxExp = GetCrackingExpNext();
		if (vars.SkillCrackingCurrentExp >= maxExp) {
			// settings.PlayLevelSound();
			vars.SkillCrackingLevel += 1;
			// progress.CheckAchievement(36);
			vars.SkillCrackingCurrentExp -= maxExp;
			CheckCrackingLevel();
			player.SetCrackingRadius();
			vars.SaveGameData();
		}
	}

	public void GainCrackingExp(int amount) {
		int oldLevel = vars.SkillCrackingLevel;

		vars.SkillCrackingCurrentExp += amount;
		CheckCrackingLevel();
    
		int newLevel = vars.SkillCrackingLevel;
    
		ShowPopupInfo(PopupEventType.SkillExpGained, subjectName: "Cracking", amount: amount);

		if (newLevel > oldLevel) {
			var timer = GetTree().CreateTimer(1.0f);
			timer.Timeout += () => OnSkillLevelUpPopupTimeout("Cracking", newLevel);
		}
	}

	private void OnSkillLevelUpPopupTimeout(string skillId, int newLevel) {
		ShowPopupInfo(PopupEventType.SkillLevelUp, subjectName: skillId, amount: newLevel);
	}
	
	private void testgaincrackingexp() { //for testing -delete
		GainCrackingExp(GetCrackingExpNext());
		// GainCrackingExp(1);
		skills.UpdateWorldSkillsUI();
	}
	
	public int GetCrackingExpNext() {
		return 2 + vars.SkillCrackingLevel;
		// if (vars.SkillCrackingLevel >= 100) { return Math.Ceiling(Math.Pow((vars.PlayerLevel + 1), 2.6) * 100); } //100 is soft cap
		// return Math.Ceiling(Math.Pow((vars.SkillCrackingLevel + 1), 1.6) * 100);
	}
	
	public int GetCrackingSlowRadius() {
		return 500 + (vars.SkillCrackingLevel * 50);
	}
	
	public float GetCrackingSpeed() { return 1 + (vars.SkillCrackingLevel * .1f); }

	public float GetCrackingSpeedPercent() {
		if (vars == null) { return 1.0f; }
		// Base speed (1.0 or 100%) + 10% (.1f) increase per level
		// Example: Level 10 means 1.0 + (10 * 0.1) = 2.0 (200%)
		return 1.0f + (vars.SkillCrackingLevel * 0.1f); 
	}
	
	//cleansing
	public double GetCleanseTime() { //seconds
		return 3;
	} //need radius maybe
	
	//camping
	public void GainCampingLevel() {
		vars.SkillCampingLevel += 1;
		if (vars.SkillCampingLevel == 1) { return; }
		var timer = GetTree().CreateTimer(1.0f);
		timer.Timeout += () => OnSkillLevelUpPopupTimeout("Camping", vars.SkillCampingLevel);
		vars.SaveGameData();
	}
	
	private void BuildTent() {
		// GD.Print(vars.TentCooldownTimer);
		if (vars.TentCooldownTimer > 0) { return; }
		if (tentScene == null) return;
		isPlacing = true;
		placementCollisions.Clear();
		tentPreviewSprite.Texture = tentTexture;
		tentPlacementPreview.Visible = true;
		tentPreviewSprite.SelfModulate = placementValidColor;
	}

	private void PlaceTent() {
		if (placementCollisions.Count > 0) { return; }
		tent = tentScene.Instantiate<Tent>();
		tent.GlobalPosition = tentPlacementPreview.GlobalPosition;
		AddChild(tent);
		isPlacing = false;
		tentPlacementPreview.Visible = false;
		tent.BeginTentBuild();
		vars.TentCooldownTimer = GetTentBuildCooldown() * 60.0;
		ui.ShowCampButtonCooldown();
	}

	public double GetTentBuildTime() {
		return 0;
		return 4; //for testing -delete
		return 10 - vars.SkillCampingLevel;
	}

	public double GetTentBuildCooldown() {
		return 10 - (vars.SkillCampingLevel * 0.5f);
	}

	private void OnPreviewAreaEntered(Area2D area) {
		if (!placementCollisions.Contains(area)) { placementCollisions.Add(area); }
	}

	private void OnPreviewAreaExited(Area2D area) {
		if (placementCollisions.Contains(area)) { placementCollisions.Remove(area); }
	}
	
	//consumables
	private void UseTinctureHealth() {
		if (vars.TinctureHealthCooldown > 0) { return; }
		if (vars.TinctureHealthAmount < 1) { return; }
		if (vars.CurrentPlayerHealth >= GetPlayerMaxHealth()) { return; }
		vars.TinctureHealthAmount -= 1;
		vars.TinctureHealthCooldown = 3;
		player.UseTinctureHealth(GetTinctureHealthPercent());
		ui.UpdateTinctureHealthButton();
	}

	private float GetTinctureHealthPercent() {
		return .2f;
	}
	
	private void UseTinctureSpeed() {
		if (vars.TinctureSpeedCooldown > 0) { return; }
		if (vars.TinctureSpeedAmount < 1) { return; }
		if (tinctureSpeedIncrease > 0) { return; }
		vars.TinctureSpeedAmount -= 1;
		vars.TinctureSpeedCooldown = 5;
		ui.UpdateTinctureSpeedButton();
		SetTinctureSpeedIncrease(1000);
	}

	private int GetTinctureSpeed() {
		return 2000;
		return vars.SkillCraftingLevel * tinctureSpeedIncrease;
	}

	public void SetTinctureSpeedIncrease(int amount) {
		tinctureSpeedIncrease = amount;
	}
	
	private void UseTinctureConceal() {
		if (vars.TinctureConcealCooldown > 0) { return; }
		if (vars.TinctureConcealAmount < 1) { return; }
		vars.TinctureConcealAmount -= 1;
		vars.TinctureConcealCooldown = GetConcealTime();
		
		ui.UpdateTinctureConcealButton();
	}

	private int GetConcealTime() {
		return 8;
	}

	//player stats
	public float GetPlayerHealthRegenRate() {
		return 2f;
	}
	
	//player health
	public int GetPlayerLevelHealth() { return vars.PlayerLevel * 10; }
	public int GetPlayerExtractHealth() { return GetEquippedExtractStatValue("Health"); }
	public int GetPlayerMaxHealth() { return GetPlayerLevelHealth() + GetPlayerExtractHealth(); }

	//player damage
	public double GetPlayerLevelDamage() { return Math.Ceiling(Math.Pow(vars.PlayerLevel, 1.35)); }
	// public double GetPlayerLevelDamage() { return 0; } //for testing -delete
	public double GetPlayerExtractDamage() { return GetEquippedExtractStatValue("Damage"); }
	public double GetPlayerEquippedWeaponDamage() { return currentWeaponData.Damage; }

	public float GetSkillDamageEffect(string bulletType) {
		switch (bulletType) {
			case "bulletSprayer": return 1 + GetSkillPointsEffect("SprayerDamage", vars.SkillSprayerDamage);
			case "bulletSoftener": return 1 + GetSkillPointsEffect("SoftenerDamage", vars.SkillSoftenerDamage);
			case "bulletSpreader": return 1 + GetSkillPointsEffect("SpreaderDamage", vars.SkillSpreaderDamage);
			case "bulletSniper": return 1 + GetSkillPointsEffect("SniperDamage", vars.SkillSniperDamage);
			case "bulletSlower": return 1 + GetSkillPointsEffect("SlowerDamage", vars.SkillSlowerDamage);
			case "bulletSmasher": return 1 + GetSkillPointsEffect("SmasherDamage", vars.SkillSmasherDamage);
		}
		return 1;
	}
	
	public double GetPlayerFinalDamage() {
		var final = GetPlayerLevelDamage() + GetPlayerExtractDamage() + GetPlayerEquippedWeaponDamage();
		final *= GetSkillDamageEffect(currentWeaponData.Id);
		return Math.Ceiling(final);
	}
	//player damage
	
	//pierce
	public int GetPlayerFinalPierce() {
		var final = GetPlayerExtractPierce() + GetPlayerEquippedWeaponPierce();
		final += (int) GetSkillPierceEffect(currentWeaponData.Id);
		return final;
	}

	public float GetSkillPierceEffect(string bulletType) {
		switch (bulletType) {
			// case "bulletSprayer": return 1 + GetTotalSkillPierceEffect("SprayerPierce", vars.SkillSprayerPierce); //not used
			case "bulletSoftener": return GetSkillPointsEffect("SoftenerPierce", vars.SkillSoftenerPierce);
			// case "bulletSpreader": return 1 + GetTotalSkillPierceEffect("SpreaderPierce", vars.SkillSpreaderPierce); //not used
			// case "bulletSniper": return 1 + GetTotalSkillPierceEffect("SniperPierce", vars.SkillSniperPierce); //not used
			// case "bulletSlower": return 1 + GetTotalSkillPierceEffect("SlowerPierce", vars.SkillSlowerPierce); //not used
			// case "bulletSmasher": return 1 + GetTotalSkillPierceEffect("SmasherPierce", vars.SkillSmasherPierce); //not used
		}
		return 0;
	}
	
	public int GetPlayerExtractPierce() { return GetEquippedExtractStatValue("Pierce"); }
	public int GetPlayerEquippedWeaponPierce() { return currentWeaponData.Pierce; }
	//pierce
	
	//crit
	public int GetPlayerFinalCritChance() {
		var final = GetPlayerExtractCritChance() + GetPlayerEquippedWeaponCritChance();
		final += (int) GetSkillCritChanceEffect(currentWeaponData.Id);
		return final;
	}
	public float GetSkillCritChanceEffect(string bulletType) {
		switch (bulletType) {
			case "bulletSprayer": return GetSkillPointsEffect("SprayerCritChance", vars.SkillSprayerCritChance);
			// case "bulletSoftener": return 1 + GetSkillPointsEffect("SoftenerCritChance", vars.SkillSoftenerCritChance);
			// case "bulletSpreader": return 1 + GetSkillPointsEffect("SpreaderCritChance", vars.SkillSpreaderCritChance);
			// case "bulletSniper": return 1 + GetSkillPointsEffect("SniperCritChance", vars.SkillSniperCritChance);
			// case "bulletSlower": return 1 + GetSkillPointsEffect("SlowerCritChance", vars.SkillSlowerCritChance);
			// case "bulletSmasher": return 1 + GetSkillPointsEffect("SmasherCritChance", vars.SkillSmasherCritChance);
		}
		return 0;
	}
	
	public int GetPlayerExtractCritChance() { return GetEquippedExtractStatValue("Crit Chance"); }
	public int GetPlayerEquippedWeaponCritChance() { return currentWeaponData.Crit; }
	
	public int GetPlayerFinalCritDamage() { return GetPlayerExtractCritDamage() + GetPlayerEquippedWeaponCritDamage(); }
	public int GetPlayerExtractCritDamage() { return GetEquippedExtractStatValue("Crit Damage"); }
	public int GetPlayerEquippedWeaponCritDamage() { return currentWeaponData.CritDamage; }
	//crit
	
	//knockback
	public int GetPlayerFinalKnockback() {
		var final = GetPlayerExtractKnockback() + GetPlayerEquippedWeaponKnockback();
		final += GetSkillKnockbackEffect(currentWeaponData.Id);
		return (int) Math.Ceiling(final);
	}
	
	public double GetPlayerExtractKnockback() {
		return 0;
		// return ui.GetExtractStatValue("Knockback"); //DOESN'T YET EXIST
	}
	public double GetPlayerEquippedWeaponKnockback() { return currentWeaponData.Knockback; }

	public float GetSkillKnockbackEffect(string bulletType) {
		switch (bulletType) {
			case "bulletSprayer": return GetSkillPointsEffect("SprayerKnockback", vars.SkillSprayerKnockback);
			// case "bulletSoftener": return GetSkillPointsEffect("SoftenerKnockback", vars.SkillSoftenerKnockback);
			case "bulletSpreader": return GetSkillPointsEffect("SpreaderKnockback", vars.SkillSpreaderKnockback);
			// case "bulletSniper": return GetSkillPointsEffect("SniperKnockback", vars.SkillSniperKnockback);
			// case "bulletSlower": return GetSkillPointsEffect("SlowerKnockback", vars.SkillSlowerKnockback);
			case "bulletSmasher": return GetSkillPointsEffect("SmasherKnockback", vars.SkillSmasherKnockback);
		}
		return 0;
	}
	//knockback
	
	//melt
	public int GetPlayerMeltDamage() {
		return (int) Math.Ceiling(GetPlayerFinalDamage() * .1f);
	}

	public float GetPlayerMeltDuration() {
		var meltDuration = currentWeaponData.MeltDuration;
		meltDuration += GetSkillMeltEffect(currentWeaponData.Id);
		return meltDuration;
	}
	
	public float GetSkillMeltEffect(string bulletType) {
		switch (bulletType) {
			case "bulletSprayer": return GetSkillPointsEffect("SprayerMelt", vars.SkillSprayerMelt);
			// case "bulletSoftener": return GetSkillPointsEffect("SoftenerMelt", vars.SkillSoftenerMelt);
			// case "bulletSpreader": return GetSkillPointsEffect("SpreaderMelt", vars.SkillSpreaderMelt);
			// case "bulletSniper": return GetSkillPointsEffect("SniperMelt", vars.SkillSniperMelt);
			case "bulletSlower": return GetSkillPointsEffect("SlowerMelt", vars.SkillSlowerMelt);
			case "bulletSmasher": return GetSkillPointsEffect("SmasherMelt", vars.SkillSmasherMelt);
		}
		return 0;
	}
	
	public int GetPlayerMeltResist() { return 100; }
	//melt
	
	//chill
	public float GetPlayerChillDuration() {
		var chillDuration = currentWeaponData.ChillDuration;
		chillDuration += GetSkillChillEffect(currentWeaponData.Id);
		return chillDuration;
	}
	
	public float GetSkillChillEffect(string bulletType) {
		switch (bulletType) {
			// case "bulletSprayer": return GetSkillPointsEffect("SprayerChill", vars.SkillSprayerChill);
			case "bulletSoftener": return GetSkillPointsEffect("SoftenerChill", vars.SkillSoftenerChill);
			// case "bulletSpreader": return GetSkillPointsEffect("SpreaderChill", vars.SkillSpreaderChill);
			// case "bulletSniper": return GetSkillPointsEffect("SniperChill", vars.SkillSniperChill);
			// case "bulletSlower": return GetSkillPointsEffect("SlowerChill", vars.SkillSlowerChill);
			case "bulletSmasher": return GetSkillPointsEffect("SmasherChill", vars.SkillSmasherChill);
		}
		return 0;
	}
	
	public int GetPlayerChillResist() { return 100; }
	//chill
	
	//reload
	public float GetFinalWeaponReload() {
		var baseReloadTime = GetPlayerEquippedWeaponReload();
    
		//get the total time reduction multiplier (e.g., 0.5 for 50% less time)
		var timeMultiplier = 1.0f - GetSkillReloadEffect(currentWeaponData.Id); 
	
		//ensure multiplier is not negative
		if (timeMultiplier < 0) { timeMultiplier = 0; } 
    
		var finalTime = baseReloadTime * timeMultiplier;
    
		// GD.Print($"Reload Base: {baseReloadTime} | Multiplier: {timeMultiplier} | Final: {finalTime}");
		return finalTime;
	}
	
	// public double GetPlayerExtractReload() { return ui.GetExtractStatValue("ReloadSpeed"); } //not used
	public float GetPlayerEquippedWeaponReload() { return currentWeaponData.ReloadSpeed; }
	
	public float GetSkillReloadEffect(string bulletType) { //original
		switch (bulletType) {
			// case "bulletSprayer": return GetSkillPointsEffect("SprayerReload", vars.SkillSprayerReload);
			// case "bulletSoftener": return GetSkillPointsEffect("SoftenerReload", vars.SkillSoftenerReload);
			case "bulletSpreader": return GetSkillPointsEffect("SpreaderReload", vars.SkillSpreaderReload);
			case "bulletSniper": return GetSkillPointsEffect("SniperReload", vars.SkillSniperReload);
			case "bulletSlower": return GetSkillPointsEffect("SlowerReload", vars.SkillSlowerReload);
			// case "bulletSmasher": return GetSkillPointsEffect("SmasherReload", vars.SkillSmasherReload);
		}
		return 0;
	}
	//reload
	
	//instant kill
	public int GetPlayerFinalInstantKillChance() {
		return (int) GetSkillInstantKillEffect(currentWeaponData.Id);
	}
	public float GetSkillInstantKillEffect(string bulletType) {
		switch (bulletType) {
			case "bulletSniper": return GetSkillPointsEffect("SniperInstant", vars.SkillSniperInstant);
		}
		return 0;
	}
	//instant kill
	
	//health on hit
	public float GetPlayerFinalHealthOnHit() {
		return GetSkillHealthOnHitEffect(currentWeaponData.Id);
	}
	public float GetSkillHealthOnHitEffect(string bulletType) {
		switch (bulletType) {
			// case "bulletSprayer": return GetSkillPointsEffect("SprayerRestore", vars.SkillSprayerRestore);
			// case "bulletSoftener": return GetSkillPointsEffect("SoftenerRestore", vars.SkillSoftenerRestore);
			// case "bulletSpreader": return GetSkillPointsEffect("SpreaderRestore", vars.SkillSpreaderRestore);
			case "bulletSniper": return GetSkillPointsEffect("SniperRestore", vars.SkillSniperRestore);
			// case "bulletSlower": return GetSkillPointsEffect("SlowerRestore", vars.SkillSlowerRestore);
			// case "bulletSmasher": return GetSkillPointsEffect("SmasherRestore", vars.SkillSmasherRestore);
		}
		return 0;
	}
	//health on hit
	
	//bullet size
	public Vector2 GetPlayerFinalBulletSize() {
		var final = GetPlayerEquippedWeaponBulletSize() * GetPlayerExtractBulletSize() * GetSkillBulletSize(currentWeaponData.Id);
		// final *= (int) skillMultiplier;
		return new Vector2(final, final);
	}

	public int GetPlayerExtractBulletSize() {
		return 1; //multiplier
		// return ui.GetExtractStatValue("BulletSize"); //DOESN'T YET EXIST
	}
	public int GetPlayerEquippedWeaponBulletSize() { return currentWeaponData.Size; }

	public float GetSkillBulletSize(string bulletType) {
		switch (bulletType) {
			// case "bulletSprayer": return 1 + GetSkillPointsEffect("SprayerSize", vars.SkillSprayerSize);
			case "bulletSoftener": return 1 + GetSkillPointsEffect("SoftenerSize", vars.SkillSoftenerSize);
			// case "bulletSpreader": return 1 + GetSkillPointsEffect("SpreaderSize", vars.SkillSpreaderSize);
			// case "bulletSniper": return 1 + GetSkillPointsEffect("SniperSize", vars.SkillSniperSize);
			case "bulletSlower": return 1 + GetSkillPointsEffect("SlowerSize", vars.SkillSlowerSize);
			// case "bulletSmasher": return 1 + GetSkillPointsEffect("SmasherSize", vars.SkillSmasherSize);
		}
		return 1;
	}
	//bullet size
	
	//bullet amount
	public int GetPlayerFinalBulletAmount() {
		var finalAmount = 3;
		finalAmount += GetSkillSpreaderBulletAmount();
		return finalAmount;
	}

	public int GetSkillSpreaderBulletAmount() {
		return (int) GetSkillPointsEffect("SpreaderBullets", vars.SkillSpreaderBullets);
	}
	
	/*GET ALL POINTS FOR A GIVEN SKILL*/
	public float GetSkillPointsEffect(string skillId, int currentPoints) { //rename
		var skillData = skills.GetSkillData(skillId);
		float[] allocated = skillData.Effects;
		var finalEffect = 0f;
		for (int i = 0; i < Math.Min(currentPoints, allocated.Length); i++) {
			finalEffect += allocated[i];
		}
		return finalEffect;
	}
	
	public int GetSkillLevelForWeapon(string weaponType, string statName) {
		switch (weaponType + statName) {
			case "SprayerDamage": return vars.SkillSprayerDamage;
			case "SoftenerDamage": return vars.SkillSoftenerDamage;
			case "SpreaderDamage": return vars.SkillSpreaderDamage;
			case "SniperDamage": return vars.SkillSniperDamage;
			case "SlowerDamage": return vars.SkillSlowerDamage;
			case "SmasherDamage": return vars.SkillSmasherDamage;
			default: return 0;
		}
	}
	
	//player speed
	public int GetPlayerLevelSpeed() { return 1200 + (vars.PlayerLevel * 5); }
	public int GetPlayerExtractSpeed() { return GetEquippedExtractStatValue("Speed") * 100; }
	public int GetPlayerFinalSpeed() { return GetPlayerLevelSpeed() + GetPlayerExtractSpeed() + GetTinctureSpeed(); }
	// public int GetPlayerFinalSpeed() { return 3000; } //for testing -delete
	
	//player shield
	public int GetPlayerLevelShield() { return vars.PlayerLevel + 5; }
	public int GetPlayerExtractShield() { return GetEquippedExtractStatValue("Shield"); }
	public int GetPlayerFinalShield() { return GetPlayerLevelShield() + GetPlayerExtractShield(); }
	
	//player shield regen
	public int GetPlayerLevelShieldRegen() { return 1; }
	public int GetPlayerExtractShieldRegen() { return GetEquippedExtractStatValue("Shield Regen"); }
	public int GetPlayerFinalShieldRegen() { return GetPlayerLevelShieldRegen() + GetPlayerExtractShieldRegen(); }
	
	public float GetFinalWeaponSpeed() { return currentWeaponData.Speed; }
	public int GetFinalWeaponRange() { return currentWeaponData.Range; }
	
	//extract drop
	public float GetPlayerLevelExtractDropChance() { return vars.PlayerLevel * .01f; }
	public float GetAreaRankExtractDropChance() { return GetAreaRank(vars.CurrentArea) * .1f; }
	public float GetEquippedExtractsDropChance() { return GetEquippedExtractStatValue("Extract Drop") * .1f; }
	public float GetPlayerFinalExtractDropChanceDisplay() { return (GetPlayerLevelExtractDropChance() + GetEquippedExtractsDropChance()); }
	public float GetPlayerFinalExtractDropChance(int enemyRank) { //probably not done
		// return 100; //for testing -delete
		var enemyRankChance = enemyRank * .01;
		var final = ((GetPlayerLevelExtractDropChance() + GetEquippedExtractsDropChance() + GetAreaRankExtractDropChance()) * enemyRankChance);
		return (float) final;
	}
	
	//sucrose drop
	public int GetPlayerLevelSucroseDrop() { return 0; }
	public int GetPlayerExtractSucroseDrop() { return GetEquippedExtractStatValue("Sucrose Drop"); }
	public int GetPlayerFinalSucroseDrop() { return 100 + GetPlayerExtractSucroseDrop(); }
	
	//exp drop
	public int GetPlayerLevelExpDrop() { return 0; }
	public int GetPlayerExtractExpDrop() { return GetEquippedExtractStatValue("Exp Drop"); }
	public int GetPlayerFinalExpDrop() { return 100 + GetPlayerExtractExpDrop(); }
	
	//player pickup range
	public int GetPlayerLevelPickupRange() { return (vars.PlayerLevel * 5) + 500; }
	public int GetPlayerExtractPickupRange() { return GetEquippedExtractStatValue("Pickup Range"); }
	public int GetPlayerFinalPickupRange() { return (GetPlayerLevelPickupRange() + GetPlayerExtractPickupRange()); }

	private int GetEquippedExtractStatValue(string stat) {
		var value = 0;
		
		var equippedExtracts = vars.savedResources.equippedExtracts;
		if (equippedExtracts == null) return 0;
		
		foreach (var extract in equippedExtracts) {
			switch (stat) {
				case "Damage": value += extract.ExtractBaseDamage; break;
				case "Pierce": value += extract.ExtractBasePierce; break;
				case "Crit Chance": value += extract.ExtractBaseCritChance; break;
				case "Crit Damage": value += extract.ExtractBaseCritDamage; break;
				case "Shield": value += extract.ExtractBaseShield; break;
				case "Shield Regen": value += extract.ExtractBaseShieldRegen; break;
				case "Health": value += extract.ExtractBaseHealth; break;
				case "Pickup Range": value += extract.ExtractBasePickupRange; break;
				case "Speed": value += extract.ExtractBaseSpeed; break;
				case "Extract Drop": value += extract.ExtractBaseExtractDrop; break;
				case "Sucrose Drop": value += extract.ExtractBaseSucroseDrop; break;
				case "Exp Drop": value += extract.ExtractBaseExpGain; break;
			}
		}
		return value;
	}
	
	public void SetCurrentWeaponData(int data) {
		currentWeaponData = weaponDataList[data];
		vars.CurrentFireMode = data; //maybe
	}

	public void SetWeaponDataOnPlayer() {
		player.SetCurrentWeaponData();
	}
	
	public void SetWeaponCrosshairs() {
		player.SetMouseCursor();
	}
	
	public Vector2 GetPlayerPosition() {
		var pos = player.Call("GetPlayerPosition");
		return (Vector2) pos;
	}
	public Camera2D GetPlayerCamera() { return playerCamera; }
	
	public bool GetInventoryFull() { return ui.GetExtractsInventoryFull(); }
	
	public int GetAreaRank(string area) {
		switch (area) {
			case "Plain": return vars.PlainRank;
			case "Grove": return vars.GroveRank;
			case "Falls": return vars.FallsRank;
			case "Cake": return vars.CakeRank;
			case "Swamp": return vars.SwampRank;
			case "Woods": return vars.WoodsRank;
			case "Point": return vars.PointRank;
		}
		return 0;
	}
	
	public Node2D GetWorld() { return GetNode<Node2D>("World"); }
	
	public WeaponData GetCurrentWeaponData() { return currentWeaponData; }
	public List<WeaponData> GetAllWeaponData() { return weaponDataList; }
	
	private void BuildWeaponData() {
		var sprayerTexture = GD.Load<Texture2D>("res://assets/sprites/bullet-sprayer.png");
		var softenerTexture = GD.Load<Texture2D>("res://assets/sprites/bullet-softener.png");
		var spreaderTexture = GD.Load<Texture2D>("res://assets/sprites/bullet-spreader.png");
		var sniperTexture = GD.Load<Texture2D>("res://assets/sprites/bullet-sniper.png");
		var slowerTexture = GD.Load<Texture2D>("res://assets/sprites/bullet-slower.png");
		var aoeTexture = GD.Load<Texture2D>("res://assets/sprites/bullet-smasher.png");
		var sprayerCrosshairs = ResourceLoader.Load<Texture2D>("res://assets/sprites/crosshairs-x-16x16.png");
		var softenerCrosshairs = ResourceLoader.Load<Texture2D>("res://assets/sprites/crosshairs-o-120x120.png");
		var spreaderCrosshairs = ResourceLoader.Load<Texture2D>("res://assets/sprites/crosshairs-u-84x144.png");
		var sniperCrosshairs = ResourceLoader.Load<Texture2D>("res://assets/sprites/crosshairs-+-120x120.png");
		var slowerCrosshairs = ResourceLoader.Load<Texture2D>("res://assets/sprites/crosshairs-v-120x120.png");
		var aoeCrosshairs = ResourceLoader.Load<Texture2D>("res://assets/sprites/crosshairs-v-120x120.png");

		weaponDataList = new List<WeaponData>();
		weaponDataList.Add(new WeaponData(sprayerTexture, sprayerCrosshairs, "bulletSprayer", "Sprayer", "Fast bullet that fires quickly",
			0, "auto", 1, .2f, 180, 4000, 1, 0, 1, 2, 0, 0, 1));
		
		weaponDataList.Add(new WeaponData(softenerTexture, softenerCrosshairs, "bulletSoftener", "Softener", "Large bullet that has\n high pierce and applies melt",
			1, "auto", 2, 1f, 120, 1200, 8, 0, 1, 2, 5, 0, 1));
		
		weaponDataList.Add(new WeaponData(spreaderTexture, spreaderCrosshairs, "bulletSpreader", "Spreader", "Three bullets that fire in a cone",
			2, "auto", 1, .5f, 180, 2000, 1, 0, 1, 2, 0, 0, 1));
		
		weaponDataList.Add(new WeaponData(sniperTexture, sniperCrosshairs, "bulletSniper", "Sniper", "High damage and crit,\nslow reload speed",
			3, "auto", 29, 2f, 300, 6000, 1, 5, 5, 2, 0, 0, 1));
		
		weaponDataList.Add(new WeaponData(slowerTexture, slowerCrosshairs, "bulletSlower", "Slower", "Large bullet that has\n high pierce and applies chill",
			4, "auto", 1, 1f, 80, 2500, 8, 10, 0, 2, 0, 5, 1));
		
		weaponDataList.Add(new WeaponData(aoeTexture, aoeCrosshairs, "bulletSmasher", "Smasher", "A large AoE with knockback",
			5, "auto", 49, 5, 80, 1800, 8, 30, 5, 2, 0, 0, 3));
		SetCurrentWeaponData(0); //DON'T DO THIS HERE -actually maybe keep here
	}

	//dev
	private void gainsucrose() {
		ChangeSucrose(600);
		// ChangeSucrose(50000);
	}
	
	private void removesucrose() {
		ChangeSucrose(-100);
		// ChangeSucrose(50000);
	}
	
	private void testgainlevel() {
		GainPlayerExp(GetPlayerExpNext());
	}
	
	private void additem() {
		// foreach (var item in allInventoryItems) {
		//     if (item.Id.Equals("testModOne")) {
		//         GainItem(item);
		//     }
		// }
	}
}