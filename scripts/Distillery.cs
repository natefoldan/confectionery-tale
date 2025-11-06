using System.Collections.Generic;
using Godot;

namespace ConfectioneryTale.scripts;

public partial class Distillery : Control {
	private UI ui;
	private Main main;
	private Variables vars;
	private ExtractManager extractManager;
	private TooltipHandler tooltips;
	private Label currentSucroseLabel;
	private Node2D inventoryContainer;
	private Control refinerPanel;
	private Control condenserPanel;
	private Texture2D swapRefinerButtonTexture;
	private Texture2D swapCondenserButtonTexture;
	private TextureButton swapButton;
	private Label swapRefinerLabel;
	private Label swapCondenserLabel;
	private Label swapLabelLeft;
	private Label swapLabelRight;
	private TextureRect refinerAddArrow;
	
	//condenser - turns sucrose into 1 extract
	private DistillerySlot condenserOutputSlot;
	private HSlider condenserSlider;
	private int[,] condenserData;
	private TextureButton condenserButton;
	private TextureRect condenserTierOne;
	private TextureRect condenserTierTwo;
	private TextureRect condenserTierThree;
	private TextureRect condenserTierFour;
	private TextureRect condenserTierFive;
	private TextureButton chancePanel;
	private HBoxContainer chancePanelContainer;
	private Label condenserOutputLabel;
	private Label condenserCostLabel;
	private Label condenserTierOneChanceLabel;
	private Label condenserTierTwoChanceLabel;
	private Label condenserTierThreeChanceLabel;
	private Label condenserTierFourChanceLabel;
	private Label condenserTierFiveChanceLabel;
	private Label collectCondenseExtractLabel;
	private TextureRect condenserBuildScreen;
	private TextureButton condenserBuildButton;
	
	//refiner - turns 3 extracts into 1 higher tier
	private PackedScene distillerySlot;
	private DistillerySlot refinerOutputSlot;
	private List<DistillerySlot> allRefinerSlots;
	private TextureButton refinerOneButton;
	private TextureButton refinerTwoButton;
	private TextureButton refinerThreeButton;
	private TextureButton refinerFourButton;
	private TextureButton refinerFiveButton;
	private TextureRect refinerBuildScreen;
	private TextureButton refinerBuildButton;
	
	public override void _Ready() {
		distillerySlot = GD.Load<PackedScene>("res://scenes/distillery_slot.tscn");
		vars = GetNode<Variables>("/root/Variables");
		tooltips = GetNode<TooltipHandler>("/root/TooltipHandler");
		ui = GetNode<UI>("/root/Main/UI");
		main = GetNode<Main>("/root/Main");
		extractManager = GetNode<ExtractManager>("/root/ExtractManager");
		main.SucroseChanged += HandleSucroseChanged;
		SetupRightPanel();
		SetupRefiner();
		SetupCondenser();
	}

	private void SetupRightPanel() {
		swapRefinerButtonTexture = GD.Load<Texture2D>("res://assets/btn-blue-swap-right.png");
		swapCondenserButtonTexture = GD.Load<Texture2D>("res://assets/btn-blue-swap-left.png");
		swapButton = GetNode<TextureButton>("RightPanel/SwapButton");
		swapRefinerLabel = GetNode<Label>("RightPanel/SwapButton/CondenserLabel");
		swapCondenserLabel = GetNode<Label>("RightPanel/SwapButton/RefinerLabel");
		swapLabelLeft = GetNode<Label>("RightPanel/SwapButton/SwapLabelRight");
		swapLabelRight = GetNode<Label>("RightPanel/SwapButton/SwapLabelLeft");
		currentSucroseLabel = GetNode<Label>("CurrentSucrose/Label");
		inventoryContainer = GetNode<Node2D>("RightPanel/InventoryContainer");
		refinerAddArrow = GetNode<TextureRect>("RightPanel/AddArrow");
	}
	
	private void SetupRefiner() {
		refinerPanel = GetNode<Control>("Refiner");
		refinerOneButton = GetNode<TextureButton>("Refiner/VBoxContainer/TierOne/Button");
		refinerTwoButton = GetNode<TextureButton>("Refiner/VBoxContainer/TierTwo/Button");
		refinerThreeButton = GetNode<TextureButton>("Refiner/VBoxContainer/TierThree/Button");
		refinerFourButton = GetNode<TextureButton>("Refiner/VBoxContainer/TierFour/Button");
		refinerFiveButton = GetNode<TextureButton>("Refiner/VBoxContainer/TierFive/Button");
		refinerBuildScreen = GetNode<TextureRect>("Refiner/Build");
		refinerBuildButton = GetNode<TextureButton>("Refiner/Build/TextureButton");
		BuildRefinerSlots();
	}
	
