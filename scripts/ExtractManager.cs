using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ConfectioneryTale.scripts;

public partial class ExtractManager : Node {
    [Export] private PackedScene itemDropScene;
    [Export] private Texture2D tierOneExtractTexture;
    [Export] private Texture2D tierTwoExtractTexture;
    [Export] private Texture2D tierThreeExtractTexture;
    [Export] private Texture2D tierFourExtractTexture;
    [Export] private Texture2D tierFiveExtractTexture;
    [Export] private Texture2D tierSixExtractTexture;

    private Variables vars;
    private Main main;
    private UI ui;
    private List<ExtractData> extractDataList;
    private List<(int tier, int weight)> dropWeights;
    private List<(int tier, int chance)> extractDropChances;
    private List<(string stat, int min, int max)> tierOneExtractStats;
    private List<(string stat, int min, int max)> tierTwoExtractStats;
    private List<(string stat, int min, int max)> tierThreeExtractStats;
    private List<(string stat, int min, int max)> tierFourExtractStats;
    private List<(string stat, int min, int max)> tierFiveExtractStats;
    private List<(string statName, int value)> tempItemStatsList;
    
    private List<ItemDrop> allCurrentDroppedExtracts = new List<ItemDrop>();
    
	public override void _Ready() {
		vars = GetNode<Variables>("/root/Variables");
		main = GetNode<Main>("/root/Main");
		ui = main.GetNode<UI>("UI");
		BuildExtractData();
		SetExtractDropWeights();
	}

	public void SpawnExtractDrop(Vector2 pos, int enemyRank) {
		var dropTier = GetExtractDropTier(enemyRank);
		if (dropTier == 0) { return; }
		
		BaseExtract newExtract = GenerateNewExtract(dropTier);
		var itemDropNode = itemDropScene.Instantiate<ItemDrop>();
		itemDropNode.SetPos(pos);
		itemDropNode.SetThisExtract(newExtract); //ItemDrop node holds this BaseExtract data
		allCurrentDroppedExtracts.Add(itemDropNode); //track the visual node (not directly saved by this call)
		main.GetWorld().CallDeferred("add_child", itemDropNode);
		itemDropNode.CallDeferred("SetExtractTexture");
	}

	public void SpawnMaterialDrop(Vector2 pos, BaseMaterial materialDrop) {
		if (materialDrop == null) { return; }
		// GD.Print(materialDrop.Id);
		var itemDropNode = itemDropScene.Instantiate<ItemDrop>();
		itemDropNode.SetPos(pos);
		allCurrentDroppedExtracts.Add(itemDropNode);
		main.GetWorld().CallDeferred("add_child", itemDropNode);
		itemDropNode.CallDeferred("SetMaterialTexture", materialDrop.DisplayTexture);
		itemDropNode.SetThisMaterial(materialDrop);
	}

	public void GainExtract(BaseExtract extractToGain) {
		// GD.Print($"Item gained: {extractToGain.Name ?? "Unnamed Item"}");
   
		//this calls AddItemToInventory with the BaseExtract DATA. AddItemToInventory then adds it to vars.savedResources.playerInventoryItems
		bool addedToInventory = ui.AddExtractToInventory(extractToGain); 
   
		if (addedToInventory) {
			main.ShowPopupInfo(Main.PopupEventType.ExtractGained, amount: 1, specificItemName: extractToGain.Name);
			//if successfully added to inventory, remove its visual node and data from world drops.
			RemoveItemDropNodeAndData(extractToGain.Id); //pass the ID of the BaseExtract
			vars.savedResources.inventoryExtracts.Add(extractToGain);
			vars.SaveGameData();
		} else {
			//will rarely be hit now because Player.CollectItem handles full inventory before calling GainItem
			GD.Print($"Inventory full for item {extractToGain.Name}.");
		}
	}
	
