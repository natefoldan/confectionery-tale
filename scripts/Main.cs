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
	private Node2D world;
	private Camera2D playerCamera;
	private PackedScene playerScene;
	private PackedScene cutsceneManagerScene;
	private PackedScene currencySucroseScene;
	private PackedScene itemDropScene;
	private PackedScene popupTextScene;
	private WeaponData weaponData;
	private WeaponData currentWeaponData;
	private Texture2D tierOneExtractTexture;
	private Texture2D tierTwoExtractTexture;
	private Texture2D tierThreeExtractTexture;
	private Texture2D tierFourExtractTexture;
	private Texture2D tierFiveExtractTexture;
	private List<WeaponData> weaponDataList;
	private List<ExtractData> extractDataList;
	private List<ItemDrop> allCurrentDroppedItems = new List<ItemDrop>();
	private List<(int tier, int weight)> dropWeights;
	private List<(int tier, int chance)> extractDropChances;
	private List<(string stat, int min, int max)> tierOneExtractStats;
	private List<(string stat, int min, int max)> tierTwoExtractStats;
	private List<(string stat, int min, int max)> tierThreeExtractStats;
	private List<(string stat, int min, int max)> tierFourExtractStats;
	private List<(string stat, int min, int max)> tierFiveExtractStats;
	private List<(string statName, int value)> tempItemStatsList;
	
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
        
        world = GetNode<Node2D>("World");
        tileMapWorld = GetNode<TileMapLayer>("World/TileMapWorld");
        playerScene = GD.Load<PackedScene>("res://scenes/player.tscn");
        cutsceneManagerScene = GD.Load<PackedScene>("res://scenes/cutscene_manager.tscn");
        currencySucroseScene = GD.Load<PackedScene>("res://scenes/currency_sucrose.tscn");
        itemDropScene = GD.Load<PackedScene>("res://scenes/item_drop.tscn");
        popupTextScene = GD.Load<PackedScene>("res://scenes/popup_text.tscn");
        
        playerSpawnPortal = GetNode<Sprite2D>("World/PlayerSpawnPointOne/PlayerSpawnPortal");
        playerSpawnPortal.Visible = false;
        
	    tierOneExtractTexture = ResourceLoader.Load<Texture2D>("res://assets/sprites/extract-yellow.png");
	    tierTwoExtractTexture = ResourceLoader.Load<Texture2D>("res://assets/sprites/extract-green.png");
	    tierThreeExtractTexture = ResourceLoader.Load<Texture2D>("res://assets/sprites/extract-blue.png");
	    tierFourExtractTexture = ResourceLoader.Load<Texture2D>("res://assets/sprites/extract-purple.png");
	    tierFiveExtractTexture = ResourceLoader.Load<Texture2D>("res://assets/sprites/extract-orange.png");
	    // tierSixExtractTexture = ResourceLoader.Load<Texture2D>("res://assets/sprites/extract-pink.png");
        weaponData = GetNode<WeaponData>("/root/WeaponData");
        BuildWeaponData();
        BuildExtractData();
        SetExtractDropWeights();
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
	
	public void SpawnExtractDrop(Vector2 pos, int enemyRank) {
		var dropTier = GetModDropType(enemyRank);
		if (dropTier == 0) { return; }
		
		BaseExtract newExtract = GenerateNewExtract(dropTier);
		var itemDropNode = itemDropScene.Instantiate<ItemDrop>();
		itemDropNode.SetPos(pos);
		itemDropNode.SetThisExtract(newExtract); //ItemDrop node holds this BaseExtract data
		allCurrentDroppedItems.Add(itemDropNode); //track the visual node (not directly saved by this call)
		world.CallDeferred("add_child", itemDropNode);
		
		itemDropNode.CallDeferred("SetExtractTexture");
	}

	public void SpawnMaterialDrop(Vector2 pos, BaseMaterial materialDrop) {
		if (materialDrop == null) { return; }
		// GD.Print(materialDrop.Id);
		// BaseResource newResource = GenerateNewExtract(dropTier);
		var itemDropNode = itemDropScene.Instantiate<ItemDrop>();
		itemDropNode.SetPos(pos);
		// itemDropNode.SetThisItemResource(newExtract); //ItemDrop node holds this BaseExtract data
		
		allCurrentDroppedItems.Add(itemDropNode); //track the visual node (not directly saved by this call)
		world.CallDeferred("add_child", itemDropNode);
		
		itemDropNode.CallDeferred("SetMaterialTexture", materialDrop.DisplayTexture);
		
		itemDropNode.SetThisMaterial(materialDrop);
	}

	public BaseExtract GenerateNewDistilleryExtract(int tier) {
		if (tier == 0) { return null; }
		BaseExtract newExtract = GenerateNewExtract(tier);
		return newExtract;
	}
	
	private BaseExtract GenerateNewExtract(int tier) {
		BaseExtract droppedItemData = new BaseExtract();
		
		droppedItemData.Id = Guid.NewGuid().ToString();
		// GD.Print($"Assigned Item ID: {droppedItemData.Id}");
		droppedItemData.ModTier = tier;
		
		Texture2D itemDisplayTexture = GetTextureForTier(tier);
		droppedItemData.DisplayTexture = itemDisplayTexture;
		
		RollModValues(droppedItemData);
		GenerateTempStatsList(droppedItemData);
		GenerateItemName(droppedItemData);
		SetItemQuality(droppedItemData);
		return droppedItemData;
	}
	
	//this generates extract that is manually set in the world
	public BaseExtract GenerateFixedExtract(int tier, Godot.Collections.Dictionary<string, int> fixedStats)
	{
		GD.Print("generate");
		BaseExtract itemData = new BaseExtract();

		itemData.Id = Guid.NewGuid().ToString();
		itemData.ModTier = tier;
		itemData.DisplayTexture = GetTextureForTier(tier); // Your existing function

		// --- This block replaces your random RollModValues() ---
		var allModDefinitions = GetExtractDataList(); // Your existing function

		foreach (var stat in fixedStats)
		{
			string statName = stat.Key;
			int statValue = stat.Value;

			// We must find the MaxRoll for this stat to calculate its quality
			var modDef = allModDefinitions.FirstOrDefault(d => d.Tier == tier && d.Stat == statName);

			float calculatedQualityPercentage = 0f;
			if (modDef != null && modDef.MaxRoll > 0)
			{
				// Calculate quality just like in RollModValues
				calculatedQualityPercentage = ((float)statValue / modDef.MaxRoll) * 100f;
				calculatedQualityPercentage = (int)Math.Ceiling(calculatedQualityPercentage);
			}

			// Set the fixed stat and its calculated quality
			itemData.SetModStatAndValue(statName, statValue, calculatedQualityPercentage);
		}
		// --- End of new block ---

		GenerateTempStatsList(itemData); // Your existing function
		GenerateItemName(itemData);      // Your existing function
		SetItemQuality(itemData);        // Your existing function

		return itemData;
	}
	
	private Texture2D GetTextureForTier(int tier) {
		switch (tier) {
			case 1: return tierOneExtractTexture;
			case 2: return tierTwoExtractTexture;
			case 3: return tierThreeExtractTexture;
			case 4: return tierFourExtractTexture;
			case 5: return tierFiveExtractTexture;
			default: return null; //default/placeholder texture
		}
	}

	public void GainAssignment(string assignment) {
		assignments.GainAssignment(assignment);
	}
	
	public void GainExtract(BaseExtract extractToGain) {
		// GD.Print($"Item gained: {extractToGain.Name ?? "Unnamed Item"}");
   
		//this calls AddItemToInventory with the BaseExtract DATA. AddItemToInventory then adds it to vars.savedResources.playerInventoryItems
		bool addedToInventory = ui.AddExtractToInventory(extractToGain); 
   
		if (addedToInventory) {
			ShowPopupInfo(PopupEventType.ExtractGained, amount: 1, specificItemName: extractToGain.Name);
			//if successfully added to inventory, remove its visual node and data from world drops.
			RemoveItemDropNodeAndData(extractToGain.Id); // Pass the ID of the BaseExtract
			vars.savedResources.inventoryExtracts.Add(extractToGain);
			vars.SaveGameData();
		} else {
			//will rarely be hit now because Player.CollectItem handles full inventory before calling GainItem
			GD.Print($"Inventory full for item {extractToGain.Name}.");
		}
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
	
	private void RemoveItemDropNodeAndData(string itemId) {
		//remove the visual ItemDrop node from the scene
		ItemDrop nodeToRemove = null;
		foreach (var dropNode in allCurrentDroppedItems) {
			if (IsInstanceValid(dropNode) && dropNode.GetExtractData() != null && dropNode.GetExtractData().Id == itemId) {
				nodeToRemove = dropNode;
				break;
			}
		}

		if (nodeToRemove != null) {
			allCurrentDroppedItems.Remove(nodeToRemove); // Remove visual node from Main's tracking
			nodeToRemove.QueueFree(); // Free the visual node from the scene tree
		}

		// --- Remove the BaseExtract DATA from the saved world drops list ---
		// This is crucial to prevent picked-up items from reappearing as world drops on next load.
		// This part is for items that were *dropped in the world* and are now being picked up.
		BaseExtract dataToRemove = null;
		// Iterate through the saved list itself (vars.savedResources.droppedItems)
		foreach (var savedWorldDropData in vars.savedResources.inventoryExtracts) {
			if (savedWorldDropData.Id == itemId) { // Compare using the unique ID
				// GD.Print("comparing ids: " + savedWorldDropData.Id + " | " + itemId);
				dataToRemove = savedWorldDropData;
				break;
			}
		}

		if (dataToRemove != null) {
			vars.savedResources.inventoryExtracts.Remove(dataToRemove); //remove the data from the saved Array
			vars.SaveGameData();
		}
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
	
	//mods
	private void SetExtractDropWeights() {
		extractDropChances = new List<(int, int)>() {
			(5, 5), //t5 .0001% 1/1000000 -too high, should be influenced by other stuff (enemy level, player stats)
			(4, 4), //t4 .001% 1/100000
			(3, 3), //t3 .01% 1/10000
			(2, 2), //t2 .1% 1/1000
			(1, 1), //t1 1% 1/100
		};
		
		// for (int i = 0; i < 50; i++) { GetModDropType(); } //simulate mod drop
	}

	private int GetModDropType(int enemyRank) {
        float finalModDropChance = GetPlayerFinalExtractDropChance(enemyRank); // e.g., 0.1f, 0.2f, 1.5f

        // finalModDropChance = 100; //for testing -delete
        
		//generate a random float between 0.0f (inclusive) and 1.0f (exclusive)
        float randomNormalizedFloat = GD.Randf();

		//scale it to your desired range (0.0f to 100.0f)
        float randomFloat100 = randomNormalizedFloat * 100.0f; //this generates a random float between 0.0 and 100.0

		//compare the random float (0-100) to finalModDropChance (which is also 0-100)
        if (randomFloat100 <= finalModDropChance) {
	        // GD.Print($"MOD DROP SUCCESS! Rolled {randomFloat100:0.00}% <= {finalModDropChance:0.00}%");
        } else { //mod does not drop
	        // GD.Print($"MOD DROP FAILED! Rolled {randomFloat100:0.00}% > {finalModDropChance:0.00}%");
	        return 0;
        }

        // GD.Print("a mod might drop...");

        //determine which rarity drops. iterate from highest rarity (smallest chance number) to lowest
        //this ensures a rare drop takes precedence over a common one if conditions met
        int droppedTier = 0; //default to no drop if no condition met

        // Sort by tier descending, or iterate in a fixed order (e.g., highest tier first)
        // Ensure your list is ordered from highest tier to lowest, or sort it.
        // For example, if modDropChances is always ordered from lowest tier to highest (as you defined it),
        // you might need to iterate in reverse or sort first.
        // Let's assume you want to check higher rarities first, if multiple conditions are met.
        // So, iterate modDropChances in reverse or define it from highest to lowest tier.
        var sortedChances = extractDropChances.OrderByDescending(d => d.tier);

        foreach (var dropInfo in sortedChances) { //iterate from highest tier to lowest
            int randomNumber = GD.RandRange(1, dropInfo.chance); //roll a number between 1 and the chance value
            if (randomNumber == 1) { //if the random number is exactly 1, the condition is met
                droppedTier = dropInfo.tier;
                break; //found the rarity, stop checking
            }
        }

        // if (droppedTier > 0) {
        //     GD.Print($"Dropped item of Tier: {droppedTier}");
        // } else {
        //     GD.Print("No specific rarity condition met for item drop (but overall chance passed).");
        // }

        // droppedTier = 1; //for testing -delete
        droppedTier = GD.RandRange(1, 5);
        return droppedTier;
    }

	private void ResetModStatsList(int tier) {
		switch (tier) {
			case 1: tierOneExtractStats = new List<(string, int, int)>() { }; break;
			case 2: tierTwoExtractStats = new List<(string, int, int)>() { }; break;
			case 3: tierThreeExtractStats = new List<(string, int, int)>() { }; break;
			case 4: tierFourExtractStats = new List<(string, int, int)>() { }; break;
			case 5: tierFiveExtractStats = new List<(string, int, int)>() { }; break;
		}
		
		foreach (var extractData in extractDataList) {
			if (extractData.Tier != tier) { continue; }
			switch (tier) {
				case 1: tierOneExtractStats.Add((extractData.Stat, extractData.MinRoll, extractData.MaxRoll)); break;
				case 2: tierTwoExtractStats.Add((extractData.Stat, extractData.MinRoll, extractData.MaxRoll)); break;
				case 3: tierThreeExtractStats.Add((extractData.Stat, extractData.MinRoll, extractData.MaxRoll)); break;
				case 4: tierFourExtractStats.Add((extractData.Stat, extractData.MinRoll, extractData.MaxRoll)); break;
				case 5: tierFiveExtractStats.Add((extractData.Stat, extractData.MinRoll, extractData.MaxRoll)); break;
			}
		}
	}
	
	private void RollModValues(BaseExtract itemData) {
	    var tier = itemData.ModTier; //access ModTier directly from the BaseExtract data

	    ResetModStatsList(tier); //reset the list for the chosen tier

	    //get the correct list based on tier
	    List<(string stat, int min, int max)> targetModStatsList = null;
	    switch (tier) {
	        case 1: targetModStatsList = tierOneExtractStats; break;
	        case 2: targetModStatsList = tierTwoExtractStats; break;
	        case 3: targetModStatsList = tierThreeExtractStats; break;
	        case 4: targetModStatsList = tierFourExtractStats; break;
	        case 5: targetModStatsList = tierFiveExtractStats; break;
	        default: GD.PushWarning($"Invalid tier {tier} for mod value rolling. No mods rolled."); return;
	    }
	    
	    if (targetModStatsList == null || targetModStatsList.Count == 0) {
	        GD.PushWarning($"No mod stats available for tier {tier}. No mods rolled.");
	        return;
	    }

	    // IMPORTANT: Make a COPY of the list for rolling, so the original lists (tierOneModStats etc.)
	    // aren't permanently depleted across multiple item drops. if emptied, the next item won't have stats to roll
	    List<(string stat, int min, int max)> rollingList = new List<(string, int, int)>(targetModStatsList);

	    //initialize qualities to 0
	    // itemData.ModBaseDamageQuality = 0f;
	    // itemData.ModBasePierceQuality = 0f;
	    // itemData.ModBaseCritChanceQuality = 0f;
	    // itemData.ModBaseCritDamageQuality = 0f;
	    // itemData.ModBaseShieldQuality = 0f;
	    // itemData.ModBaseShieldRegenQuality = 0f;
	    // itemData.ModBaseHealthQuality = 0f;
	    // itemData.ModBasePickupRangeQuality = 0f;
	    // itemData.ModBaseSpeedQuality = 0f;
	    // itemData.ModBaseExtractDropQuality = 0f;
	    // itemData.ModBaseSucroseDropQuality = 0f;
	    // itemData.ModBaseExpGainQuality = 0f;
	    
	    //apply 'tier' number of mods
	    for (int i = 0; i < tier; i++) {
	        if (rollingList.Count == 0) {
	            GD.PushWarning($"Not enough unique mod stats in rolling list for tier {tier} (applied {i} of {tier} mods).");
	            break; //break if list becomes empty before applying all mods
	        }

	        var randomIndex = GD.RandRange(0, rollingList.Count - 1);
	        var newStat = rollingList[randomIndex];
	        var finalValue = GD.RandRange(newStat.min, newStat.max);
	        
	        //calculate the quality percentage for THIS specific stat
	        float calculatedQualityPercentage = 0f;
	        if (newStat.max > 0) { //avoid division by zero if max is 0
		        calculatedQualityPercentage = ((float)finalValue / newStat.max) * 100f;
		        calculatedQualityPercentage = (int)Math.Ceiling(calculatedQualityPercentage);
	        }
	        
	        itemData.SetModStatAndValue(newStat.stat, finalValue, calculatedQualityPercentage); //set stat on the BaseExtract data
	        rollingList.RemoveAt(randomIndex); //remove from the temporary rolling list
	    }
	}
	
	private void GenerateTempStatsList(BaseExtract item) {
		tempItemStatsList = new List<(string statName, int value)>();
		tempItemStatsList.Add(("Damage", item.ModBaseDamage));
		tempItemStatsList.Add(("Pierce", item.ModBasePierce));
		tempItemStatsList.Add(("Crit Chance", item.ModBaseCritChance));
		tempItemStatsList.Add(("Crit Damage", item.ModBaseCritDamage));
		tempItemStatsList.Add(("Shield", item.ModBaseShield));
		tempItemStatsList.Add(("Shield Regen", item.ModBaseShieldRegen));
		tempItemStatsList.Add(("Health", item.ModBaseHealth));
		tempItemStatsList.Add(("Pickup Range", item.ModBasePickupRange));
		tempItemStatsList.Add(("Speed", item.ModBaseSpeed));
		tempItemStatsList.Add(("Extract Drop", item.ModBaseExtractDrop));
		tempItemStatsList.Add(("Sucrose Drop", item.ModBaseSucroseDrop));
		tempItemStatsList.Add(("Exp Gain", item.ModBaseExpGain));
	}
	
	private void GenerateItemName(BaseExtract item) {
		var data = GetExtractDataList();
        
		List<(string name, int value, int maxValue, float percent)> stats = new List<(string, int, int, float)>(); //don't delete
		
		foreach (var d in data) {
			if (item.ModTier != d.Tier) { continue; }
			foreach (var s in tempItemStatsList) {
				// GD.Print(s.statName + " | " + d.Stat);
				if (s.statName != d.Stat) { continue; }
				// GD.Print("found matching stat");
				var percent = ((float)s.value / (float)d.MaxRoll) * 100;
				stats.Add((s.statName, s.value, d.MaxRoll, percent));
				//divide value / maxroll to get percent
			}
		}

		var bestStat = "";
		var currentHighest = 0f;
		foreach (var s in stats) {
			// GD.Print(s.name + ": " + s.value + "\nmax: " + s.maxValue + "\npercent: " + s.percent);
			// GD.Print(s.name + " max: " + s.maxValue + " | " + s.percent + "%");
			if(s.percent < currentHighest) { continue; }
			bestStat = s.name;
			currentHighest = s.percent;
		}
        
		// GD.Print("highest = " + bestStat + ": " +  currentHighest);
        
		item.Name = bestStat;
	}

	private void SetItemQuality(BaseExtract item) {
    if (item == null) {
        GD.PushWarning("SetItemQuality called with a null item.");
        return;
    }

    var allPossibleModDefinitions = GetExtractDataList(); // Get all possible mod definitions

    float totalActualRolledValue = 0;
    float totalMaxPotentialForPresentStats = 0;

    // GD.Print($"Calculating quality for item: ID={item.Id}, Tier={item.ModTier}");

    // Iterate through all *possible* mods to find matches for the item's tier
    foreach (var modDefinition in allPossibleModDefinitions) {
        if (item.ModTier != modDefinition.Tier) { continue; } //skip mods not of the item's tier

        // Check if the item actually has this stat rolled
        // (This relies on GetActualRolledStatValueFromItem returning > 0
        // or some other way to know if the stat is 'active' on the item)
        int actualRolledValueForThisStat = GetActualRolledStatValueFromItem(item, modDefinition.Stat);

        //only include this stat in the quality calculation if it's present on the item
        if (actualRolledValueForThisStat > 0) { //stat is "active" on the item
            totalActualRolledValue += actualRolledValueForThisStat;
            totalMaxPotentialForPresentStats += modDefinition.MaxRoll;
            // GD.Print($"Stat Present: {modDefinition.Stat}, Rolled: {actualRolledValueForThisStat}, Max: {modDefinition.MaxRoll}. Current Total Rolled: {totalActualRolledValue}, Current Total Max: {totalMaxPotentialForPresentStats}");
        }
        // else
        // {
        //     // GD.Print($"Stat Not Present or Zero: {modDefinition.Stat}. Not including in quality calculation.");
        // }
    }

    if (totalMaxPotentialForPresentStats == 0) {
        GD.Print($"No active stats found on item ID={item.Id} for Tier {item.ModTier} or their total max potential is zero. Setting quality to 0.");
        item.ModQuality = 0f;
        return;
    }

    item.ModQuality = (totalActualRolledValue / totalMaxPotentialForPresentStats) * 100f;
    // GD.Print($"Item ID={item.Id}, Tier={item.ModTier} - Total Rolled (Present Stats): {totalActualRolledValue}, Total Max (Present Stats): {totalMaxPotentialForPresentStats}, Quality: {item.ModQuality}%");
}

	//helper function to get the actual rolled value of a specific stat from the item
	private int GetActualRolledStatValueFromItem(BaseExtract item, string statName) {
	    switch (statName) { //must exactly match string in ExtractDataList
	        case "Damage": return item.ModBaseDamage;
	        case "Pierce": return item.ModBasePierce;
	        case "Crit Chance": return item.ModBaseCritChance;
	        case "Crit Damage": return item.ModBaseCritDamage;
	        case "Shield": return item.ModBaseShield;
	        case "Shield Regen": return item.ModBaseShieldRegen;
	        case "Health": return item.ModBaseHealth;
	        case "Pickup Range": return item.ModBasePickupRange;
	        case "Speed": return item.ModBaseSpeed;
	        case "Extract Drop": return item.ModBaseExtractDrop;
	        case "Sucrose Drop": return item.ModBaseSucroseDrop;
	        case "Exp Gain": return item.ModBaseExpGain;
	        default:
	            // GD.Print($"Warning: Unknown stat name '{statName}' in GetActualRolledStatValueFromItem for item ID {item.Id}. Assuming 0.");
	            return 0; //if the stat isn't recognized or isn't on the item, it contributes 0
	    }
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
	
	private void ShowPopupInfo(PopupEventType eventType, string subjectName = null, int amount = 0, string specificItemName = null) {
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
	public int GetPlayerModHealth() { return ui.GetModStatValue("Health"); }
	public int GetPlayerMaxHealth() { return GetPlayerLevelHealth() + GetPlayerModHealth(); }

	//player damage
	public double GetPlayerLevelDamage() { return Math.Ceiling(Math.Pow(vars.PlayerLevel, 1.35)); }
	// public double GetPlayerLevelDamage() { return 0; } //for testing -delete
	public double GetPlayerModDamage() { return ui.GetModStatValue("Damage"); }
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
		var final = GetPlayerLevelDamage() + GetPlayerModDamage() + GetPlayerEquippedWeaponDamage();
		final *= GetSkillDamageEffect(currentWeaponData.Id);
		return Math.Ceiling(final);
	}
	//player damage
	
	//pierce
	public int GetPlayerFinalPierce() {
		var final = GetPlayerModPierce() + GetPlayerEquippedWeaponPierce();
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
	
	public int GetPlayerModPierce() { return ui.GetModStatValue("Pierce"); }
	public int GetPlayerEquippedWeaponPierce() { return currentWeaponData.Pierce; }
	//pierce
	
	//crit
	public int GetPlayerFinalCritChance() {
		var final = GetPlayerModCritChance() + GetPlayerEquippedWeaponCritChance();
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
	
	public int GetPlayerModCritChance() { return ui.GetModStatValue("Crit Chance"); }
	public int GetPlayerEquippedWeaponCritChance() { return currentWeaponData.Crit; }
	
	public int GetPlayerFinalCritDamage() { return GetPlayerModCritDamage() + GetPlayerEquippedWeaponCritDamage(); }
	public int GetPlayerModCritDamage() { return ui.GetModStatValue("Crit Damage"); }
	public int GetPlayerEquippedWeaponCritDamage() { return currentWeaponData.CritDamage; }
	//crit
	
	//knockback
	public int GetPlayerFinalKnockback() {
		var final = GetPlayerModKnockback() + GetPlayerEquippedWeaponKnockback();
		final += GetSkillKnockbackEffect(currentWeaponData.Id);
		return (int) Math.Ceiling(final);
	}
	
	public double GetPlayerModKnockback() {
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
	public int GetPlayerModSpeed() { return ui.GetModStatValue("Speed"); }
	// public int GetPlayerFinalSpeed() { return GetPlayerLevelSpeed() + GetPlayerModSpeed() + GetTinctureSpeed(); }
	public int GetPlayerFinalSpeed() { return 3000; } //for testing -delete
	
	//player shield
	public int GetPlayerLevelShield() { return vars.PlayerLevel + 5; }
	public int GetPlayerModShield() { return ui.GetModStatValue("Shield"); }
	public int GetPlayerFinalShield() { return GetPlayerLevelShield() + GetPlayerModShield(); }
	
	//player shield regen
	public int GetPlayerLevelShieldRegen() { return 1; }
	public int GetPlayerModShieldRegen() { return ui.GetModStatValue("Shield Regen"); }
	public int GetPlayerFinalShieldRegen() { return GetPlayerLevelShieldRegen() + GetPlayerModShieldRegen(); }
	
	public float GetFinalWeaponSpeed() { return currentWeaponData.Speed; }
	public int GetFinalWeaponRange() { return currentWeaponData.Range; }
	
	//extract drop
	public float GetPlayerLevelExtractDropChance() { return vars.PlayerLevel * .01f; }
	public float GetAreaRankExtractDropChance() { return GetAreaRank(vars.CurrentArea) * .1f; }
	public float GetEquippedExtractsDropChance() { return ui.GetModStatValue("Extract Drop") * .1f; }
	public float GetPlayerFinalExtractDropChanceDisplay() { return (GetPlayerLevelExtractDropChance() + GetEquippedExtractsDropChance()); }
	public float GetPlayerFinalExtractDropChance(int enemyRank) { //probably not done
		// return 100; //for testing -delete
		var enemyRankChance = enemyRank * .01;
		var final = ((GetPlayerLevelExtractDropChance() + GetEquippedExtractsDropChance() + GetAreaRankExtractDropChance()) * enemyRankChance);
		return (float) final;
	}
	
	//sucrose drop
	public int GetPlayerLevelSucroseDrop() { return 0; }
	public int GetPlayerModSucroseDrop() { return ui.GetModStatValue("Sucrose Drop"); }
	// public int GetPlayerFinalSucroseDrop() { return GetPlayerLevelSucroseDrop() + GetPlayerModSucroseDrop(); } //prev
	public int GetPlayerFinalSucroseDrop() { return 100 + GetPlayerModSucroseDrop(); }
	
	//exp drop
	public int GetPlayerLevelExpDrop() { return 0; }
	public int GetPlayerModExpDrop() { return ui.GetModStatValue("Exp Drop"); }
	public int GetPlayerFinalExpDrop() { return 100 + GetPlayerModExpDrop(); }
	
	//player pickup range
	public int GetPlayerLevelPickupRange() { return (vars.PlayerLevel * 5) + 500; }
	public int GetPlayerModPickupRange() { return ui.GetModStatValue("Pickup Range"); }
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
	
	public BaseExtract GetItemById(string id) {
		// foreach (var item in allInventoryItems) { if (item.Id.Equals(id)) { return item; } }
		return null;
	}

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

	public List<ExtractData> GetExtractDataList() { return extractDataList; }
	
	private void BuildExtractData() {
		extractDataList = new List<ExtractData>(); //*stat names must match names in SetModStatAndValue
		
		// extractDataList.Add(new ExtractData("Crit Chance", 1, 1, 2)); //no t1 critchance
		// extractDataList.Add(new ExtractData("Crit Chance", 2, 1, 3)); //no t2 critchance
		extractDataList.Add(new ExtractData("Crit Chance", 3, 1, 2));
		extractDataList.Add(new ExtractData("Crit Chance", 4, 2, 3));
		extractDataList.Add(new ExtractData("Crit Chance", 5, 3, 4));
		
		// extractDataList.Add(new ExtractData("CritDamage", 1, 0, 0)); //no t1 critdamage
		extractDataList.Add(new ExtractData("Crit Damage", 2, 5, 10));
		extractDataList.Add(new ExtractData("Crit Damage", 3, 10, 20));
		extractDataList.Add(new ExtractData("Crit Damage", 4, 20, 40));
		extractDataList.Add(new ExtractData("Crit Damage", 5, 40, 50));
		
		extractDataList.Add(new ExtractData("Damage", 1, 1, 5));
		extractDataList.Add(new ExtractData("Damage", 2, 5, 10));
		extractDataList.Add(new ExtractData("Damage", 3, 10, 20));
		extractDataList.Add(new ExtractData("Damage", 4, 20, 30));
		extractDataList.Add(new ExtractData("Damage", 5, 30, 50));
		
		extractDataList.Add(new ExtractData("Exp Gain", 1, 1, 5));
		extractDataList.Add(new ExtractData("Exp Gain", 2, 5, 10));
		extractDataList.Add(new ExtractData("Exp Gain", 3, 10, 20));
		extractDataList.Add(new ExtractData("Exp Gain", 4, 20, 30));
		extractDataList.Add(new ExtractData("Exp Gain", 5, 30, 50));
		
		// extractDataList.Add(new ExtractData("Extract Drop", 1, 0, 0)); //no t1 extract
		extractDataList.Add(new ExtractData("Extract Drop", 2, 1, 2));
		extractDataList.Add(new ExtractData("Extract Drop", 3, 2, 3));
		extractDataList.Add(new ExtractData("Extract Drop", 4, 3, 4));
		extractDataList.Add(new ExtractData("Extract Drop", 5, 4, 5));
	
		extractDataList.Add(new ExtractData("Health", 1, 1, 5));
		extractDataList.Add(new ExtractData("Health", 2, 5, 10));
		extractDataList.Add(new ExtractData("Health", 3, 10, 20));
		extractDataList.Add(new ExtractData("Health", 4, 20, 30));
		extractDataList.Add(new ExtractData("Health", 5, 30, 50));
		
		extractDataList.Add(new ExtractData("Pickup Range", 1, 1, 5));
		extractDataList.Add(new ExtractData("Pickup Range", 2, 5, 10));
		extractDataList.Add(new ExtractData("Pickup Range", 3, 10, 20));
		extractDataList.Add(new ExtractData("Pickup Range", 4, 20, 30));
		extractDataList.Add(new ExtractData("Pickup Range", 5, 30, 50));
		
		// extractDataList.Add(new ExtractData("Pierce", 1, 0, 0)); //no t1 pierce
		extractDataList.Add(new ExtractData("Pierce", 2, 1, 1));
		extractDataList.Add(new ExtractData("Pierce", 3, 1, 2));
		extractDataList.Add(new ExtractData("Pierce", 4, 2, 3));
		extractDataList.Add(new ExtractData("Pierce", 5, 2, 4));
		
		extractDataList.Add(new ExtractData("Shield", 1, 1, 2));
		extractDataList.Add(new ExtractData("Shield", 2, 1, 3));
		extractDataList.Add(new ExtractData("Shield", 3, 2, 4));
		extractDataList.Add(new ExtractData("Shield", 4, 4, 5));
		extractDataList.Add(new ExtractData("Shield", 5, 5, 10));
		
		extractDataList.Add(new ExtractData("Speed", 1, 1, 5));
		extractDataList.Add(new ExtractData("Speed", 2, 1, 10));
		extractDataList.Add(new ExtractData("Speed", 3, 5, 20));
		extractDataList.Add(new ExtractData("Speed", 4, 5, 30));
		extractDataList.Add(new ExtractData("Speed", 5, 10, 40));
		
		extractDataList.Add(new ExtractData("Sucrose Drop", 1, 1, 5));
		extractDataList.Add(new ExtractData("Sucrose Drop", 2, 5, 10));
		extractDataList.Add(new ExtractData("Sucrose Drop", 3, 10, 20));
		extractDataList.Add(new ExtractData("Sucrose Drop", 4, 20, 30));
		extractDataList.Add(new ExtractData("Sucrose Drop", 5, 30, 50));
		
		// extractDataList.Add(new ExtractData("ShieldRegen", 1, 0, 0)); //no shield regen
	}
	
	
	public class ExtractData {
		public string Stat;
		public int Tier;
		public int MinRoll;
		public int MaxRoll;
    
		public ExtractData() { }

		public ExtractData(string stat, int tier, int minRoll, int maxRoll) {
			Tier = tier;
			Stat = stat;
			MinRoll =  minRoll;
			MaxRoll =  maxRoll;
		}
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