	private void SetupCondenser() {
		condenserPanel = GetNode<Control>("Condenser");
		condenserOutputSlot = GetNode<DistillerySlot>("Condenser/Output");
		condenserSlider = GetNode<HSlider>("Condenser/HSlider");
		condenserCostLabel = GetNode<Label>("Condenser/Button/Label");
		condenserButton = GetNode<TextureButton>("Condenser/Button");
		condenserTierOne = GetNode<TextureRect>("Condenser/ChancePanel/HBoxContainer/TierOne");
		condenserTierTwo = GetNode<TextureRect>("Condenser/ChancePanel/HBoxContainer/TierTwo");
		condenserTierThree = GetNode<TextureRect>("Condenser/ChancePanel/HBoxContainer/TierThree");
		condenserTierFour = GetNode<TextureRect>("Condenser/ChancePanel/HBoxContainer/TierFour");
		condenserTierFive = GetNode<TextureRect>("Condenser/ChancePanel/HBoxContainer/TierFive");
		condenserTierOneChanceLabel = GetNode<Label>("Condenser/ChancePanel/HBoxContainer/TierOne/ChanceLabel");
		condenserTierTwoChanceLabel = GetNode<Label>("Condenser/ChancePanel/HBoxContainer/TierTwo/ChanceLabel");
		condenserTierThreeChanceLabel = GetNode<Label>("Condenser/ChancePanel/HBoxContainer/TierThree/ChanceLabel");
		condenserTierFourChanceLabel = GetNode<Label>("Condenser/ChancePanel/HBoxContainer/TierFour/ChanceLabel");
		condenserTierFiveChanceLabel = GetNode<Label>("Condenser/ChancePanel/HBoxContainer/TierFive/ChanceLabel");
		collectCondenseExtractLabel = GetNode<Label>("Condenser/CollectLabel");
		condenserBuildScreen = GetNode<TextureRect>("Condenser/Build");
		condenserBuildButton = GetNode<TextureButton>("Condenser/Build/TextureButton");
		chancePanel = GetNode<TextureButton>("Condenser/ChancePanel");
		chancePanelContainer = GetNode<HBoxContainer>("Condenser/ChancePanel/HBoxContainer");
		condenserOutputLabel = GetNode<Label>("Condenser/ChancePanel/Label");
		
		condenserOutputSlot.DistilleryExtractClicked += RemoveExtractFromDistillery;
		collectCondenseExtractLabel.Visible = false;
		condenserOutputSlot.Reset();
		BuildCondenserData();
	}
	
	private void HandleSucroseChanged() {
		UpdateDistillery();
		CheckCondenserBuild();
		CheckRefinerBuild();
	}

	private void UpdateDistillery() {
		if (!Visible) { return; }
		CheckRefinerButtons();
		CheckCondenseButton();
		currentSucroseLabel.Text = ui.TrimNumber(vars.CurrentSucrose);
	}
	
