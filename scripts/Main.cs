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
	
	private Sprite2D playerSpawnPortal;
	private StaticBody2D shelterPlain;
	private StaticBody2D shelterGrove;
	private Shelter shelterPlainScript;
	private Shelter shelterGroveScript;
	private List<Shelter> allShelters;
	private List<WorldObject> allWorldObjects;
	private List<Node2D> placementCollisions = new List<Node2D>();
	private readonly Color placementValidColor = new Color(0.5f, 1f, 0.5f, 0.7f); // Green tint
	private readonly Color placementInvalidColor = new Color(1f, 0.5f, 0.5f, 0.7f); // Red tint
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
		
		//dev
		if (vars.HideDev) { GetNode<GridContainer>("UI/dev").Visible = false; }
		if (vars.HideHud) { GetNode<Control>("UI/WorldHud").Visible = false; }
    }

    public override void _Process(double delta) {
	    if (isPlacing) {
		    tentPlacementPreview.GlobalPosition = GetGlobalMousePosition();
		    tentPreviewSprite.SelfModulate = isPlacementValid ? placementValidColor : placementInvalidColor;
	    }
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
		// shelterGrove = GetNode<StaticBody2D>("World/ShelterGrove");
		
		allShelters = new List<Shelter>();
		
		if (shelterPlain is Shelter plainScriptInstance) {
			shelterPlainScript = plainScriptInstance;
			allShelters.Add(shelterPlainScript);
		}
		
		// if (shelterGrove is Shelter groveScriptInstance) {
		// 	shelterGroveScript = groveScriptInstance;
		// 	allShelters.Add(shelterGroveScript);
		// }
		
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
			    //todo conflict, spawners and world objects are in same group
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
    }
    
    public void RemoveWorldObject(string objectId) {
	    foreach (var wo in allWorldObjects) {
		    if (!wo.GetObjectId().Equals(objectId)) { continue; }
		    // GD.Print("removing " + wo.GetInteractId());
		    wo.Remove();
	    }
    }

	public void SpawnCurrencySucrose(Vector2 pos, double value) {
		var currencySucrose = currencySucroseScene.Instantiate<CurrencySucrose>();
		value *= Math.Round((GetPlayerFinalSucroseDrop() * .01));
		currencySucrose.SetPos(pos);
		currencySucrose.SetValue(value);
		CallDeferred("add_child", currencySucrose);
	}
	
	public void GainAssignment(string assignment) {
		assignments.GainAssignment(assignment);
	}
	
	public void GainMaterial(BaseMaterial material, int amount) {
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
		//main.SetCurrentWeaponData(vars.CurrentFireMode); //hm
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
				// GetTree().Paused = true; //too complicated to use
				vars.CutsceneActive = true;
				whatAssignment = "MA01";
				var start = GetNode<Node2D>("World/PlayerSpawnPointOne");
				globalTravelPoint = start.GetPosition();
				
				// Tell the player to start the cutscene, passing the destination.
				player.StartTeleportCutscene(globalTravelPoint, whatAssignment);
				// player.StartTeleportCutscene(textCutsceneScene, globalTravelPoint, whatAssignment); //delete
				// playTween = true; //original -delete
				break;
			case "plain":
				globalTravelPoint = shelterPlainScript.ToGlobal(shelterPlainScript.GetTravelPoint()); 
				shelterPlainScript.SwapToInterior(); //DON'T DO IT LIKE THIS -delete
				break;
			case "grove":
				globalTravelPoint = shelterGroveScript.ToGlobal(shelterGroveScript.GetTravelPoint()); 
				shelterGroveScript.SwapToInterior(); //DON'T DO IT LIKE THIS -delete
				break;
		}
		player.SetPos(globalTravelPoint);
		ui.CloseWorldMap();
		// if (playTween) { player.TeleportAnimation(whatAssignment); } //original -delete
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
		amount *= GetPlayerFinalExpDrop() * .01;
		var final = Math.Ceiling(amount);
		// GD.Print("gained exp: " + final);
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
		return 1 + vars.SkillCrackingLevel;
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
		vars.TinctureSpeedCooldown = 3;
		ui.UpdateTinctureSpeedButton();
		tinctureSpeedIncrease = 200;
	}

	private int GetTinctureSpeed() {
		return vars.SkillCraftingLevel * tinctureSpeedIncrease;
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
	public int GetPlayerModHealth() { return ui.GetExtractStatValue("Health"); }
	public int GetPlayerMaxHealth() { return GetPlayerLevelHealth() + GetPlayerModHealth(); }

	//player damage
	public double GetPlayerLevelDamage() { return Math.Ceiling(Math.Pow(vars.PlayerLevel, 1.35)); }
	// public double GetPlayerLevelDamage() { return 0; } //for testing -delete
	public double GetPlayerExtractDamage() { return ui.GetExtractStatValue("Damage"); }
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
	
	public int GetPlayerExtractPierce() { return ui.GetExtractStatValue("Pierce"); }
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
	
	public int GetPlayerExtractCritChance() { return ui.GetExtractStatValue("Crit Chance"); }
	public int GetPlayerEquippedWeaponCritChance() { return currentWeaponData.Crit; }
	
	public int GetPlayerFinalCritDamage() { return GetPlayerExtractCritDamage() + GetPlayerEquippedWeaponCritDamage(); }
	public int GetPlayerExtractCritDamage() { return ui.GetExtractStatValue("Crit Damage"); }
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
		// return ui.GetModStatValue("Knockback"); //DOESN'T YET EXIST
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
    
		// Get the total time reduction multiplier (e.g., 0.5 for 50% less time)
		var timeMultiplier = 1.0f - GetSkillReloadEffect(currentWeaponData.Id); 
	
		// Ensure multiplier is not negative (though it shouldn't be with your setup)
		if (timeMultiplier < 0) { timeMultiplier = 0; } 
    
		// --- FIX: Multiply the base time by the time multiplier ---
		var finalTime = baseReloadTime * timeMultiplier;
    
		// GD.Print($"Reload Base: {baseReloadTime} | Multiplier: {timeMultiplier} | Final: {finalTime}");
		return finalTime;
	}
	
	// public double GetPlayerModReload() { return ui.GetModStatValue("ReloadSpeed"); } //not used
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
		var final = GetPlayerEquippedWeaponBulletSize() * GetPlayerModBulletSize() * GetSkillBulletSize(currentWeaponData.Id);
		// final *= (int) skillMultiplier;
		return new Vector2(final, final);
	}

	public int GetPlayerModBulletSize() {
		return 1; //multiplier
		// return ui.GetModStatValue("BulletSize"); //DOESN'T YET EXIST
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
	public int GetPlayerModSpeed() { return ui.GetExtractStatValue("Speed"); }
	// public int GetPlayerFinalSpeed() { return GetPlayerLevelSpeed() + GetPlayerModSpeed() + GetTinctureSpeed(); }
	public int GetPlayerFinalSpeed() { return 3000; } //for testing -delete
	
	//player shield
	public int GetPlayerLevelShield() { return vars.PlayerLevel + 5; }
	public int GetPlayerModShield() { return ui.GetExtractStatValue("Shield"); }
	public int GetPlayerFinalShield() { return GetPlayerLevelShield() + GetPlayerModShield(); }
	
	//player shield regen
	public int GetPlayerLevelShieldRegen() { return 1; }
	public int GetPlayerModShieldRegen() { return ui.GetExtractStatValue("Shield Regen"); }
	public int GetPlayerFinalShieldRegen() { return GetPlayerLevelShieldRegen() + GetPlayerModShieldRegen(); }
	
	public float GetFinalWeaponSpeed() { return currentWeaponData.Speed; }
	public int GetFinalWeaponRange() { return currentWeaponData.Range; }
	
	//extract drop
	public float GetPlayerLevelExtractDropChance() { return vars.PlayerLevel * .01f; }
	public float GetAreaRankExtractDropChance() { return GetAreaRank(vars.CurrentArea) * .1f; }
	public float GetEquippedExtractsDropChance() { return ui.GetExtractStatValue("Extract Drop") * .1f; }
	public float GetPlayerFinalExtractDropChanceDisplay() { return (GetPlayerLevelExtractDropChance() + GetEquippedExtractsDropChance()); }
	public float GetPlayerFinalExtractDropChance(int enemyRank) { //probably not done
		// return 100; //for testing -delete
		var enemyRankChance = enemyRank * .01;
		var final = ((GetPlayerLevelExtractDropChance() + GetEquippedExtractsDropChance() + GetAreaRankExtractDropChance()) * enemyRankChance);
		return (float) final;
	}
	
	//sucrose drop
	public int GetPlayerLevelSucroseDrop() { return 0; }
	public int GetPlayerModSucroseDrop() { return ui.GetExtractStatValue("Sucrose Drop"); }
	// public int GetPlayerFinalSucroseDrop() { return GetPlayerLevelSucroseDrop() + GetPlayerModSucroseDrop(); } //prev
	public int GetPlayerFinalSucroseDrop() { return 100 + GetPlayerModSucroseDrop(); }
	
	//exp drop
	public int GetPlayerLevelExpDrop() { return 0; }
	public int GetPlayerModExpDrop() { return ui.GetExtractStatValue("Exp Drop"); }
	public int GetPlayerFinalExpDrop() { return 100 + GetPlayerModExpDrop(); }
	
	//player pickup range
	public int GetPlayerLevelPickupRange() { return (vars.PlayerLevel * 5) + 500; }
	public int GetPlayerModPickupRange() { return ui.GetExtractStatValue("Pickup Range"); }
	public int GetPlayerFinalPickupRange() { return (GetPlayerLevelPickupRange() + GetPlayerModPickupRange()); }

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
			1, "auto", 2, 1f, 120, 800, 8, 0, 1, 2, 5, 0, 1));
		
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
		ChangeSucrose(1000000);
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