	private int GetExtractDropTier(int enemyRank) {
        float finalExtractDropChance = main.GetPlayerFinalExtractDropChance(enemyRank); // e.g., 0.1f, 0.2f, 1.5f

        // finalExtractDropChance = 100; //for testing -delete
        
		//generate a random float between 0.0f (inclusive) and 1.0f (exclusive)
        float randomNormalizedFloat = GD.Randf();

		//scale it to desired range (0.0f to 100.0f)
        float randomFloat100 = randomNormalizedFloat * 100.0f; //this generates a random float between 0.0 and 100.0

		//compare the random float (0-100) to finalExtractDropChance (which is also 0-100)
        if (randomFloat100 <= finalExtractDropChance) {
	        // GD.Print($"extract drop success. rolled {randomFloat100:0.00}% <= {finalExtractDropChance:0.00}%");
        } else { //extract does not drop
	        // GD.Print($"extract drop failed. rolled {randomFloat100:0.00}% > {finalExtractDropChance:0.00}%");
	        return 0;
        }

        // GD.Print("a extract might drop...");

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

	private void ResetExtractStatsList(int tier) {
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
	
	private void RollExtractValues(BaseExtract itemData) {
	    var tier = itemData.ExtractTier; //access ExtractTier directly from the BaseExtract data

	    ResetExtractStatsList(tier); //reset the list for the chosen tier

	    //get the correct list based on tier
	    List<(string stat, int min, int max)> targetExtractStatsList = null;
	    switch (tier) {
	        case 1: targetExtractStatsList = tierOneExtractStats; break;
	        case 2: targetExtractStatsList = tierTwoExtractStats; break;
	        case 3: targetExtractStatsList = tierThreeExtractStats; break;
	        case 4: targetExtractStatsList = tierFourExtractStats; break;
	        case 5: targetExtractStatsList = tierFiveExtractStats; break;
	        default: GD.PushWarning($"Invalid tier {tier} for extract value rolling. No stats rolled."); return;
	    }
	    
	    if (targetExtractStatsList == null || targetExtractStatsList.Count == 0) {
	        GD.PushWarning($"No extract stats available for tier {tier}. No stats rolled.");
	        return;
	    }

	    // important: make a copy of the list for rolling, so the original lists (tierOneExtractStats etc.) aren't permanently depleted across multiple item drops. if emptied, the next item won't have stats to roll
	    List<(string stat, int min, int max)> rollingList = new List<(string, int, int)>(targetExtractStatsList);

	    //apply 'tier' number of stats
	    for (int i = 0; i < tier; i++) {
	        if (rollingList.Count == 0) {
	            GD.PushWarning($"Not enough unique extract stats in rolling list for tier {tier} (applied {i} of {tier} stats).");
	            break; //break if list becomes empty before applying all stats
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
	        
	        itemData.SetExtractStatAndValue(newStat.stat, finalValue, calculatedQualityPercentage); //set stat on the BaseExtract data
	        rollingList.RemoveAt(randomIndex); //remove from the temporary rolling list
	    }
	}
	
	private void GenerateTempStatsList(BaseExtract item) {
		tempItemStatsList = new List<(string statName, int value)>();
		tempItemStatsList.Add(("Damage", item.ExtractBaseDamage));
		tempItemStatsList.Add(("Pierce", item.ExtractBasePierce));
		tempItemStatsList.Add(("Crit Chance", item.ExtractBaseCritChance));
		tempItemStatsList.Add(("Crit Damage", item.ExtractBaseCritDamage));
		tempItemStatsList.Add(("Shield", item.ExtractBaseShield));
		tempItemStatsList.Add(("Shield Regen", item.ExtractBaseShieldRegen));
		tempItemStatsList.Add(("Health", item.ExtractBaseHealth));
		tempItemStatsList.Add(("Pickup Range", item.ExtractBasePickupRange));
		tempItemStatsList.Add(("Speed", item.ExtractBaseSpeed));
		tempItemStatsList.Add(("Extract Drop", item.ExtractBaseExtractDrop));
		tempItemStatsList.Add(("Sucrose Drop", item.ExtractBaseSucroseDrop));
		tempItemStatsList.Add(("Exp Gain", item.ExtractBaseExpGain));
	}
	
	private void GenerateExtractName(BaseExtract item) {
		var data = GetExtractDataList();
        
		List<(string name, int value, int maxValue, float percent)> stats = new List<(string, int, int, float)>(); //don't delete
		
		foreach (var d in data) {
			if (item.ExtractTier != d.Tier) { continue; }
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

	private void SetExtractQuality(BaseExtract item) {
    if (item == null) {
        GD.PushWarning("SetItemQuality called with a null item.");
        return;
    }

    var allPossibleExtractDefinitions = GetExtractDataList(); // Get all possible extract definitions

    float totalActualRolledValue = 0;
    float totalMaxPotentialForPresentStats = 0;

    // GD.Print($"Calculating quality for item: ID={item.Id}, Tier={item.ExtractTier}");

    // Iterate through all *possible* stats to find matches for the item's tier
    foreach (var extractDefinition in allPossibleExtractDefinitions) {
        if (item.ExtractTier != extractDefinition.Tier) { continue; } //skip stats not of the item's tier

        // Check if the item actually has this stat rolled
        // (This relies on GetActualRolledStatValueFromItem returning > 0
        // or some other way to know if the stat is 'active' on the item)
        int actualRolledValueForThisStat = GetActualRolledStatValueFromItem(item, extractDefinition.Stat);

        //only include this stat in the quality calculation if it's present on the item
        if (actualRolledValueForThisStat > 0) { //stat is "active" on the item
            totalActualRolledValue += actualRolledValueForThisStat;
            totalMaxPotentialForPresentStats += extractDefinition.MaxRoll;
            // GD.Print($"Stat Present: {modDefinition.Stat}, Rolled: {actualRolledValueForThisStat}, Max: {modDefinition.MaxRoll}. Current Total Rolled: {totalActualRolledValue}, Current Total Max: {totalMaxPotentialForPresentStats}");
        }
        // else
        // {
        //     // GD.Print($"Stat Not Present or Zero: {modDefinition.Stat}. Not including in quality calculation.");
        // }
    }

    if (totalMaxPotentialForPresentStats == 0) {
        GD.Print($"No active stats found on item ID={item.Id} for Tier {item.ExtractTier} or their total max potential is zero. Setting quality to 0.");
        item.ExtractQuality = 0f;
        return;
    }

    item.ExtractQuality = (totalActualRolledValue / totalMaxPotentialForPresentStats) * 100f;
    // GD.Print($"Item ID={item.Id}, Tier={item.ExtractTier} - Total Rolled (Present Stats): {totalActualRolledValue}, Total Max (Present Stats): {totalMaxPotentialForPresentStats}, Quality: {item.ExtractQuality}%");
}

	//helper function to get the actual rolled value of a specific stat from the item
	private int GetActualRolledStatValueFromItem(BaseExtract item, string statName) {
	    switch (statName) { //must exactly match string in ExtractDataList
	        case "Damage": return item.ExtractBaseDamage;
	        case "Pierce": return item.ExtractBasePierce;
	        case "Crit Chance": return item.ExtractBaseCritChance;
	        case "Crit Damage": return item.ExtractBaseCritDamage;
	        case "Shield": return item.ExtractBaseShield;
	        case "Shield Regen": return item.ExtractBaseShieldRegen;
	        case "Health": return item.ExtractBaseHealth;
	        case "Pickup Range": return item.ExtractBasePickupRange;
	        case "Speed": return item.ExtractBaseSpeed;
	        case "Extract Drop": return item.ExtractBaseExtractDrop;
	        case "Sucrose Drop": return item.ExtractBaseSucroseDrop;
	        case "Exp Gain": return item.ExtractBaseExpGain;
	        default:
	            // GD.Print($"Warning: Unknown stat name '{statName}' in GetActualRolledStatValueFromItem for item ID {item.Id}. Assuming 0.");
	            return 0; //if the stat isn't recognized or isn't on the item, it contributes 0
	    }
	}
	
	public BaseExtract GenerateNewDistilleryExtract(int tier) {
		if (tier == 0) { return null; }
		BaseExtract newExtract = GenerateNewExtract(tier);
		return newExtract;
	}
	
	private BaseExtract GenerateNewExtract(int tier) {
		BaseExtract droppedExtractData = new BaseExtract();
		
		droppedExtractData.Id = Guid.NewGuid().ToString();
		// GD.Print($"Assigned Item ID: {droppedItemData.Id}");
		droppedExtractData.ExtractTier = tier;
		
		Texture2D itemDisplayTexture = GetTextureForTier(tier);
		droppedExtractData.DisplayTexture = itemDisplayTexture;
		
		RollExtractValues(droppedExtractData);
		GenerateTempStatsList(droppedExtractData);
		GenerateExtractName(droppedExtractData);
		SetExtractQuality(droppedExtractData);
		return droppedExtractData;
	}
	
	//this generates extract that is manually set in the world
	public BaseExtract GenerateFixedExtract(int tier, Godot.Collections.Dictionary<string, int> fixedStats) {
		GD.Print("generate");
		BaseExtract fixedExtractData = new BaseExtract();

		fixedExtractData.Id = Guid.NewGuid().ToString();
		fixedExtractData.ExtractTier = tier;
		fixedExtractData.DisplayTexture = GetTextureForTier(tier);

		//replace the random roll values
		var allExtractDefinitions = GetExtractDataList();

		foreach (var stat in fixedStats) {
			string statName = stat.Key;
			int statValue = stat.Value;

			//must find the MaxRoll for this stat to calculate its quality
			var modDef = allExtractDefinitions.FirstOrDefault(d => d.Tier == tier && d.Stat == statName);

			float calculatedQualityPercentage = 0f;
			if (modDef != null && modDef.MaxRoll > 0) {
				// Calculate quality just like in RollExtractValues
				calculatedQualityPercentage = ((float)statValue / modDef.MaxRoll) * 100f;
				calculatedQualityPercentage = (int)Math.Ceiling(calculatedQualityPercentage);
			}

			// Set the fixed stat and its calculated quality
			fixedExtractData.SetExtractStatAndValue(statName, statValue, calculatedQualityPercentage);
		}

		GenerateTempStatsList(fixedExtractData);
		GenerateExtractName(fixedExtractData);
		SetExtractQuality(fixedExtractData);

		return fixedExtractData;
	}
	
	private void RemoveItemDropNodeAndData(string itemId) {
		//remove the visual ItemDrop node from the scene
		ItemDrop nodeToRemove = null;
		foreach (var dropNode in allCurrentDroppedExtracts) {
			if (IsInstanceValid(dropNode) && dropNode.GetExtractData() != null && dropNode.GetExtractData().Id == itemId) {
				nodeToRemove = dropNode;
				break;
			}
		}

		if (nodeToRemove != null) {
			allCurrentDroppedExtracts.Remove(nodeToRemove); // Remove visual node from Main's tracking
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
	
	private Texture2D GetTextureForTier(int tier) {
		switch (tier) {
			case 1: return tierOneExtractTexture;
			case 2: return tierTwoExtractTexture;
			case 3: return tierThreeExtractTexture;
			case 4: return tierFourExtractTexture;
			case 5: return tierFiveExtractTexture;
			case 6: return tierSixExtractTexture;
			default: return null; //default/placeholder texture
		}
	}
	
	public List<ExtractData> GetExtractDataList() { return extractDataList; }
	
	private void SetExtractDropWeights() {
		extractDropChances = new List<(int, int)>() {
			(5, 5), //t5 .0001% 1/1000000 -too high, should be influenced by other stuff (enemy level, player stats)
			(4, 4), //t4 .001% 1/100000
			(3, 3), //t3 .01% 1/10000
			(2, 2), //t2 .1% 1/1000
			(1, 1), //t1 1% 1/100
		};
		
		// for (int i = 0; i < 50; i++) { GetExtractDropType(); } //simulate extract drop
	}
	
	private void BuildExtractData() {
		extractDataList = new List<ExtractData>(); //*stat names must match names in SetExtractStatAndValue
		
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