	private void BuildRefinerSlots() {
		allRefinerSlots = new List<DistillerySlot>();
		var inputContainerOne = GetNode<HBoxContainer>("Refiner/VBoxContainer/TierOne/Input/InputSlots");
		var inputContainerTwo = GetNode<HBoxContainer>("Refiner/VBoxContainer/TierTwo/Input/InputSlots");
		var inputContainerThree = GetNode<HBoxContainer>("Refiner/VBoxContainer/TierThree/Input/InputSlots");
		var inputContainerFour = GetNode<HBoxContainer>("Refiner/VBoxContainer/TierFour/Input/InputSlots");
		var inputContainerFive = GetNode<HBoxContainer>("Refiner/VBoxContainer/TierFive/Input/InputSlots");
		
		var outputContainerOne = GetNode<Control>("Refiner/VBoxContainer/TierOne/OutputSlot");
		var outputContainerTwo = GetNode<Control>("Refiner/VBoxContainer/TierTwo/OutputSlot");
		var outputContainerThree = GetNode<Control>("Refiner/VBoxContainer/TierThree/OutputSlot");
		var outputContainerFour = GetNode<Control>("Refiner/VBoxContainer/TierFour/OutputSlot");
		var outputContainerFive = GetNode<Control>("Refiner/VBoxContainer/TierFive/OutputSlot");
		
		for (int i = 1; i < 16; i++) { //build input slots
			DistillerySlot slot = distillerySlot.Instantiate<DistillerySlot>();
			slot.DistilleryExtractClicked += RemoveExtractFromDistillery;

			var slotTier = ((i - 1) / 3) + 1;
			slot.slotTier = slotTier;

			if (slotTier == 1) { inputContainerOne.AddChild(slot); }
			else if (slotTier == 2) { inputContainerTwo.AddChild(slot); }
			else if (slotTier == 3) { inputContainerThree.AddChild(slot); }
			else if (slotTier == 4) { inputContainerFour.AddChild(slot); }
			else if (slotTier == 5) { inputContainerFive.AddChild(slot); }
			allRefinerSlots.Add(slot);
		}
		
		for (int i = 1; i < 6; i++) { //build output slots
			DistillerySlot slot = distillerySlot.Instantiate<DistillerySlot>();
			slot.DistilleryExtractClicked += RemoveExtractFromDistillery;
			slot.slotTier = i;
			slot.outputSlot = true;

			switch (i) {
				case 1: outputContainerOne.AddChild(slot);  break;
				case 2: outputContainerTwo.AddChild(slot); break;
				case 3: outputContainerThree.AddChild(slot); break;
				case 4: outputContainerFour.AddChild(slot); break;
				case 5: outputContainerFive.AddChild(slot); break;
			}
			allRefinerSlots.Add(slot);
		}
		foreach (var s in allRefinerSlots) { s.Reset(); }
	}

	private void CheckRefinerButtons() {
		refinerOneButton.Disabled = true;
		refinerTwoButton.Disabled = true;
		refinerThreeButton.Disabled = true;
		refinerFourButton.Disabled = true;
		refinerFiveButton.Disabled = true;
		
		if (vars.CurrentSucrose >= GetRefineCost(1) && CheckReadyForCombine(1)) { refinerOneButton.Disabled = false; }
		if (vars.CurrentSucrose >= GetRefineCost(2) && CheckReadyForCombine(2)) { refinerTwoButton.Disabled = false; }
		if (vars.CurrentSucrose >= GetRefineCost(3) && CheckReadyForCombine(3)) { refinerThreeButton.Disabled = false; }
		if (vars.CurrentSucrose >= GetRefineCost(4) && CheckReadyForCombine(4)) { refinerFourButton.Disabled = false; }
		if (vars.CurrentSucrose >= GetRefineCost(5) && CheckReadyForCombine(5)) { refinerFiveButton.Disabled = false; }
	}
	
	public bool AddExtractToRefiner(BaseExtract item) {
		if (!refinerPanel.Visible) { return false; }
		foreach (var slot in allRefinerSlots) {
			if (slot.outputSlot) { continue; } //make sure its an input slot
			if (!slot.slotTier.Equals(item.ExtractTier)) { continue; } //match the extract tier to the item tier
			if (slot.GetFull()) { continue; }
			slot.AddExtractToSlot(item);
			CheckRefinerButtons();
			return true;
		}
		return false;
	}

	private void RemoveExtractFromDistillery(BaseExtract item, DistillerySlot slot) {
		// GD.Print("removing " + item.Name);
		if (ui.GetExtractsInventoryFull()) { return; } //add notification about inventory full        
		// if (_equippedExtractsData.Remove(item)) { item.Equipped = false; } //items not saved in refiner
		ui.AddExtractToInventory(item);
		slot.Reset();
		CheckRefinerButtons();
		CheckCondenseButton();
		collectCondenseExtractLabel.Visible = false; //BAD SPOT -this will reset if extract is removed from refiner
		chancePanelContainer.Visible = true; //BAD SPOT -this will reset if extract is removed from refiner
		condenserOutputLabel.Text = ""; //BAD SPOT -this will reset if extract is removed from refiner
	}

	private int GetRefineCost(int tier) {
		switch (tier) {
			case 1: return 1000;
			case 2: return 2000;
			case 3: return 3000;
			case 4: return 5000;
			case 5: return 10000;
		}
		return 0;
	}
	
	private void CombineExtracts(int tier) {
		if (tier < 1) { return; }
		if (!CheckReadyForCombine(tier)) { return; }
		main.ChangeSucrose(-GetRefineCost(tier));
		GetNewExtractOutput(tier + 1);
		ResetRefinerInputSlots(tier);
		CheckRefinerButtons();
	}

	private bool CheckReadyForCombine(int tier) {
		foreach (var slot in allRefinerSlots) { //check for output slot full
			if (!slot.outputSlot) { continue; }
			if (!slot.slotTier.Equals(tier)) { continue; }
			switch (tier) {
				case 1: if (slot.GetFull()) { return false; } break;
				case 2: if (slot.GetFull()) { return false; } break;
				case 3: if (slot.GetFull()) { return false; } break;
				case 4: if (slot.GetFull()) { return false; } break;
				case 5: if (slot.GetFull()) { return false; } break;
			}
		}
		
		var tierOne = 0;
		var tierTwo = 0;
		var tierThree = 0;
		var tierFour = 0;
		var tierFive = 0;
		
		foreach (var slot in allRefinerSlots) {
			if (slot.outputSlot) { continue; }
			if (!slot.slotTier.Equals(tier)) { continue; }
			switch (tier) {
				case 1: if (slot.GetFull()) { tierOne++; } break;
				case 2: if (slot.GetFull()) { tierTwo++; } break;
				case 3: if (slot.GetFull()) { tierThree++; } break;
				case 4: if (slot.GetFull()) { tierFour++; } break;
				case 5: if (slot.GetFull()) { tierFive++; } break;
			}
		}
		
		switch (tier) {
			case 1: return tierOne == 3;
			case 2: return tierTwo == 3;
			case 3: return tierThree == 3;
			case 4: return tierFour == 3;
			case 5: return tierFive == 3;
		}
		return false;
	}
	
	private void GetNewExtractOutput(int tier) {
		if (tier > 5) { tier = 5; } //not done, this will upgrade a t5 extract
		BaseExtract newExtract = extractManager.GenerateNewDistilleryExtract(tier);
		foreach (var slot in allRefinerSlots) {
			if (!slot.outputSlot) { continue; }
			if (!slot.slotTier.Equals(tier - 1)) { continue; }
			if (slot.GetFull()) { continue; }
			slot.AddExtractToSlot(newExtract);
			return;
		}
	}
	
	private void ResetRefinerInputSlots(int tier) {
		foreach (var slot in allRefinerSlots) {
			if (slot.outputSlot) { continue; }
			if (!slot.slotTier.Equals(tier)) { continue; }
			slot.Reset();
		}
	}
	
	//condenser
	private void SliderValueChanged(float value) {
		SetCondenserOutputDisplay();
	}

	private void SetCondenserOutputDisplay() {
		chancePanelContainer.Visible = true;
		condenserOutputLabel.Text = "";
		
		var chanceTierOne = GetCondenserChance(1);
		var chanceTierTwo = GetCondenserChance(2);
		var chanceTierThree = GetCondenserChance(3);
		var chanceTierFour = GetCondenserChance(4);
		var chanceTierFive = GetCondenserChance(5);
		
		condenserTierOneChanceLabel.Text = chanceTierOne + "%";
		condenserTierTwoChanceLabel.Text = chanceTierTwo + "%";
		condenserTierThreeChanceLabel.Text = chanceTierThree + "%";
		condenserTierFourChanceLabel.Text = chanceTierFour + "%";
		condenserTierFiveChanceLabel.Text = chanceTierFive + "%";
		
		condenserTierOne.Visible = chanceTierOne > 0;
		condenserTierTwo.Visible = chanceTierTwo > 0;
		condenserTierThree.Visible = chanceTierThree > 0;
		condenserTierFour.Visible = chanceTierFour > 0;
		condenserTierFive.Visible = chanceTierFive > 0;
		
		condenserCostLabel.Text = GetCondenseCost().ToString("N0");
		CheckCondenseButton();
	}

	private void CheckCondenseButton() {
		var afford = vars.CurrentSucrose >= GetCondenseCost();
		var full = condenserOutputSlot.GetFull();

		if (afford && !full) { condenserButton.Disabled = false; }
		else if (!afford) { condenserButton.Disabled = true; }
		else if (full) { condenserButton.Disabled = true; }
		
		chancePanel.Disabled = condenserButton.Disabled;
	}
	
	private int GetCondenserChance(int tier) {
		var sliderLoc = (int)condenserSlider.Value;
		return condenserData[sliderLoc - 1, tier - 1];
	}

	private int GetCondenseCost() {
		return (int) condenserSlider.Value * 10000;
	}

	private void CreateCondenserExtract() {
		var finalTier = 0;
		List<(int tier, float chance)> tierChances = new List<(int, float)>();
		tierChances.Add((1, GetCondenserChance(1)));
		tierChances.Add((2, GetCondenserChance(2)));
		tierChances.Add((3, GetCondenserChance(3)));
		tierChances.Add((4, GetCondenserChance(4)));
		tierChances.Add((5, GetCondenserChance(5)));
		
		float randomNumber = GD.Randf() * 100.0f;

		float cumulativeChance = 0.0f;
		foreach (var tierInfo in tierChances) {
			cumulativeChance += tierInfo.chance;
			// GD.Print($"Rolling {randomNumber:0.00}% against {cumulativeChance:0.00}% for Tier {tierInfo.tier}");
			if (randomNumber < cumulativeChance) {
				finalTier = tierInfo.tier;
				break;
			}
		}
		chancePanelContainer.Visible = false;
		condenserOutputLabel.Text = $"Created Tier {finalTier} Extract";
		BaseExtract newExtract = extractManager.GenerateNewDistilleryExtract(finalTier);
		condenserOutputSlot.AddExtractToSlot(newExtract);
		collectCondenseExtractLabel.Visible = true;
		CheckCondenseButton();
	}
	
	private void HideCondenserOutput() {
		condenserTierOne.Visible = false;
		condenserTierTwo.Visible = false;
		condenserTierThree.Visible = false;
		condenserTierFour.Visible = false;
		condenserTierFive.Visible = false;
	}
	
	public void DistilleryOpened(InventoryExtracts inventoryExtracts) { //rename, and remove inventory WHEN CLOSING
		CheckRefinerButtons();
		if (!inventoryContainer.HasNode("InventoryExtracts")) { inventoryContainer.AddChild(inventoryExtracts); }
		// if (!inventoryPlayerStatsContainer.HasNode("PlayerStats")) { inventoryPlayerStatsContainer.AddChild(playerStats); }
		ShowCondenser();
		// ShowRefiner();
		UpdateDistillery();
	}

	public void DistilleryClosed(InventoryExtracts inventoryExtracts) { //refactor
		if (inventoryContainer.HasNode("InventoryExtracts")) { inventoryContainer.RemoveChild(inventoryExtracts); }
		GetTree().Paused = false;
	}
	
	private void ShowCondenser() {
		condenserBuildScreen.Visible = false;
		refinerPanel.Visible = false;
		condenserPanel.Visible = true;
		swapButton.TextureNormal = swapRefinerButtonTexture;
		// swapButton.TextureNormal = swapCondenserButtonTexture;
		var blackText = tooltips.GetDecimalColor("black");
		var whiteText = tooltips.GetDecimalColor("white");
		swapRefinerLabel.Set("theme_override_colors/font_color", blackText);
		swapCondenserLabel.Set("theme_override_colors/font_color", whiteText);
		swapLabelLeft.Visible = true;
		swapLabelRight.Visible = false;
		refinerAddArrow.Visible = false;
		SetCondenserOutputDisplay();
		CheckCondenserBuild();
	}

	private void CheckCondenserBuild() {
		inventoryContainer.Visible = vars.CondenserBuilt;
		if (vars.CondenserBuilt) { return; }
		condenserBuildScreen.Visible = true;
		condenserBuildButton.Disabled = true;
		var sucroseMet = CheckCondenserSucroseRequirementMet();
		var essenceMet = CheckCondenserEssenceRequirementMet();
		var partMet = CheckCondenserPartRequirementMet();
		
		if (sucroseMet && essenceMet && partMet) {
			condenserBuildButton.Disabled = false;
		}
		
		UpdateCondenserBuildDisplay();
	}

	private void UpdateCondenserBuildDisplay() {
		var sucroseCostLabel = GetNode<Label>("Condenser/Build/MaterialOne/Label");
		sucroseCostLabel.Text = $"Sucrose\n" +
		                        $"{vars.CurrentSucrose}/{GetCondenserBuildSucroseCost()}";
		
		sucroseCostLabel.Set("theme_override_colors/font_color",
			CheckCondenserSucroseRequirementMet() ? tooltips.GetDecimalColor("green") : tooltips.GetDecimalColor("red"));
		
		var essenceCostLabel = GetNode<Label>("Condenser/Build/MaterialTwo/Label");
		essenceCostLabel.Text = $"Chewy Essence\n" +
		                        $"{ui.GetOwnedMaterialAmount("essenceChewy")}/{GetCondenserBuildEssenceCost()}";
		essenceCostLabel.Set("theme_override_colors/font_color",
			CheckCondenserEssenceRequirementMet() ? tooltips.GetDecimalColor("green") : tooltips.GetDecimalColor("red"));
		
		var partCostLabel = GetNode<Label>("Condenser/Build/MaterialThree/Label");
		partCostLabel.Text = $"Condenser Part\n" +
		                     $"{ui.GetOwnedMaterialAmount("partCondenser")}/{GetCondenserBuildPartCost()}";
		partCostLabel.Set("theme_override_colors/font_color",
			CheckCondenserPartRequirementMet() ? tooltips.GetDecimalColor("green") : tooltips.GetDecimalColor("red"));
	}
	
	private void BuildCondenser() {
		main.ChangeSucrose(-GetCondenserBuildSucroseCost());
		main.LoseMaterial(ui.GetMaterialById("essenceChewy"), GetCondenserBuildEssenceCost());
		main.LoseMaterial(ui.GetMaterialById("partCondenser"), GetCondenserBuildPartCost());
		vars.CondenserBuilt = true;
		ShowCondenser();
	}
	
	private bool CheckCondenserSucroseRequirementMet() { return vars.CurrentSucrose >= GetCondenserBuildSucroseCost(); }
	private bool CheckCondenserEssenceRequirementMet() { return ui.GetOwnedMaterialAmount("essenceChewy") >= GetCondenserBuildEssenceCost(); }
	private bool CheckCondenserPartRequirementMet() { return ui.GetOwnedMaterialAmount("partCondenser") >= GetCondenserBuildPartCost(); }
	
	private int GetCondenserBuildSucroseCost() { return 1000; }
	private int GetCondenserBuildEssenceCost() { return 2; }
	private int GetCondenserBuildPartCost() { return 1; }
	
	private void ShowRefiner() {
		refinerBuildScreen.Visible = false;
		refinerPanel.Visible = true;
		condenserPanel.Visible = false;
		// swapButton.TextureNormal = swapRefinerButtonTexture;
		swapButton.TextureNormal = swapCondenserButtonTexture;
		var blackText = tooltips.GetDecimalColor("black");
		var whiteText = tooltips.GetDecimalColor("white");
		swapRefinerLabel.Set("theme_override_colors/font_color", whiteText);
		swapCondenserLabel.Set("theme_override_colors/font_color",blackText);
		swapLabelLeft.Visible = false;
		swapLabelRight.Visible = true;
		refinerAddArrow.Visible = true;
		CheckRefinerBuild();
	}
	
	private void CheckRefinerBuild() {
		inventoryContainer.Visible = vars.RefinerBuilt;
		if (vars.RefinerBuilt) { return; }
		refinerBuildScreen.Visible = true;
		refinerBuildButton.Disabled = true;
		var sucroseMet = CheckRefinerSucroseRequirementMet();
		var essenceMet = CheckRefinerEssenceRequirementMet();
		var partMet = CheckRefinerPartRequirementMet();
		
		if (sucroseMet && essenceMet && partMet) {
			refinerBuildButton.Disabled = false;
		}
	}

	private void BuildRefiner() {
		main.ChangeSucrose(-GetRefinerBuildSucroseCost());
		//GetRefinerBuildEssenceCost()
		//GetRefinerBuildPartCost()
		vars.RefinerBuilt = true;
		ShowRefiner();
	}
	
	private bool CheckRefinerSucroseRequirementMet() {
		return vars.CurrentSucrose >= GetRefinerBuildSucroseCost();
	}
	
	private bool CheckRefinerEssenceRequirementMet() {
		return vars.CurrentSucrose >= GetRefinerBuildEssenceCost();
	}
	
	private bool CheckRefinerPartRequirementMet() {
		return vars.CurrentSucrose >= GetRefinerBuildPartCost();
	}
	
	private int GetRefinerBuildSucroseCost() {
		return 1;
	}
	
	private int GetRefinerBuildEssenceCost() {
		return 10;
	}
	
	private int GetRefinerBuildPartCost() {
		return 1;
	}
	
	private void SwapRefinerCondenser() {
		if (refinerPanel.Visible) { ShowCondenser(); }
		else if (condenserPanel.Visible) { ShowRefiner(); }
	}
	
	private void BuildCondenserData() {
		condenserData = new int[100, 5] {
			{80, 20, 0, 0, 0},
			{77, 23, 0, 0, 0},
			{75, 25, 0, 0, 0},
			{72, 28, 0, 0, 0},
			{69, 31, 0, 0, 0},
			{67, 33, 0, 0, 0},
			{64, 36, 0, 0, 0},
			{61, 39, 0, 0, 0},
			{59, 41, 0, 0, 0},
			{56, 44, 0, 0, 0},
			{53, 47, 0, 0, 0},
			{51, 49, 0, 0, 0},
			{48, 52, 0, 0, 0},
			{45, 55, 0, 0, 0},
			{43, 57, 0, 0, 0},
			{40, 60, 0, 0, 0},
			{37, 63, 0, 0, 0},
			{35, 65, 0, 0, 0},
			{32, 68, 0, 0, 0},
			{29, 71, 0, 0, 0},
			{27, 73, 0, 0, 0},
			{24, 76, 0, 0, 0},
			{21, 79, 0, 0, 0},
			{19, 81, 0, 0, 0},
			{16, 80, 4, 0, 0},
			{13, 77, 9, 0, 0},
			{11, 75, 15, 0, 0},
			{8, 72, 20, 0, 0},
			{5, 69, 25, 0, 0},
			{3, 67, 31, 0, 0},
			{0, 64, 36, 0, 0},
			{0, 61, 39, 0, 0},
			{0, 59, 41, 0, 0},
			{0, 56, 44, 0, 0},
			{0, 53, 47, 0, 0},
			{0, 51, 49, 0, 0},
			{0, 48, 52, 0, 0},
			{0, 45, 55, 0, 0},
			{0, 43, 57, 0, 0},
			{0, 40, 60, 0, 0},
			{0, 37, 63, 0, 0},
			{0, 35, 65, 0, 0},
			{0, 32, 68, 0, 0},
			{0, 29, 71, 0, 0},
			{0, 27, 73, 0, 0},
			{0, 24, 76, 0, 0},
			{0, 21, 79, 0, 0},
			{0, 19, 81, 0, 0},
			{0, 16, 80, 4, 0},
			{0, 13, 77, 9, 0},
			{0, 11, 75, 15, 0},
			{0, 8, 72, 20, 0},
			{0, 5, 69, 25, 0},
			{0, 3, 67, 31, 0},
			{0, 0, 64, 36, 0},
			{0, 0, 61, 39, 0},
			{0, 0, 59, 41, 0},
			{0, 0, 56, 44, 0},
			{0, 0, 53, 47, 0},
			{0, 0, 51, 49, 0},
			{0, 0, 48, 52, 0},
			{0, 0, 45, 55, 0},
			{0, 0, 43, 57, 0},
			{0, 0, 40, 60, 0},
			{0, 0, 37, 63, 0},
			{0, 0, 35, 65, 0},
			{0, 0, 32, 68, 0},
			{0, 0, 29, 71, 0},
			{0, 0, 27, 73, 0},
			{0, 0, 24, 76, 0},
			{0, 0, 21, 79, 0},
			{0, 0, 19, 81, 0},
			{0, 0, 16, 80, 4},
			{0, 0, 13, 77, 9},
			{0, 0, 11, 75, 15},
			{0, 0, 8, 72, 20},
			{0, 0, 5, 69, 25},
			{0, 0, 3, 67, 31},
			{0, 0, 0, 64, 36},
			{0, 0, 0, 61, 39},
			{0, 0, 0, 59, 41},
			{0, 0, 0, 56, 44},
			{0, 0, 0, 53, 47},
			{0, 0, 0, 51, 49},
			{0, 0, 0, 48, 52},
			{0, 0, 0, 45, 55},
			{0, 0, 0, 43, 57},
			{0, 0, 0, 40, 60},
			{0, 0, 0, 37, 63},
			{0, 0, 0, 35, 65},
			{0, 0, 0, 32, 68},
			{0, 0, 0, 29, 71},
			{0, 0, 0, 27, 73},
			{0, 0, 0, 24, 76},
			{0, 0, 0, 21, 79},
			{0, 0, 0, 19, 81},
			{0, 0, 0, 16, 84},
			{0, 0, 0, 13, 87},
			{0, 0, 0, 11, 89},
			{0, 0, 0, 8, 92}
		};
	}
}