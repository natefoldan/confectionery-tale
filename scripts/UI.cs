using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace ConfectioneryTale.scripts;

public partial class UI : CanvasLayer {
    [Signal]
    public delegate void ExtractInventoryChangedEventHandler();

    // [Signal] public delegate void MaterialsChangedEventHandler(); //moved to main
    [Signal]
    public delegate void ObjectBuiltEventHandler(string what);

    [Export] private BaseMaterial[] allMaterials;
    private Main main;
    private Variables vars;
    private TooltipHandler tooltips;
    private Distillery distillery;
    private Skills skills;
    private Assignments assignments;

    private PackedScene inventoryExtractsSlotScene;
    private PackedScene itemSlotScene;
    private const byte MaxInventoryExtractSlots = 100;

    private Array<BaseExtract>
        inventoryExtractsData; //reference to the list in SavedResources. will hold the actual Array<BaseExtract> that gets saved/loaded  //moved to inventoryextracts

    private Array<BaseExtract> equippedExtractsData;
    public Array<SavedItemData> savedItemDataArray = new Array<SavedItemData>();

    private TextureRect worldMap;
    private TextureProgressBar playerExpBar;
    private TextureProgressBar playerReloadBar;
    private TextureProgressBar playerShieldBar;
    private TextureProgressBar playerHealthBar;
    private Label playerShieldBarLabel;
    private Label playerHealthBarLabel;
    private Label playerExpLabel;
    private Label playerShieldLabel;
    private Label playerHealthLabel;
    private Label playerReloadLabel;
    private Label playerLevelLabel;
    private Texture2D equipmentPanelTextureNormal;
    private Texture2D equipmentPanelTextureActive;

    private List<ItemSlot> allInventoryExtractSlots;
    private List<ItemSlot> allEquippedExtractSlots;
    private List<ItemSlot> allMaterialsSlots;

    //game hud
    private TextureButton radialOneButton;
    private TextureButton radialTwoButton;
    private TextureButton radialThreeButton;
    private TextureButton radialFourButton;
    private TextureButton radialFiveButton;
    private TextureButton radialSixButton;
    private TextureRect radialTwoButtonBullet;
    private TextureRect radialThreeButtonBullet;
    private TextureRect radialFourButtonBullet;
    private TextureRect radialFiveButtonBullet;
    private TextureRect radialSixButtonBullet;
    private Texture2D radialOneTextureNormal;
    private Texture2D radialOneTextureActive;
    private Texture2D radialTwoTextureNormal;
    private Texture2D radialTwoTextureActive;
    private Texture2D radialThreeTextureNormal;
    private Texture2D radialThreeTextureActive;
    private Texture2D radialFourTextureNormal;
    private Texture2D radialFourTextureActive;
    private Texture2D radialFiveTextureNormal;
    private Texture2D radialFiveTextureActive;
    private Texture2D radialSixTextureNormal;
    private Texture2D radialSixTextureActive;

    // private List<TextureButton> allWeaponSlots; //if using selectable slots at bottom of hud
    // private List<TextureButton> allRadialButtons; //not used
    private List<(string stat, TextureRect tooltip, string lookupString)> allWeaponStatTooltips;
    // private Label currentWeaponlabel;

    private TextureButton tabInventory;
    private TextureButton tabEquipment;
    private TextureButton tabSkills;
    private TextureButton tabAssignments;
    private TextureButton tabProgression;
    private TextureButton tabSettings;
    private Texture2D tabTextureActive;
    private Texture2D tabTextureInactive;

    private Control mainMenu;
    private Control panelInventory;
    private Control panelEquipment;
    private Control panelSkills;
    private Control panelAssignments;
    private Control panelProgression;
    private Control panelSettings;
    private List<Control> allPanels;
    private string previousPanel;

    //player stats
    private InventoryExtracts inventoryExtracts;
    private PlayerStats playerStats;
    private Node2D inventoryInventoryExtractsContainer;
    private Node2D inventoryPlayerStatsContainer;
    private Node2D equipmentPlayerStatsContainer;
    private RichTextLabel statLabelDamage;
    private RichTextLabel statLabelPierce;
    private RichTextLabel statLabelCrit;
    private RichTextLabel statLabelCritDamage;
    private RichTextLabel statLabelShield;
    private RichTextLabel statLabelShieldRegen;
    private RichTextLabel statLabelHealth;
    private RichTextLabel statLabelRange;
    private RichTextLabel statLabelSpeed;
    private RichTextLabel statLabelExtractDrop;
    private RichTextLabel statLabelSucroseDrop;
    private RichTextLabel statLabelExpDrop;
    private RichTextLabel statLabelSlow;
    private RichTextLabel statLabelMelt;
    private RichTextLabel statLabelSlowResist;
    private RichTextLabel statLabelMeltResist;
    private Label currentSucroseLabel;

    //weapon stats
    private TextureButton weaponButtonSprayer;
    private TextureButton weaponButtonSoftener;
    private TextureButton weaponButtonSpreader;
    private TextureButton weaponButtonSniper;
    private TextureButton weaponButtonSlower;
    private TextureButton weaponButtonSmasher;
    private List<TextureButton> allGearButtons;

    //radial menu
    private Control radialMenu;

    //hud buttons
    private Texture2D tinctureHealthTexture;
    private Texture2D tinctureHealthTextureBlack;
    private TextureRect tinctureHealthIcon;
    private Label tinctureHealthAmountLabel;
    private ProgressBar tinctureHealthCooldownBar;
    private Label tinctureHealthCooldownLabel;
    
    private Texture2D tinctureSpeedTexture;
    private Texture2D tinctureSpeedTextureBlack;
    private TextureRect tinctureSpeedIcon;
    private Label tinctureSpeedAmountLabel;
    private ProgressBar tinctureSpeedCooldownBar;
    private Label tinctureSpeedCooldownLabel;
    
    private Texture2D tinctureConcealTexture;
    private Texture2D tinctureConcealTextureBlack;
    private TextureRect tinctureConcealIcon;
    private Label tinctureConcealAmountLabel;
    private ProgressBar tinctureConcealCooldownBar;
    private Label tinctureConcealCooldownLabel;
    
    private ProgressBar tentCooldownBar;
    private Label tentCooldownLabel;
    
    //world text
    private Label worldTextLabel;
    private Timer worldTextTimer;
    private StringBuilder currentDisplayedText = new StringBuilder();
    private float wordsPerSecond = 10f;
    private float charactersPerSecond = 30f;
    private int currentWordIndex = 0;
    private int currentCharIndex = 0;
    private string[] words; //not used with characters
    private PackedScene popupBoxScene;
    private PackedScene popupTutorialScene;
    
    private class TutorialText {
        public string Header;
        public string Description;
    }
    
    private System.Collections.Generic.Dictionary<string, TutorialText> tutorialDatabase;

    public override void _Ready() {
        vars = GetNode<Variables>("/root/Variables");

        if (vars == null) {
            GD.PushError("UI._Ready: Variables singleton not found");
            return;
        }

        tooltips = GetNode<TooltipHandler>("/root/TooltipHandler");
        distillery = GetNode<Distillery>("Distillery");
        skills = GetNode<Skills>("MainMenu/Skills");
        assignments = GetNode<Assignments>("MainMenu/Assignments");

        popupBoxScene = GD.Load<PackedScene>("res://scenes/popup_box.tscn");
        popupTutorialScene = GD.Load<PackedScene>("res://scenes/popup_tutorial.tscn");
        
        SavedResources savedResourcesInstance = vars.GetSavedResourcesInstance();
        // GD.Print($"UI._Ready: savedResourcesInstance is null? {savedResourcesInstance == null}");

        if (savedResourcesInstance == null) {
            GD.PushError("UI._Ready: SavedResources instance is null within Variables singleton!");
            return;
        }

        inventoryExtractsData = savedResourcesInstance.inventoryExtracts;
        equippedExtractsData = savedResourcesInstance.equippedExtracts;
        if (vars.SavedItemDataArray == null) { UpdateSavedItemData(); }
        LoadSavedItemData();
        // SetCompassTarget();
        // UpdateTinctureHealthButton(); //delete
        // UpdateTinctureSpeedButton(); //hmm
        // UpdateTinctureConcealButton(); //hmm
        BuildTutorialDictionary();
    }

    public override void _Process(double delta) {
        ToggleRadialMenu();
        UpdateCampingCooldown(delta);
        UpdateTinctureHealthCooldown(delta);
        UpdateTinctureSpeedCooldown(delta);
        UpdateTinctureConcealCooldown(delta);
        // UpdateCompassArrow();
    }

    private void ToggleRadialMenu() {
        if (Input.IsActionPressed("toggle_radial_menu")) {
            if (radialMenu.Visible) return;
            UpdateRadialButtons();
            radialMenu.Visible = true;
            GetTree().Root.SetInputAsHandled();
        } else {
            if (!radialMenu.Visible) return;
            radialMenu.Visible = false;
            GetTree().Root.SetInputAsHandled();
        }

        // var centerPoint = GetNode<Node2D>("RadialMenu/CenterPoint"); //delete
        var centerPoint = GetNode<Control>("RadialMenu/CenterPoint");
        Input.WarpMouse(centerPoint.GlobalPosition);
    }
    
    public void InitializeUIComponents(Main gameMain) {
        main = gameMain;
        main.SucroseChanged += HandleSucroseChanged;
        ExtractInventoryChanged += HandleExtractsInventoryChanged;
        main.MaterialsChanged += HandleMaterialsChanged;

        itemSlotScene = GD.Load<PackedScene>("res://scenes/item_slot.tscn");
        
        var playerStatsScene = GD.Load<PackedScene>("res://scenes/player_stats.tscn");
        playerStats = playerStatsScene.Instantiate<PlayerStats>();
        
        var inventoryExtractsScene = GD.Load<PackedScene>("res://scenes/inventory_extracts.tscn");
        inventoryExtracts = inventoryExtractsScene.Instantiate<InventoryExtracts>();
        inventoryExtracts.InventorySorted += SortExtractsInventory;
        
        inventoryInventoryExtractsContainer = GetNode<Node2D>("MainMenu/Inventory/InventoryExtractsContainer");
        inventoryPlayerStatsContainer = GetNode<Node2D>("MainMenu/Inventory/PlayerStatsContainer");
        equipmentPlayerStatsContainer = GetNode<Node2D>("MainMenu/Equipment/PlayerStatsContainer");
        playerLevelLabel = playerStats.GetNode<Label>("Level");
        statLabelDamage = playerStats.GetNode<RichTextLabel>("Stats/Damage/RichTextLabel");
        statLabelPierce = playerStats.GetNode<RichTextLabel>("Stats/Pierce/RichTextLabel");
        statLabelCrit = playerStats.GetNode<RichTextLabel>("Stats/CritChance/RichTextLabel");
        statLabelCritDamage = playerStats.GetNode<RichTextLabel>("Stats/CritDamage/RichTextLabel");
        statLabelShield = playerStats.GetNode<RichTextLabel>("Stats/Shield/RichTextLabel");
        statLabelShieldRegen = playerStats.GetNode<RichTextLabel>("Stats/ShieldRegen/RichTextLabel");
        statLabelHealth = playerStats.GetNode<RichTextLabel>("Stats/Health/RichTextLabel");
        statLabelRange = playerStats.GetNode<RichTextLabel>("Stats/Range/RichTextLabel");
        statLabelSpeed = playerStats.GetNode<RichTextLabel>("Stats/Speed/RichTextLabel");
        statLabelExtractDrop = playerStats.GetNode<RichTextLabel>("Stats/ExtractDrop/RichTextLabel");
        statLabelSucroseDrop = playerStats.GetNode<RichTextLabel>("Stats/SucroseDrop/RichTextLabel");
        statLabelExpDrop = playerStats.GetNode<RichTextLabel>("Stats/ExpDrop/RichTextLabel");
        statLabelSlow = playerStats.GetNode<RichTextLabel>("Stats/Slow/RichTextLabel");
        statLabelMelt = playerStats.GetNode<RichTextLabel>("Stats/Melt/RichTextLabel");
        statLabelSlowResist = playerStats.GetNode<RichTextLabel>("Stats/SlowResist/RichTextLabel");
        statLabelMeltResist = playerStats.GetNode<RichTextLabel>("Stats/MeltResist/RichTextLabel");
        currentSucroseLabel = GetNode<Label>("WorldHud/CurrentSucrose/Label");
        currentSucroseLabel.Text = TrimNumber(vars.CurrentSucrose);
        
        worldMap = GetNode<TextureRect>("WorldMap");
        worldMap.Visible = false;
        
        playerExpBar = GetNode<TextureProgressBar>("WorldHud/PlayerExpBar");
        playerReloadBar = GetNode<TextureProgressBar>("WorldHud/ReloadBar");
        playerShieldBar = GetNode<TextureProgressBar>("WorldHud/PlayerShieldBar");
        playerHealthBar = GetNode<TextureProgressBar>("WorldHud/PlayerHealthBar");
        playerShieldBarLabel = GetNode<Label>("WorldHud/PlayerShieldBar/Label");
        playerHealthBarLabel = GetNode<Label>("WorldHud/PlayerHealthBar/Label");
        playerExpLabel = GetNode<Label>("WorldHud/PlayerExpBar/Label");
        playerReloadLabel = GetNode<Label>("WorldHud/ReloadBar/Label");
        // currentWeaponlabel = GetNode<Label>("WorldHud/WeaponPanel/Name");

        weaponButtonSprayer = GetNode<TextureButton>("MainMenu/Equipment/Weapons/Sprayer");
        weaponButtonSoftener = GetNode<TextureButton>("MainMenu/Equipment/Weapons/Softener");
        weaponButtonSpreader = GetNode<TextureButton>("MainMenu/Equipment/Weapons/Spreader");
        weaponButtonSniper = GetNode<TextureButton>("MainMenu/Equipment/Weapons/Sniper");
        weaponButtonSlower = GetNode<TextureButton>("MainMenu/Equipment/Weapons/Slower");
        weaponButtonSmasher = GetNode<TextureButton>("MainMenu/Equipment/Weapons/Smasher");
        allGearButtons = new List<TextureButton> { weaponButtonSprayer, weaponButtonSoftener, weaponButtonSpreader, weaponButtonSniper, weaponButtonSlower, weaponButtonSmasher };

        weaponButtonSprayer.GetNode<Label>("Description").Text = main.GetAllWeaponData()[0].Description;
        weaponButtonSoftener.GetNode<Label>("Description").Text = main.GetAllWeaponData()[1].Description;
        weaponButtonSpreader.GetNode<Label>("Description").Text = main.GetAllWeaponData()[2].Description;
        weaponButtonSniper.GetNode<Label>("Description").Text = main.GetAllWeaponData()[3].Description;
        weaponButtonSlower.GetNode<Label>("Description").Text = main.GetAllWeaponData()[4].Description;
        weaponButtonSmasher.GetNode<Label>("Description").Text = main.GetAllWeaponData()[5].Description;
        radialMenu = GetNode<Control>("RadialMenu");

        worldTextLabel = GetNode<Label>("WorldText/Label");
        worldTextTimer = GetNode<Timer>("WorldText/Timer");
        
        SetupHudButtons();
        UpdateExpBar();
        GetRadialButtons();
        BuildInventoryExtractSlots();
        BuildEquippedExtractSlots();
        BuildMaterialsSlots();
        BuildAllWeaponStatTooltipsList();
        SetupMenus();
        //for testing -delete
        // foreach (var material in allMaterials) { 
        //     GD.Print($"{material.Name}: {material.CurrentOwned}");
        // }
    }

    private void SetupHudButtons() {
        tentCooldownBar = GetNode<ProgressBar>("WorldHud/Useables/Tent/ProgressBar");
        tentCooldownLabel = GetNode<Label>("WorldHud/Useables/Tent/ProgressBar/TimeLabel");
        tentCooldownBar.Visible = false;
        tentCooldownLabel.Visible = false;
        
        tinctureHealthIcon = GetNode<TextureRect>("WorldHud/Consumables/TinctureHealth/TextureRect");
        tinctureHealthAmountLabel = GetNode<Label>("WorldHud/Consumables/TinctureHealth/Label");
        tinctureHealthTexture = ResourceLoader.Load<Texture2D>("res://assets/sprites/tincture-health.png");
        tinctureHealthTextureBlack = ResourceLoader.Load<Texture2D>("res://assets/sprites/tincture-health-black.png");
        
        tinctureSpeedIcon = GetNode<TextureRect>("WorldHud/Consumables/TinctureSpeed/TextureRect");
        tinctureSpeedAmountLabel = GetNode<Label>("WorldHud/Consumables/TinctureSpeed/Label");
        tinctureSpeedTexture = ResourceLoader.Load<Texture2D>("res://assets/sprites/tincture-health.png"); //change to green
        tinctureSpeedTextureBlack = ResourceLoader.Load<Texture2D>("res://assets/sprites/tincture-health-black.png");
        
        tinctureConcealIcon = GetNode<TextureRect>("WorldHud/Consumables/TinctureConceal/TextureRect");
        tinctureConcealAmountLabel = GetNode<Label>("WorldHud/Consumables/TinctureConceal/Label");
        tinctureConcealTexture = ResourceLoader.Load<Texture2D>("res://assets/sprites/tincture-health.png"); //change to purple
        tinctureConcealTextureBlack = ResourceLoader.Load<Texture2D>("res://assets/sprites/tincture-health-black.png");
        
        tinctureHealthCooldownBar = GetNode<ProgressBar>("WorldHud/Consumables/TinctureHealth/ProgressBar");
        tinctureHealthCooldownLabel = GetNode<Label>("WorldHud/Consumables/TinctureHealth/ProgressBar/Label");
        tinctureHealthCooldownBar.Visible = false;
        tinctureHealthCooldownLabel.Visible = false;
        
        tinctureSpeedCooldownBar = GetNode<ProgressBar>("WorldHud/Consumables/TinctureSpeed/ProgressBar");
        tinctureSpeedCooldownLabel = GetNode<Label>("WorldHud/Consumables/TinctureSpeed/ProgressBar/Label");
        tinctureSpeedCooldownBar.Visible = false;
        tinctureSpeedCooldownLabel.Visible = false;
        
        tinctureConcealCooldownBar = GetNode<ProgressBar>("WorldHud/Consumables/TinctureConceal/ProgressBar");
        tinctureConcealCooldownLabel = GetNode<Label>("WorldHud/Consumables/TinctureConceal/ProgressBar/Label");
        tinctureConcealCooldownBar.Visible = false;
        tinctureConcealCooldownLabel.Visible = false;
        
        tinctureHealthIcon.Texture = vars.TinctureHealthAmount > 0 ? tinctureHealthTexture : tinctureHealthTextureBlack;
        tinctureHealthAmountLabel.Text = $"{vars.TinctureHealthAmount}";
        tinctureSpeedIcon.Texture = vars.TinctureSpeedAmount > 0 ? tinctureSpeedTexture : tinctureSpeedTextureBlack;
        tinctureSpeedAmountLabel.Text = $"{vars.TinctureSpeedAmount}";
        tinctureConcealIcon.Texture = vars.TinctureConcealAmount > 0 ? tinctureConcealTexture : tinctureConcealTextureBlack;
        tinctureConcealAmountLabel.Text = $"{vars.TinctureConcealAmount}";
        
        // UpdateTinctureHealthButton();
        // UpdateTinctureSpeedButton();
        // UpdateTinctureConcealButton();
    }

    private void UpdateCampingCooldown(double delta) {
        if (vars.TentCooldownTimer > 0) {
            vars.TentCooldownTimer -= delta;
            if (vars.TentCooldownTimer <= 0) {
                vars.TentCooldownTimer = 0;
                tentCooldownBar.Value = 0;
                tentCooldownBar.Visible = false;
                tentCooldownLabel.Visible = false;
            } else {
                tentCooldownBar.Value = vars.TentCooldownTimer;
                double totalSeconds = Math.Ceiling(vars.TentCooldownTimer);

                int minutes = (int)totalSeconds / 60;
                int seconds = (int)totalSeconds % 60;

                // The "D2" format ensures two digits (e.g., "09" instead of "9")
                tentCooldownLabel.Text = $"{minutes:D2}:{seconds:D2}";
            }
        }
    }

    private void ResetCampButton() {
        tentCooldownBar.MaxValue = vars.TentCooldownTimer;
        tentCooldownBar.Value = vars.TentCooldownTimer;
    }
    
    public void ShowCampButtonCooldown() { //rename
        tentCooldownBar.MaxValue = vars.TentCooldownTimer;
        tentCooldownBar.Value = vars.TentCooldownTimer;
        tentCooldownBar.Visible = true;
        tentCooldownLabel.Visible = true;
    }

    public void UpdateTinctureHealthButton() {
        tinctureHealthIcon.Texture = vars.TinctureHealthAmount > 0 ? tinctureHealthTexture : tinctureHealthTextureBlack;
        tinctureHealthAmountLabel.Text = $"{vars.TinctureHealthAmount}";
        ShowTinctureHealthButtonCooldown();
    }
    
    public void UpdateTinctureSpeedButton() {
        tinctureSpeedIcon.Texture = vars.TinctureSpeedAmount > 0 ? tinctureSpeedTexture : tinctureSpeedTextureBlack;
        tinctureSpeedAmountLabel.Text = $"{vars.TinctureSpeedAmount}";
        ShowTinctureSpeedButtonCooldown();
    }
    
    public void UpdateTinctureConcealButton() {
        tinctureConcealIcon.Texture = vars.TinctureConcealAmount > 0 ? tinctureConcealTexture : tinctureConcealTextureBlack;
        tinctureConcealAmountLabel.Text = $"{vars.TinctureConcealAmount}";
        ShowTinctureConcealButtonCooldown();
    }

    private void ShowTinctureHealthButtonCooldown() {
        tinctureHealthCooldownBar.MaxValue = vars.TinctureHealthCooldown;
        tinctureHealthCooldownBar.Value = vars.TinctureHealthCooldown;
        tinctureHealthCooldownBar.Visible = true;
        tinctureHealthCooldownLabel.Visible = true;
    }
    
    private void ShowTinctureSpeedButtonCooldown() {
        tinctureSpeedCooldownBar.MaxValue = vars.TinctureSpeedCooldown;
        tinctureSpeedCooldownBar.Value = vars.TinctureSpeedCooldown;
        tinctureSpeedCooldownBar.Visible = true;
        tinctureSpeedCooldownLabel.Visible = true;
    }
    
    private void ShowTinctureConcealButtonCooldown() {
        tinctureConcealCooldownBar.MaxValue = vars.TinctureConcealCooldown;
        tinctureConcealCooldownBar.Value = vars.TinctureConcealCooldown;
        tinctureConcealCooldownBar.Visible = true;
        tinctureConcealCooldownLabel.Visible = true;
    }
    
    private void UpdateTinctureHealthCooldown(double delta) {
        // if (vars.TinctureHealthCooldown <= 0) { return; } //FIX

        if (vars.TinctureHealthCooldown > 0) {
            vars.TinctureHealthCooldown -= delta;
            if (vars.TinctureHealthCooldown <= 0) {
                vars.TinctureHealthCooldown = 0;
                tinctureHealthCooldownBar.Value = 0;
                tinctureHealthCooldownBar.Visible = false;
                tinctureHealthCooldownLabel.Visible = false;
            }
            else {
                tinctureHealthCooldownBar.Value = vars.TinctureHealthCooldown;
                double totalSeconds = Math.Ceiling(vars.TinctureHealthCooldown);
                int seconds = (int) totalSeconds % 60;
                tinctureHealthCooldownLabel.Text = $"{seconds:D2}";
            }
        }
    }
    
    private void UpdateTinctureSpeedCooldown(double delta) {
        // if (vars.TinctureSpeedCooldown <= 0) { return; } //FIX
        
        if (vars.TinctureSpeedCooldown > 0) {
            vars.TinctureSpeedCooldown -= delta;
            if (vars.TinctureSpeedCooldown <= 0) {
                vars.TinctureSpeedCooldown = 0;
                tinctureSpeedCooldownBar.Value = 0;
                tinctureSpeedCooldownBar.Visible = false;
                tinctureSpeedCooldownLabel.Visible = false;
            }
            else {
                tinctureSpeedCooldownBar.Value = vars.TinctureSpeedCooldown;
                double totalSeconds = Math.Ceiling(vars.TinctureSpeedCooldown);
                int seconds = (int) totalSeconds % 60;
                tinctureSpeedCooldownLabel.Text = $"{seconds:D2}";
            }
        }
    }

    private void UpdateTinctureConcealCooldown(double delta) {
        // if (vars.TinctureConcealCooldown <= 0) { return; } //FIX
        
        if (vars.TinctureConcealCooldown > 0) {
            vars.TinctureConcealCooldown -= delta;
            if (vars.TinctureConcealCooldown <= 0) {
                vars.TinctureConcealCooldown = 0;
                tinctureConcealCooldownBar.Value = 0;
                tinctureConcealCooldownBar.Visible = false;
                tinctureConcealCooldownLabel.Visible = false;
            } else {
                tinctureConcealCooldownBar.Value = vars.TinctureConcealCooldown;
                double totalSeconds = Math.Ceiling(vars.TinctureConcealCooldown);

                int minutes = (int)totalSeconds / 60;
                int seconds = (int)totalSeconds % 60;

                // The "D2" format ensures two digits (e.g., "09" instead of "9")
                tinctureConcealCooldownLabel.Text = $"{minutes:D2}:{seconds:D2}";
            }
        }
    }
    
    private void testtextreveal() {
        StartTextReveal("");
    }
    
    // public void StartTextReveal(string text) { //by word
    //     if (worldTextLabel == null || worldTextTimer == null) { return; }
    //     
    //     //split the text into words, handling multiple spaces and removing empty entries
    //     words = fullTextToDisplay.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    //     
    //     currentWordIndex = 0;
    //     currentDisplayedText.Clear(); //clear any previous text
    //     worldTextLabel.Text = ""; //start with empty text
    //
    //     if (words.Length > 0) {
    //         //calculate wait time per word (1 / words per second)
    //         worldTextTimer.WaitTime = 1.0f / wordsPerSecond;
    //         worldTextTimer.Start();
    //     } else { //if there's no words, just display the full text immediately
    //         worldTextLabel.Text = fullTextToDisplay;
    //     }
    // }
    //
    // private void OnWorldTextTimerTimeout() { //by word
    //     if (currentWordIndex < words.Length) {
    //         //append the next word and a space (if it's not the last word)
    //         currentDisplayedText.Append(words[currentWordIndex]);
    //         if (currentWordIndex < words.Length - 1) {
    //             currentDisplayedText.Append(" ");
    //         }
    //         
    //         worldTextLabel.Text = currentDisplayedText.ToString();
    //         currentWordIndex++;
    //     }
    //     
    //     //check if all words have been revealed
    //     if (currentWordIndex >= words.Length) {
    //         worldTextTimer.Stop();
    //         // EmitSignal(SignalName.RevealComplete);
    //     } else {
    //         worldTextTimer.Start(); //restart the timer for the next word
    //     }
    // }
    
    private string fullTextToDisplay = "Welcome to Sweetopia.\nline break test, also length test. i don't know how long but this is pretty long"; //Time to begin your Confectionery Tale..
    
    public void StartTextReveal(string text) { //by character
        if (worldTextLabel == null || worldTextTimer == null) { return; }
        
        worldTextLabel.Visible = true;
        worldTextLabel.Modulate = new Color(1, 1, 1, 1);
        
        currentCharIndex = 0; //start from the first character
        worldTextLabel.Text = "";
    
        if (fullTextToDisplay.Length > 0) {
            worldTextTimer.WaitTime = 1.0f / charactersPerSecond; //calculate wait time per character (1 / characters per second)
            worldTextTimer.Start(); // Start the timer
        } else { //if there's no text, just display it immediately (empty string)
            worldTextLabel.Text = fullTextToDisplay; 
        }
    }
    
    private void OnWorldTextTimerTimeout() { //by character
        if (currentCharIndex < fullTextToDisplay.Length) { //check if there are more characters to reveal
            // Append the next character to the currently displayed text
            // For character-by-character, direct string concatenation is often okay,
            // as Label.Text setter in Godot's C# glue has some optimizations.
            // If text is extremely long and performance is an issue, consider StringBuilder.
            worldTextLabel.Text += fullTextToDisplay[currentCharIndex]; 
            currentCharIndex++; // Move to the next character index
        }
        
        // Check if all characters have been revealed
        if (currentCharIndex >= fullTextToDisplay.Length) {
            worldTextTimer.Stop(); // Stop the timer as reveal is complete
            // GD.Print("Character reveal finished.");
            // Optional: Emit a signal here if something else needs to know the reveal is complete
            // EmitSignal(SignalName.RevealComplete);
            
            Tween fadeTween = CreateTween();
            // fadeTween.SetPauseMode(Node.PauseModeEnum.Process); //run even if game paused -delete
            fadeTween.TweenInterval(1.5f);
            fadeTween.TweenProperty(worldTextLabel, "modulate:a", 0.0f, 1.0f);
            fadeTween.TweenCallback(Callable.From(OnDialogueTextFadeOutFinished));
            fadeTween.Play();
        } else {
            worldTextTimer.Start(); //restart the timer for the next character
        }
    }
    
    private void OnDialogueTextFadeOutFinished() {
        worldTextLabel.Visible = false;
    }
    
    private void SetupMenus() {
        tabTextureActive = GD.Load<Texture2D>("res://assets/menu-tab-active.png");
        tabTextureInactive = GD.Load<Texture2D>("res://assets/menu-tab-inactive.png");
        tabInventory = GetNode<TextureButton>("MainMenu/Tabs/Inventory");
        tabEquipment = GetNode<TextureButton>("MainMenu/Tabs/Equipment");
        tabSkills = GetNode<TextureButton>("MainMenu/Tabs/Skills");
        tabAssignments = GetNode<TextureButton>("MainMenu/Tabs/Assignments");
        tabProgression = GetNode<TextureButton>("MainMenu/Tabs/Progression");
        tabSettings = GetNode<TextureButton>("MainMenu/Tabs/Settings");

        mainMenu = GetNode<Control>("MainMenu");
        panelInventory = GetNode<Control>("MainMenu/Inventory");
        panelEquipment = GetNode<Control>("MainMenu/Equipment");
        panelSkills = GetNode<Control>("MainMenu/Skills");
        panelAssignments = GetNode<Control>("MainMenu/Assignments");
        panelProgression = GetNode<Control>("MainMenu/Progression");
        panelSettings = GetNode<Control>("MainMenu/Settings");
        
        var scrollcontainer = GetNode<ScrollContainer>("MainMenu/Assignments/ButtonContainer/ScrollContainer");
        var sb = scrollcontainer.GetVScrollBar();
        sb.CustomMinimumSize = new Vector2(20, 0); 
        sb.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        
        // var sliderbar = GetNode<HSlider>("MainMenu/Equipment/HSlider"); //delete?
        // sliderbar.CustomMinimumSize = new Vector2(200, 100);
        // sliderbar.Size = new Vector2(800, 500);
        
        allPanels = new List<Control>();
        allPanels.Add(panelInventory);
        allPanels.Add(panelEquipment);
        allPanels.Add(panelSkills);
        allPanels.Add(panelAssignments);
        allPanels.Add(panelProgression);
        allPanels.Add(panelSettings);
        CloseMainMenu();
        distillery.Visible = false; //MOVE
    }
    
    private void ShowMenuPanel(string what) {
        HideAllPanels();
        mainMenu.Visible = true;
        RemoveAllPlayerStatsNodes();
        UpdatePlayerSheet();
        
        switch (what) {
            case "inventory":
                panelInventory.Visible = true;
                RefreshInventoryPanel();
                break;
            case "equipment":
                panelEquipment.Visible = true;
                RefreshEquipmentPanel();
                break;
            case "skills":
                panelSkills.Visible = true;
                skills.RefreshSkillsPanel();
                break;
            case "assignments":
                panelAssignments.Visible = true;
                assignments.RefreshAssignmentsPanel();
                break;
            case "progression":
                panelProgression.Visible = true;
                RefreshProgressionPanel();
                break;
            case "settings":
                panelSettings.Visible = true;
                RefreshSettingsPanel();
                break;
        }
        tabInventory.TextureNormal = panelInventory.Visible ? tabTextureActive : tabTextureInactive;
        tabEquipment.TextureNormal = panelEquipment.Visible ? tabTextureActive : tabTextureInactive;
        tabSkills.TextureNormal = panelSkills.Visible ? tabTextureActive : tabTextureInactive;
        tabAssignments.TextureNormal = panelAssignments.Visible ? tabTextureActive : tabTextureInactive;
        tabProgression.TextureNormal = panelProgression.Visible ? tabTextureActive : tabTextureInactive;
        tabSettings.TextureNormal = panelSettings.Visible ? tabTextureActive : tabTextureInactive;

        tabInventory.Position = tabInventory.TextureNormal == tabTextureActive ? new Vector2(tabInventory.Position.X, 38) : new Vector2(tabInventory.Position.X, 46);
        tabEquipment.Position = tabEquipment.TextureNormal == tabTextureActive ? new Vector2(tabEquipment.Position.X, 38) : new Vector2(tabEquipment.Position.X, 46);
        tabSkills.Position = tabSkills.TextureNormal == tabTextureActive ? new Vector2(tabSkills.Position.X, 38) : new Vector2(tabSkills.Position.X, 46);
        tabAssignments.Position = tabAssignments.TextureNormal == tabTextureActive ? new Vector2(tabAssignments.Position.X, 38) : new Vector2(tabAssignments.Position.X, 46);
        tabProgression.Position = tabProgression.TextureNormal == tabTextureActive ? new Vector2(tabProgression.Position.X, 38) : new Vector2(tabProgression.Position.X, 46);
        tabSettings.Position = tabSettings.TextureNormal == tabTextureActive ? new Vector2(tabSettings.Position.X, 38) : new Vector2(tabSettings.Position.X, 46);
        
        previousPanel = what;
        GetTree().Paused = true;
    }

    private void HideAllPanels() {
        foreach (var panel in allPanels) { panel.Visible = false; }
    }

    private void RemoveAllPlayerStatsNodes() {
        if (inventoryPlayerStatsContainer.HasNode("PlayerStats")) { inventoryPlayerStatsContainer.RemoveChild(playerStats); }
        if (equipmentPlayerStatsContainer.HasNode("PlayerStats")) { equipmentPlayerStatsContainer.RemoveChild(playerStats); }
        if (inventoryInventoryExtractsContainer.HasNode("InventoryExtracts")) { inventoryInventoryExtractsContainer.RemoveChild(inventoryExtracts); }
    }
    
    private void RefreshInventoryPanel() {
        if (!inventoryPlayerStatsContainer.HasNode("PlayerStats")) { inventoryPlayerStatsContainer.AddChild(playerStats); }
        if (!inventoryInventoryExtractsContainer.HasNode("InventoryExtracts")) { inventoryInventoryExtractsContainer.AddChild(inventoryExtracts); }

        UpdatePlayerSheet();
        AddNewExtractSlots();
        RefreshExtractsInventoryDisplay();
        RefreshExtractDisplay();
        RefreshMaterialsDisplay();
    }

    private void RemoveAllInventoryNodes() {
        
    }
    
    private void RefreshEquipmentPanel() {
        if (!equipmentPlayerStatsContainer.HasNode("PlayerStats")) { equipmentPlayerStatsContainer.AddChild(playerStats); }
        SetWeaponLabels();
    }

    private void RefreshProgressionPanel() { }
    private void RefreshSettingsPanel() { }

    
    public void ToggleDistillery() {
        distillery.Visible = !distillery.Visible;
        if (inventoryInventoryExtractsContainer.HasNode("InventoryExtracts")) { inventoryInventoryExtractsContainer.RemoveChild(inventoryExtracts); } //find better place
        if (distillery.Visible) {
            distillery.DistilleryOpened(inventoryExtracts); //rename
            RefreshExtractsInventoryDisplay();
            RefreshExtractDisplay();
            GetTree().Paused = true;
        }
        else {
            distillery.DistilleryClosed(inventoryExtracts); //refactor
            GetTree().Paused = false;
        }
    
        // if (!inventoryInventoryExtractsContainer.HasNode("InventoryExtracts")) { inventoryInventoryExtractsContainer.AddChild(inventoryExtracts); }
        // GetTree().Paused = false; //no
    }
    
    private void HandleSucroseChanged() {
        // if (!Visible) { return; }
        currentSucroseLabel.Text = TrimNumber(vars.CurrentSucrose);
    }
    
    public void UpdatePlayerSheet() {
        if (!mainMenu.Visible) { return; }
        var colorGreen = tooltips.GetStringColor("teal"); //rename color green
        playerLevelLabel.Text = "KANE - LEVEL " + vars.PlayerLevel;
        statLabelDamage.Text = $"Damage: {colorGreen}{main.GetPlayerFinalDamage()}";
        statLabelPierce.Text = $"Pierce: {colorGreen}{main.GetPlayerFinalPierce()}";
        statLabelCrit.Text = $"Crit Chance: {colorGreen}{main.GetPlayerFinalCritChance()}%";
        statLabelCritDamage.Text = $"Crit Damage: {colorGreen}{main.GetPlayerFinalCritDamage()}%";
        statLabelShield.Text = $"Shield: {colorGreen}{main.GetPlayerFinalShield()}";
        statLabelShieldRegen.Text = $"Shield Regen: {colorGreen}{main.GetPlayerFinalShieldRegen()}/s";
        statLabelHealth.Text = $"Max Health: {colorGreen}{main.GetPlayerMaxHealth()}";
        statLabelRange.Text = $"Pickup Range: {colorGreen}{main.GetPlayerFinalPickupRange()}";
        statLabelSpeed.Text = $"Move Speed: {colorGreen}{(main.GetPlayerFinalSpeed() / 100)}"; // m/s
        // statLabelSpeed.Text = $"Move Speed: {colorGreen}{(main.GetPlayerFinalSpeed())}"; // m/s
        statLabelExtractDrop.Text = $"Extract Drop: {colorGreen}{main.GetPlayerFinalExtractDropChanceDisplay().ToString("F1")}%";
        statLabelSucroseDrop.Text = $"Sucrose Drop: {colorGreen}{main.GetPlayerFinalSucroseDrop()}%";
        statLabelExpDrop.Text = $"Exp Gain: {colorGreen}{main.GetPlayerFinalExpDrop()}%";
        
        // statLabelSlow.Text = $"Slow: {colorGreen}{main.GetPlayerChillAmount() * 100}%"; //not done
        statLabelMelt.Text = $"Melt: {colorGreen}{main.GetPlayerMeltDamage()}/s";
        statLabelSlowResist.Text = $"Slow Resist: {colorGreen}{main.GetPlayerChillResist()}%";
        statLabelMeltResist.Text = $"Melt Resist: {colorGreen}{main.GetPlayerMeltResist()}%";
        
        // statLabelDamage.Text = $"Damage: {colorGreen}[outline_size={{6}}]{main.GetPlayerFinalDamage()}";
    }
    
    // private void GetWeaponButtons() { //if using selectable slots at bottom of hud
    //     allWeaponSlots = new List<TextureButton>();
    //     allWeaponSlots.Add(GetNode<TextureButton>("WorldHud/WeaponPanel/HBoxContainer/SlotOne"));
    //     allWeaponSlots.Add(GetNode<TextureButton>("WorldHud/WeaponPanel/HBoxContainer/SlotTwo"));
    //     allWeaponSlots.Add(GetNode<TextureButton>("WorldHud/WeaponPanel/HBoxContainer/SlotThree"));
    //     allWeaponSlots.Add(GetNode<TextureButton>("WorldHud/WeaponPanel/HBoxContainer/SlotFour"));
    //     allWeaponSlots.Add(GetNode<TextureButton>("WorldHud/WeaponPanel/HBoxContainer/SlotFive"));
    //     allWeaponSlots.Add(GetNode<TextureButton>("WorldHud/WeaponPanel/HBoxContainer/SlotSix"));
    // }
    //
    // public void UpdateGameHud() { //if using selectable slots at bottom of hud
    //     var currentWeaponData = main.GetCurrentWeaponData();
    //     currentWeaponlabel.Text = currentWeaponData.WeaponName;
    //     if (!currentWeaponData.FireMode.Equals(vars.CurrentFireMode)) { return; }
    //     
    //     //clear all slots
    //     foreach (var slot in allWeaponSlots) { slot.TextureNormal = weaponBoxTextureNormal; }
    //     var currentSlot = allWeaponSlots[currentWeaponData.FireMode];
    //     currentSlot.TextureNormal = weaponBoxTextureActive;
    // }

    private void GetRadialButtons() {
        radialOneTextureNormal = GD.Load<Texture2D>("res://assets/radial-one.png");
        radialOneTextureActive = GD.Load<Texture2D>("res://assets/radial-one-active.png");
        radialTwoTextureNormal = GD.Load<Texture2D>("res://assets/radial-two.png");
        radialTwoTextureActive = GD.Load<Texture2D>("res://assets/radial-two-active.png");
        radialThreeTextureNormal = GD.Load<Texture2D>("res://assets/radial-three.png");
        radialThreeTextureActive = GD.Load<Texture2D>("res://assets/radial-three-active.png");
        radialFourTextureNormal = GD.Load<Texture2D>("res://assets/radial-four.png");
        radialFourTextureActive = GD.Load<Texture2D>("res://assets/radial-four-active.png");
        radialFiveTextureNormal = GD.Load<Texture2D>("res://assets/radial-five.png");
        radialFiveTextureActive = GD.Load<Texture2D>("res://assets/radial-five-active.png");
        radialSixTextureNormal = GD.Load<Texture2D>("res://assets/radial-six.png");
        radialSixTextureActive = GD.Load<Texture2D>("res://assets/radial-six-active.png");
        radialOneButton = GetNode<TextureButton>("RadialMenu/RadialOne");
        radialTwoButton = GetNode<TextureButton>("RadialMenu/RadialTwo");
        radialThreeButton = GetNode<TextureButton>("RadialMenu/RadialThree");
        radialFourButton = GetNode<TextureButton>("RadialMenu/RadialFour");
        radialFiveButton = GetNode<TextureButton>("RadialMenu/RadialFive");
        radialSixButton = GetNode<TextureButton>("RadialMenu/RadialSix");
        radialTwoButtonBullet = radialTwoButton.GetNode<TextureRect>("BulletTexture");
        radialThreeButtonBullet = radialThreeButton.GetNode<TextureRect>("BulletTexture");
        radialFourButtonBullet = radialFourButton.GetNode<TextureRect>("BulletTexture");
        radialFiveButtonBullet = radialFiveButton.GetNode<TextureRect>("BulletTexture");
        radialSixButtonBullet = radialSixButton.GetNode<TextureRect>("BulletTexture");
        CheckRadialButtons();
    }

    public void UpdateRadialButtons() {
        var currentWeaponData = main.GetCurrentWeaponData();
        // currentWeaponlabel.Text = currentWeaponData.WeaponName; //use this
        if (!currentWeaponData.FireMode.Equals(vars.CurrentFireMode)) { return; }

        radialOneButton.TextureNormal = radialOneTextureNormal;
        radialTwoButton.TextureNormal = radialTwoTextureNormal;
        radialThreeButton.TextureNormal = radialThreeTextureNormal;
        radialFourButton.TextureNormal = radialFourTextureNormal;
        radialFiveButton.TextureNormal = radialFiveTextureNormal;
        radialSixButton.TextureNormal = radialSixTextureNormal;

        if (vars.CurrentFireMode == 0) { radialOneButton.TextureNormal = radialOneTextureActive; }
        else if (vars.CurrentFireMode == 1) { radialTwoButton.TextureNormal = radialTwoTextureActive; }
        else if (vars.CurrentFireMode == 2) { radialThreeButton.TextureNormal = radialThreeTextureActive; }
        else if (vars.CurrentFireMode == 3) { radialFourButton.TextureNormal = radialFourTextureActive; }
        else if (vars.CurrentFireMode == 4) { radialFiveButton.TextureNormal = radialFiveTextureActive; }
        else if (vars.CurrentFireMode == 5) { radialSixButton.TextureNormal = radialSixTextureActive; }
    }

    private void CheckRadialButtons() {
        radialTwoButtonBullet.Visible = vars.SoftenerOwned;
        radialTwoButton.Disabled = !vars.SoftenerOwned;
        radialTwoButton.Modulate = vars.SoftenerOwned ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, .3f);
        radialTwoButton.MouseFilter = radialTwoButton.Disabled ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Pass;
        
        radialThreeButtonBullet.Visible = vars.SpreaderOwned;
        radialThreeButton.Disabled = !vars.SpreaderOwned;
        radialThreeButton.Modulate = vars.SpreaderOwned ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, .3f);
        radialThreeButton.MouseFilter = radialThreeButton.Disabled ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Pass;
        
        radialFourButtonBullet.Visible = vars.SniperOwned;
        radialFourButton.Disabled = !vars.SniperOwned;
        radialFourButton.Modulate = vars.SniperOwned ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, .3f);
        radialFourButton.MouseFilter = radialFourButton.Disabled ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Pass;
        
        radialFiveButtonBullet.Visible = vars.SlowerOwned;
        radialFiveButton.Disabled = !vars.SlowerOwned;
        radialFiveButton.Modulate = vars.SlowerOwned ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, .3f);
        radialFiveButton.MouseFilter = radialFiveButton.Disabled ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Pass;
        
        radialSixButtonBullet.Visible = vars.SmasherOwned;
        radialSixButton.Disabled = !vars.SmasherOwned;
        radialSixButton.Modulate = vars.SmasherOwned ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, .3f);
        radialSixButton.MouseFilter = radialSixButton.Disabled ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Pass;
    }

    public void UpdateHealthBar(float current) {
        // playerHealthBar.Value = current * 100; //dont use
        // playerHealthBar.MaxValue = main.GetPlayerMaxHealth() * 100;
        playerHealthBar.Value = vars.CurrentPlayerHealth;
        playerHealthBar.MaxValue = main.GetPlayerMaxHealth();
        UpdateHealthShieldBarText();
        // playerHealthBarLabel.Text = "Health: " + playerHealthBar.Value;
        // playerHealthBarLabel.Visible = playerShieldBar.Value <= 0;
    }
    
    public void UpdateShieldBar(float current) {
        playerShieldBar.Value = current * 100;
        playerShieldBar.MaxValue = main.GetPlayerFinalShield() * 100;
        UpdateHealthShieldBarText();
        // playerShieldBarLabel.Text = "Shield: " + playerShieldBar.Value;
        // playerShieldBarLabel.Text = "Shield: " + playerShieldBar.Value + " | Health: " + playerHealthBar.Value;
        // playerShieldBarLabel.Visible = playerShieldBar.Value > 0;
        // playerHealthBarLabel.Visible = playerShieldBar.Value <= 0;
    }

    private void UpdateHealthShieldBarText() {
        // playerShieldBarLabel.Text = "Shield " + (int)playerShieldBar.Value / 100 + " | Health " + (int)playerHealthBar.Value / 100;
        playerShieldBarLabel.Text = "Shield " + (int)playerShieldBar.Value / 100 + " | Health " + vars.CurrentPlayerHealth;
    }
    
    public void UpdateReloadBar(float current, float max) {
        if (current > -1) { playerReloadBar.Value = current * 100; }
        if (max > -1) { playerReloadBar.MaxValue = max * 100; }
        float timeRemaining = max - current; //time remaining in seconds (unscaled)
        timeRemaining = Mathf.Max(0f, timeRemaining); //clamp to 0 to prevent negative display when it's fully reloaded or over
        // playerReloadLabel.Text = $"{timeRemaining:0.0}"; //format and display. divide by 100f if playerReloadBar.Value is scaled
        
        //check if the weapon is ready (timeRemaining is very close to zero)
        playerReloadLabel.Text = timeRemaining <= 0.05f ? "Ready" : $"{timeRemaining:0.0}";
        
        // UpdateWeaponSlotBar();
    }
    
    // private void UpdateWeaponSlotBar() {
    //     var bartest = GetNode<ProgressBar>("WorldHud/WeaponPanel/HBoxContainer/SlotOne/ProgressBar");
    //     bartest.Value++;
    //     if (bartest.Value >= 100) { bartest.Value = 0; }
    // }
    
    public void UpdateExpBar() {
        var next = main.GetPlayerExpNext();
        playerExpBar.MaxValue = next;
        playerExpBar.Value = vars.CurrentPlayerExp;
        playerExpLabel.Text = "EXP " + vars.CurrentPlayerExp + "/" + next;
    }

    //player
    private void BuildEquippedExtractSlots() {
        allEquippedExtractSlots = new List<ItemSlot>();
        var container = GetNode<GridContainer>("MainMenu/Inventory/Equipped/Slots");
        for (int i = 0; i < GetUnlockedExtractSlots(); i++) {
            ItemSlot slot = itemSlotScene.Instantiate<ItemSlot>();
            slot.EquippedExtractClicked += UnequipExtract;
            container.AddChild(slot);
            allEquippedExtractSlots.Add(slot);
            slot.Reset();
        }
    }

    private void AddNewExtractSlots() {
        var container = GetNode<GridContainer>("MainMenu/Inventory/Equipped/Slots");
        var currentSlots = allEquippedExtractSlots.Count;
        if (currentSlots >= GetUnlockedExtractSlots()) { return; }
        var slotsToAdd = GetUnlockedExtractSlots() - currentSlots;
        
        for (int i = 0; i < slotsToAdd; i++) {
            ItemSlot slot = itemSlotScene.Instantiate<ItemSlot>();
            slot.EquippedExtractClicked += UnequipExtract;
            container.AddChild(slot);
            allEquippedExtractSlots.Add(slot);
            slot.Reset();
        }
    }
    
    // public int GetExtractStatValue(string stat) { //moved to main -delete
    //     var value = 0;
    //     foreach (var extract in allEquippedExtractSlots) {
    //         if (!extract.GetFull()) { continue; }
    //
    //         switch (stat) {
    //             case "Damage": value += extract.GetExtract().ExtractBaseDamage; break;
    //             case "Pierce": value += extract.GetExtract().ExtractBasePierce; break;
    //             case "Crit Chance": value += extract.GetExtract().ExtractBaseCritChance; break;
    //             case "Crit Damage": value += extract.GetExtract().ExtractBaseCritDamage; break;
    //             case "Shield": value += extract.GetExtract().ExtractBaseShield; break;
    //             case "Shield Regen": value += extract.GetExtract().ExtractBaseShieldRegen; break;
    //             case "Health": value += extract.GetExtract().ExtractBaseHealth; break;
    //             case "Pickup Range": value += extract.GetExtract().ExtractBasePickupRange; break;
    //             case "Speed": value += extract.GetExtract().ExtractBaseSpeed; break;
    //             case "Extract Drop": value += extract.GetExtract().ExtractBaseExtractDrop; break;
    //             case "Sucrose Drop": value += extract.GetExtract().ExtractBaseSucroseDrop; break;
    //             case "Exp Drop": value += extract.GetExtract().ExtractBaseExpGain; break;
    //         }
    //     }
    //     return value;
    // }
    
    private int GetUnlockedExtractSlots() {
        var slots = (int)Math.Ceiling((double)vars.PlayerLevel / 5);
        if (slots > 20) { slots = 20; }
        return slots;
    }
    
    private bool EquipExtract(BaseExtract item) {
        //check if all slots filled
        var totalFilled = 0;
        foreach (var slot in allEquippedExtractSlots) { if (slot.GetFull()) { totalFilled += 1; } }
        if (totalFilled >= GetUnlockedExtractSlots()) { return false; }
        
        equippedExtractsData.Add(item); //add to the single source of truth list
        item.Equipped = true;
        RefreshExtractDisplay();
        return true;
    }
    
    private void UnequipExtract(ItemSlot slot, BaseExtract extract) {
        if (GetExtractsInventoryFull()) { return; } //add some notification about inventory full        
        if (equippedExtractsData.Remove(extract)) { extract.Equipped = false; }
        AddExtractToInventory(extract);
        RefreshExtractDisplay();
    }
    
    private void RefreshExtractDisplay() {
        foreach (var slot in allEquippedExtractSlots) { slot.Reset(); } //clear all visual slots

        //repopulate slots with current mod items
        for (int i = 0; i < equippedExtractsData.Count; i++) {
            if (i < allEquippedExtractSlots.Count) {
                // GD.Print($"  Displaying '{_equippedModsData[i].Name}' in visual slot {i}.");
                allEquippedExtractSlots[i].DisplayExtract(equippedExtractsData[i]);
            } else {
                GD.PushWarning($"Not enough visual slots to display all items. Item '{equippedExtractsData[i].Name}' skipped.");
            }
        }
        UpdatePlayerSheet();
    }
    
    private string BuildWeaponStatusEffectsString(string bulletType) { //MOVE
        // var knockbackDistance = main.GetPlayerFinalKnockback();
        // var meltDuration = main.GetPlayerMeltDuration();
        // var chillDuration = main.GetPlayerChillDuration();
        
        var knockbackDistance = main.GetSkillKnockbackEffect(bulletType);
        var meltDuration = main.GetSkillMeltEffect(bulletType);
        var chillDuration = main.GetSkillChillEffect(bulletType);

        var effectsStringBuilder = new StringBuilder();

        if (knockbackDistance > 0) {
            effectsStringBuilder.Append("Knockback");
        }

        if (meltDuration > 0) {
            if (effectsStringBuilder.Length > 0) { effectsStringBuilder.Append(", "); } //if there's already an effect, add a comma and space first
            effectsStringBuilder.Append("Melt");
        }

        if (chillDuration > 0) {
            if (effectsStringBuilder.Length > 0) { effectsStringBuilder.Append(", "); }
            effectsStringBuilder.Append("Chill");
        }
        
        return effectsStringBuilder.ToString();
    }
    
    //equipment
    private void SetWeaponLabels() {
        var allWeaponNames = new List<string> { "Sprayer", "Softener", "Spreader", "Sniper", "Slower", "Smasher" };
        const string prefix = "weaponButton"; 
    
        //all the stats and their formatting rules
        var statDefinitions = new (string name, string suffix, string format)[] {
            // Stats with breakdown (Base + Bonus)
            ("Damage", "", ""), 
            ("Pierce", "", "F0"),
            ("Crit", "%", "F0"),
            ("CritDamage", "%", "F0"),
            // Simple stats (Final Value Only)
            ("Reload", " sec", "F1"),
            ("Speed", "m/s", "F0"),
            ("Range", "m", "F0")
        };
        
        foreach (var weaponButton in allGearButtons) {
            if (weaponButton == null || string.IsNullOrEmpty(weaponButton.Name)) continue;
    
            string fullButtonName = weaponButton.Name;
            string weaponName = fullButtonName.StartsWith(prefix) ? fullButtonName.Substring(prefix.Length) : fullButtonName;
            
            if (!allWeaponNames.Contains(weaponName)) continue;
            
            foreach (var statDef in statDefinitions) {
                string statName = statDef.name;
                string suffix = statDef.suffix;
                string format = statDef.format;
                
                var statLabel = weaponButton.GetNodeOrNull<RichTextLabel>($"VBoxContainer/{statName}/RichTextLabel");
                if (statLabel == null) { continue; }
                
                var statData = WeaponStatHelper($"{weaponName}{statName}")[0]; 
                double baseStat = statData.baseStat;
                double skillBonus = statData.skillBonusAmount;
                double extractBonus = statData.extractAmount;
                
                const string spacePattern = "([a-z])([A-Z])"; //dynamic pattern to insert a space before a capital letter
                string displayName = statName;
                if (statName.Contains("Crit")) {
                    displayName = Regex.Replace(statName, spacePattern, "$1 $2"); //insert a space if a lowercase is followed by an uppercase
                }
    
                var finalAmount = baseStat + skillBonus + extractBonus;
                
                //display the stats
                var statColor = tooltips.GetStringColor("tealtwo");
                if (skillBonus > 0) { statColor = tooltips.GetStringColor("green"); }
    
                if (statName != "Reload") {
                    finalAmount = Math.Ceiling(finalAmount);
                }
    
                if (statName == "Reload") {
                    string finalTimeDisplay = baseStat.ToString(format);
                    statLabel.Text = $"{displayName}: {statColor}{finalTimeDisplay}{suffix}[/color]";
                }
                else { //all non-special case stats
                    string formattedValue = finalAmount.ToString(format);
                    statLabel.Text = $"{statName}: {statColor}{formattedValue}{suffix}[/color]";
                }
            }
            
            //effects from skills
            var effectsLabel = weaponButton.GetNodeOrNull<Label>("Effects");
            if (effectsLabel != null) { effectsLabel.Text = BuildWeaponStatusEffectsString($"bullet{weaponName}"); }
        }
        SetEquippedWeaponPanel();
    }

    private List<(double baseStat, double skillBonusAmount, double extractAmount)> WeaponStatHelper(string lookupString) {
        var allWeaponData = main.GetAllWeaponData();
        var weaponTypes = new List<(string name, int index)>() { ("Sprayer", 0), ("Softener", 1), ("Spreader", 2), ("Sniper", 3), ("Slower", 4), ("Smasher", 5) };
        string pattern = "(?<=[a-z])(?=[A-Z])";
        string[] parts = Regex.Split(lookupString, pattern);
        var weaponType = parts[0];
        var statName = parts[1];
        int weaponIndex = -1;
        foreach (var weaponTypeTuple in weaponTypes) {
            if (weaponTypeTuple.name == weaponType) { //case-sensitive match
                weaponIndex = weaponTypeTuple.index;
                break;
            }
        }

        if (weaponIndex == -1) { return null; }

        var selectedWeaponData = allWeaponData[weaponIndex];
        double baseStat = 0;
        // double skillBonusAmount = 0; //delete

        string weaponId = selectedWeaponData.Id;
        
        switch (statName) {
            case "Damage":
                string skillId = $"{weaponType}Damage";
                int skillPoints = main.GetSkillLevelForWeapon(weaponType, statName);
                float damageBonusPercent = main.GetSkillPointsEffect(skillId, skillPoints);
                double totalUnboostedDamage = selectedWeaponData.Damage + main.GetPlayerLevelDamage(); // + main.GetPlayerModDamage() not used here
                var skillBonusAmount = totalUnboostedDamage * damageBonusPercent; // e.g., 55 * 0.5 = 27.5
                return [(totalUnboostedDamage, skillBonusAmount, main.GetPlayerExtractDamage())];
            
            case "Reload":
                double baseReloadTime = selectedWeaponData.ReloadSpeed;
                double reductionPercent = main.GetSkillReloadEffect(selectedWeaponData.Id); //get the percentage reduction from the skill (e.g., 0.5)
                double timeSaved = baseReloadTime * reductionPercent; // calculate the actual Time Saved (e.g., 2s * 0.5 = 1s saved)
                double finalReloadTime = baseReloadTime - timeSaved;
                return [(finalReloadTime, timeSaved, 0)];
            case "Speed":
                baseStat = selectedWeaponData.Speed;
                double speedBonusAmount = 0; 
                return [(baseStat, speedBonusAmount, 0)];
            case "Range":
                baseStat = selectedWeaponData.Range;
                double rangeBonusAmount = 0;
                return [(baseStat, rangeBonusAmount, 0)];
            case "Pierce":
                baseStat = selectedWeaponData.Pierce;
                double pierceBonusAmount = main.GetSkillPierceEffect(weaponId); //additive number of pierce points
                return [(baseStat, pierceBonusAmount, main.GetPlayerExtractPierce())];
            case "Crit":
                baseStat = selectedWeaponData.Crit;
                double critBonusAmount = main.GetSkillCritChanceEffect(weaponId); //additive percentage point increase (e.g., 5.0)
                return [(baseStat, critBonusAmount, main.GetPlayerExtractCritChance())];
            case "CritDamage":
                baseStat = selectedWeaponData.CritDamage;
                double critDamageBonusAmount = 0; 
                return [(baseStat, critDamageBonusAmount, main.GetPlayerExtractCritDamage())];

            default:
                return [(0, 0, 0)];
        }
    }

    private void ShowWeaponStatTooltip(string what) {
        foreach (var allWeaponStatTooltip in allWeaponStatTooltips) {
            if (what.Equals(allWeaponStatTooltip.lookupString)) {
                
                var returnedData = WeaponStatHelper(allWeaponStatTooltip.lookupString)[0];
                double baseStat = returnedData.baseStat;   // Base Stat (e.g., Weapon Damage, Base Reload Time)
                double skillBonus = returnedData.skillBonusAmount; // Skill Bonus Amount (e.g., +25 damage, -1.0 sec)
                double extractBonus = returnedData.extractAmount;
                
                var theTooltip = allWeaponStatTooltip.tooltip;
                var baseLabel = theTooltip.GetNode<Label>("VBoxContainer/Base");
                var skillLabel = theTooltip.GetNode<Label>("VBoxContainer/PlayerOrWeapon");
                var extractsLabel = theTooltip.GetNode<Label>("VBoxContainer/Extracts");
                
                // --- Determine the Stat Type ---
                bool isTimeReduction = what.EndsWith("Reload");
                
                // --- 1. Base Weapon Stat Display ---
                string baseFormat = isTimeReduction ? "F2" : "N0"; // Use F2 for time clarity
                // string baseSuffix = isTimeReduction ? " sec" : ""; // Add "sec" for time stats -not used

                // --- FIX: Remove the '+' sign and add the suffix ---
                // baseLabel.Text = $"{baseStat.ToString(baseFormat)}{baseSuffix} Base"; //suffix not used
                baseLabel.Text = $"{baseStat.ToString(baseFormat)} Base";
                baseLabel.Visible = true;

                // --- 2. Skill Bonus Stat Display ---
                string bonusText;

                if (isTimeReduction) {
                    // double originalBaseTime = main.GetWeaponBaseStat(what);
                    var restoredBaseStat = baseStat + skillBonus;
                    // baseLabel.Text = $"{restoredBaseStat.ToString(baseFormat)}{baseSuffix} Base";
                    baseLabel.Text = $"{restoredBaseStat.ToString(baseFormat)} Base";
                    // baseLabel.Text = $"{baseStat.ToString(baseFormat)}{baseSuffix} Base"; //original
                    
                    // For RELOAD: Show negative sign and F2 format
                    // bonusText = $"-{skillBonus.ToString("F2")}{baseSuffix}"; // e.g., -1.00 sec
                    bonusText = $"-{skillBonus.ToString("F2")}"; // e.g., -1.00 sec
                } else {
                    // For DAMAGE/PIERCE/CRIT: Show positive sign and FormatStatValue
                    bonusText = $"+{FormatStatValue(Math.Ceiling(skillBonus))}";
                }

                // Apply the formatted text to the skill and extract label
                skillLabel.Text = $"{bonusText} Skills";
                skillLabel.Visible = !(skillBonus <= 0);

                extractsLabel.Text = $"+{extractBonus} Extracts";
                extractsLabel.Visible = !(extractBonus <= 0);
            
                theTooltip.Show();
            }
        }
    }
    
    private void MouseEnteredRadial(int what) {
        GetNode<Label>("RadialMenu/Name").Text = main.GetAllWeaponData()[what].Name;
        main.EquipWeapon(what);
        UpdateRadialButtons();
    }

    private void MouseExitedRadial() {
        UpdateRadialButtons();
    }
    
    public void SetEquippedWeaponPanel() {
        if (!mainMenu.Visible) { return; }
        // foreach (var b in allGearButtons) { b.TextureNormal = equipmentPanelTextureNormal; } //not done

        for (int i = 0; i < allGearButtons.Count; i++) {
            // allGearButtons[i].TextureNormal = equipmentPanelTextureNormal;
            allGearButtons[i].GetNode<Label>("Name").Text = main.GetAllWeaponData()[i].Name;
        }
        allGearButtons[vars.CurrentFireMode].GetNode<Label>("Name").Text = main.GetAllWeaponData()[vars.CurrentFireMode].Name + " [E]";
    }

    private void BuildAllWeaponStatTooltipsList() {
        allWeaponStatTooltips = new List<(string stat, TextureRect tooltip, string lookupString)>();
        var weaponTypes = new List<string>() { "Sprayer", "Softener", "Spreader", "Sniper", "Slower", "Smasher" };
        var statTypes = new List<string>() { "Damage", "Reload", "Speed", "Range", "Pierce", "Crit", "CritDamage" };

        foreach (var weaponType in weaponTypes) {
            foreach (var statType in statTypes) {
                string tooltipPath = $"MainMenu/Equipment/Weapons/{weaponType}/VBoxContainer/{statType}/StatTooltip";
                var theTootip = GetNode<TextureRect>(tooltipPath);
                string lookupString = weaponType + statType;
                // allWeaponStatTooltips.Add((weaponType, theTootip));
                allWeaponStatTooltips.Add((weaponType, theTootip, lookupString));
            }
        }
        HideAllWeaponStatTooltips();
    }
    
    //this is used to only show a decimal in the tooltip if the number is below 1
    private string FormatStatValue(double value) {
        if (value < 1.0 && value > 0) {
            //if the number is a decimal (and not 0), format with one decimal place.
            return value.ToString("F1");
        } else {
            //otherwise, format as a whole number. the "N0" format specifier is a good way to do this for whole number
            return value.ToString("N0");
        }
    }
    
    private void HideAllWeaponStatTooltips() {
        foreach (var t in allWeaponStatTooltips) { t.tooltip.Hide(); }
    }
    
    //inventory
    private void BuildInventoryExtractSlots() {
        allInventoryExtractSlots = new List<ItemSlot>();
        var container = inventoryExtracts.GetNode<GridContainer>("Slots");
        for (int i = 0; i < MaxInventoryExtractSlots; i++) {
            ItemSlot slot = itemSlotScene.Instantiate<ItemSlot>();
            slot.ItemSlotClicked += HandleItemSlotClicked;
            container.AddChild(slot);
            allInventoryExtractSlots.Add(slot);
            slot.Reset();
        }
    }

    private void HandleExtractsInventoryChanged() {
        RefreshExtractsInventoryDisplay();
        vars.SaveGameData();
    }
    
    private void HandleMaterialsChanged() {
        UpdateSavedItemData();
    }
    
    private void HandleItemSlotClicked(ItemSlot slot, BaseExtract extract, BaseMaterial material) {
        if (material != null) { //for testing -delete
            // main.LoseMaterial(material, 1);
            return;
        }
        
        if (material != null) { return; } //if a material was clicked, do nothing
        
        if (distillery.Visible) {
            if (!distillery.AddExtractToRefiner(extract)) { return; }
            RemoveExtractFromInventory(extract);
            return;
        }
        
        if (!EquipExtract(extract)) { return; }
        RemoveExtractFromInventory(extract);
    }
    
    public bool AddExtractToInventory(BaseExtract item) {
        // GD.Print($"add item to inventory: Attempting to add '{item.Name}' (ID: {item.Id})");
        // GD.Print($"  playerInventoryData count BEFORE add: {playerInventoryData.Count}");

        if (inventoryExtractsData.Count >= MaxInventoryExtractSlots) { return false; } //return if inventory is full

        inventoryExtractsData.Add(item); //add to the single source of truth list
        item.InInventory = true;

        // GD.Print($"add item to inventory: '{item.Name}' added to playerInventoryData. Count now: {playerInventoryData.Count}");
        // foreach(var addedItem in playerInventoryData) { GD.Print($"  playerInventoryData contains: {addedItem.Name} (ID: {addedItem.Id})"); }

        EmitSignal(SignalName.ExtractInventoryChanged);
        return true;
    }
    
    private void RemoveExtractFromInventory(BaseExtract item) {
        if (inventoryExtractsData.Remove(item)) {
            item.InInventory = false;
            EmitSignal(SignalName.ExtractInventoryChanged);
        }
    }

    private void DeleteExtract(BaseExtract item) {
        RemoveExtractFromInventory(item);
    }
    
    private void ResetInventorySlots() { foreach (var slot in allInventoryExtractSlots) { slot.Reset(); } }

    public int GetFullExtractSlotCount() {
        var count = 0;
        foreach (var slot in allInventoryExtractSlots) {
            if (!slot.GetFull()) { continue; }
            count += 1;
        }
        return count;
    }

    private void RefreshExtractsInventoryDisplay() {
        foreach (var slot in allInventoryExtractSlots) { slot.Reset(); } //clear all visual slots

        //repopulate slots with current inventory items
        for (int i = 0; i < inventoryExtractsData.Count; i++) {
            if (i < allInventoryExtractSlots.Count) {
                // GD.Print($"  Displaying '{playerInventoryData[i].Name}' in visual slot {i}.");
                allInventoryExtractSlots[i].DisplayExtract(inventoryExtractsData[i]);
            } else {
                GD.PushWarning($"Not enough visual slots to display all items. Item '{inventoryExtractsData[i].Name}' skipped.");
            }
        }
    }

    private void SortExtractsInventory(int sortType) {
        //get the direct reference to the saved list from the singleton. this ensures we're always working with the latest saved state
        inventoryExtractsData = vars.GetSavedResourcesInstance().inventoryExtracts;
        
        switch (sortType) {
            case 0:
                break;
            case 1:
                inventoryExtractsData = new Array<BaseExtract>(inventoryExtractsData.OrderBy(item => item.Name).ThenBy(item => item.ExtractTier).ToList());
                break;
            case 2:
                inventoryExtractsData = new Array<BaseExtract>(inventoryExtractsData.OrderBy(item => item.ExtractTier).ThenBy(item => item.Name).ToList());
                break;
            case 3:
                inventoryExtractsData = new Array<BaseExtract>(inventoryExtractsData.OrderBy(item => item.ExtractQuality).ThenBy(item => item.Name).ToList());
                break;
        }
        EmitSignal(SignalName.ExtractInventoryChanged);
    }

    public bool GetExtractsInventoryFull() { return GetFullExtractSlotCount() >= MaxInventoryExtractSlots; }
    
    //materials
    private void BuildMaterialsSlots() {
        allMaterialsSlots = new List<ItemSlot>();
        var container = GetNode<GridContainer>("MainMenu/Inventory/Resources/Slots");
        for (int i = 0; i < 20; i++) {
            ItemSlot slot = itemSlotScene.Instantiate<ItemSlot>();
            slot.ItemSlotClicked += HandleItemSlotClicked;
            container.AddChild(slot);
            allMaterialsSlots.Add(slot);
            slot.Reset();
        }
    }
    
    //this called by RefreshMaterialsDisplay for each material in allMaterials. it determines if a material needs its slot updated, or if it needs a new slot
    public void ProcessMaterialForDisplay(BaseMaterial materialToProcess) {
        //find if this material type is currently being displayed in any slot
        ItemSlot existingDisplaySlot = null;
        foreach (var slot in allMaterialsSlots) {
            //check if the slot is occupied AND if it displays the same material type
            if (slot.GetFull() && slot.GetInventoryMaterial().Id == materialToProcess.Id) {
                existingDisplaySlot = slot; //found the slot that should be displaying this material
                break;
            }
        }

        if (existingDisplaySlot != null) {
            //if it's already displayed, just update its content.
            // DisplayMaterial will correctly hide/show labels/textures based on CurrentOwned.
            existingDisplaySlot.DisplayMaterial(materialToProcess);
        } else {
            // If the material is NOT currently displayed, but it *should* be (i.e., its count is > 0)
            if (materialToProcess.CurrentOwned > 0) {
                // Find the first empty slot to display this "newly appearing" material in
                ItemSlot emptySlot = null;
                foreach (var slot in allMaterialsSlots) {
                    if (!slot.GetFull()) { // GetFull() now checks _isFull in ItemSlot.cs
                        emptySlot = slot;
                        break;
                    }
                }
                if (emptySlot != null) {
                    materialToProcess.InInventory = true; // Set flag (as per original logic)
                    emptySlot.DisplayMaterial(materialToProcess); // Display it in the found empty slot
                } else {
                    GD.PushWarning($"No empty display slot found for material: {materialToProcess.Name} (ID: {materialToProcess.Id}).");
                }
            } else {
                // Material has 0 count and is not currently displayed, so do nothing.
                // Ensure InInventory flag is false for materials not currently displayed and not owned.
                materialToProcess.InInventory = false; 
            }
        }
    }

    public void RefreshMaterialsDisplay() {
        // This loop iterates through all potential materials and processes them for display.
        // It relies on ProcessMaterialForDisplay and ItemSlot.DisplayMaterial
        // to correctly show/hide based on CurrentOwned counts.
        foreach (var material in allMaterials) { 
             ProcessMaterialForDisplay(material);
        }
    }

    public BaseMaterial GetMaterialById(string materialId) {
        foreach (var material in allMaterials) { 
            if (!materialId.Equals(material.Id)) { continue; }
            return material;
        }
        return null;
    }
    
    public double GetOwnedMaterialAmount(string materialId) {
        double amount = 0;
        foreach (var material in allMaterials) { 
            // GD.Print($"{material.Name}: {material.CurrentOwned}");
            if (!materialId.Equals(material.Id)) { continue; }
            amount = material.CurrentOwned;
        }
        return amount;
    }
    
    private void UpdateSavedItemData() {
        savedItemDataArray.Clear();
        savedItemDataArray = new Array<SavedItemData>();
		  
        var updateList = new List<BaseMaterial>();
		  
        foreach (var material in allMaterials) {
            updateList.Add(material);
        }
		  
        foreach (var material in updateList) {
            savedItemDataArray.Add(new SavedItemData(material.Id, material.CurrentOwned, material.TotalFound)); //updates every material regardless of if it changed or not
            // GD.Print("updated: " + material.Id + " inv: " + material.CurrentOwned);
        }
        vars.SavedItemDataArray = savedItemDataArray;
        vars.SaveGameData();
    }
	   
    private void LoadSavedItemData() {
        var loadedItemData = vars.SavedItemDataArray;
        var loadList = new List<BaseMaterial>();
    
        foreach (var material in allMaterials) { loadList.Add(material); }
    
        foreach (var loadedItem in loadedItemData) {
            foreach (var material in loadList) {
                if (loadedItem.ItemId == material.Id) {
                    material.CurrentOwned = loadedItem.CurrentOwned;
                    // GD.Print("loaded: " + material.Id + " inv: " + material.CurrentOwned);
                }
            }
        }
    }
    
    //input
    public override void _UnhandledInput(InputEvent @event) {
        // if (vars.CutsceneActive) { return; } //delete -not used
        
        if (Input.IsActionJustPressed("toggle_main_menu")) {
            if (distillery.Visible) { ToggleDistillery(); }
            else if (mainMenu.Visible) { CloseMainMenu(); }
            else { OpenMainMenu(); }
            GetTree().GetRoot().SetInputAsHandled();
        }
        
        if (Input.IsActionJustPressed("toggle_panel_inventory")) {
            if (previousPanel == "inventory" && mainMenu.Visible) { CloseMainMenu(); }
            else { ShowMenuPanel("inventory"); }
            GetTree().GetRoot().SetInputAsHandled();
        }
        
        if (Input.IsActionJustPressed("toggle_panel_equipment")) {
            if (previousPanel == "equipment" && mainMenu.Visible) { CloseMainMenu(); }
            else { ShowMenuPanel("equipment"); }
            GetTree().GetRoot().SetInputAsHandled();
        }
        
        if (Input.IsActionJustPressed("toggle_panel_skills")) {
            if (previousPanel == "skills" && mainMenu.Visible) { CloseMainMenu(); }
            else { ShowMenuPanel("skills"); }
            GetTree().GetRoot().SetInputAsHandled();
        }
        
        if (Input.IsActionJustPressed("toggle_panel_assignments")) {
            if (previousPanel == "assignments" && mainMenu.Visible) { CloseMainMenu(); }
            else { ShowMenuPanel("assignments"); }
            GetTree().GetRoot().SetInputAsHandled();
        }
        
        if (Input.IsActionJustPressed("toggle_panel_progression")) {
            if (previousPanel == "progression" && mainMenu.Visible) { CloseMainMenu(); }
            else { ShowMenuPanel("progression"); }
            GetTree().GetRoot().SetInputAsHandled();
        }
        
        if (Input.IsActionJustPressed("toggle_world_map")) {
            ToggleWorldMap();
            GetTree().GetRoot().SetInputAsHandled();
        }
    }

    public void GainNewBullet(int type) {
        switch (type) {
            case 1: vars.SoftenerOwned = true; break;
            case 2: vars.SpreaderOwned = true; break;
            case 3: vars.SniperOwned = true; break;
            case 4: vars.SlowerOwned = true; break;
            case 5: vars.SmasherOwned = true; break;
        }
        vars.SaveGameData();
        ShowPopupBox(false, type, null);
        CheckRadialButtons();
    }

    public void ShowPopupBox(bool tutorial, int bulletType, AssignmentData assignmentData) {
        // GetTree().Paused = true; //prev
        if (!GetTree().IsPaused()) { GetTree().Paused = true; } //only pause if not paused by another popup
        PopupBox popup = popupBoxScene.Instantiate<PopupBox>();
        AddChild(popup);
        
        if (bulletType > -1) {
            WeaponData weaponData = main.GetAllWeaponData()[bulletType];
            popup.HeaderText("Obtained New Bullet Type");
            popup.SubHeaderText(weaponData.Name.ToUpper());
            popup.Description(weaponData.Description, 24);
            popup.ToggleButtons("bullet");
        }

        if (assignmentData != null) {
            if (assignmentData.Complete) {
                popup.HeaderText("COMPLETED ASSIGNMENT");
                popup.SubHeaderText(assignmentData.Name);
                if (assignmentData.RewardString != "") {
                    popup.Description($"Obtained {assignmentData.RewardString}" +
                                      $"\n+{assignmentData.PointReward} Skill Points", 32);
                }
                return;
            }
            popup.HeaderText("Obtained New Assignment");
            popup.SubHeaderText(assignmentData.Name);
            popup.Description(assignmentData.Description, 24);
            popup.ToggleButtons("assignment");
            popup.SetAssignment(assignmentData);
            popup.PopupShowAssignment += HandlePopupShowAssignment;
            popup.PopupTrackAssignment += HandlePopupTrackAssignment;
        }
    }

    public void ShowPopupTutorial(string tutorialId) {
        //check *saved* list. If the ID is in the set, it's "seen"
        if (vars.SeenTutorials.Contains(tutorialId)) { return; }
        
        //check if it exists. check *in-code* dictionary to get the text
        if (!tutorialDatabase.TryGetValue(tutorialId, out TutorialText textToShow)) {
            GD.PrintErr($"Tutorial not found in database: {tutorialId}");
            return;
        }
        
        GetTree().Paused = true;
        
        if (tutorialDatabase.TryGetValue(tutorialId, out TutorialText tutorialData)) {
            PopupTutorial popup = popupTutorialScene.Instantiate<PopupTutorial>();
            AddChild(popup);

            popup.SetHeader(tutorialData.Header);
            popup.SetDescription(tutorialData.Description); // (Assuming you have a function like this)
        } else {
            GD.PrintErr($"Error: Tutorial ID '{tutorialId}' not found in database!");
        }
        
        //mark as seen. add the ID to the "seen" list and save the game
        vars.SeenTutorials.Add(tutorialId);
        vars.SaveGameData();
    }
    
    private void BuildTutorialDictionary() {
        tutorialDatabase = new System.Collections.Generic.Dictionary<string, TutorialText> {
            { "Bullet", new TutorialText { 
                Header = "Tutorial: Bullets", 
                Description = "maybe" 
            }},
            
            { "Extract", new TutorialText { 
                Header = "Tutorial: Extracts", 
                Description = "Extracts can be equipped in inventory and provide various stats.\n\n" +
                              "They can be found in the world, dropped by enemies, or created in The Distillery." 
            }},

            { "Cracking", new TutorialText { 
                Header = "Tutorial: Cracking", 
                Description = "Some objects are sealed with a hard candy coating and need to be broken open.\n\n" +
                              "Stand close to them to begin cracking. If the area is left the timer will reset.\n\n" +
                              "Enemies will be slowed when they enter cracking radius. Size and time are determined by Cracking skill level." 
            }}
            
        };
    }
    
    private void HandlePopupShowAssignment(AssignmentData assignment) {
        ShowMenuPanel("assignments");
        assignments.ShowAssignmentInfo(assignment.Id);
    }
    
    private void HandlePopupTrackAssignment(AssignmentData assignment) {
        assignments.ToggleAssignmentTracking(assignment);
    }
    
    public bool GetMainMenuVisible() { return mainMenu.Visible; }
    
    private void BuildDistillery() {
        if (!CheckCanBuildDistillery()) {
            return;
        }

        main.ChangeSucrose(-GetBuildDistillerySucroseCost());
        // vars.DistilleryBuilt = true; //not used
        vars.SaveGameData();
        EmitSignal(SignalName.ObjectBuilt, "distillery");
    }

    public bool CheckCanBuildDistillery() {
        var canBuild = false;
        if (vars.CurrentSucrose >= GetBuildDistillerySucroseCost()) {
            canBuild = true;
        }

        return canBuild;
    }

    private int GetBuildDistillerySucroseCost() {
        return 100;
    }
    
    // private void HandleInteractStateChanged() { //delete -can't interact when menu open
    //     if (vars.CurrentWorldObject == "") { HideAllPanels(); }
    // }

    private void SetWorldMapIcons() {
        var playerMarker = GetNode<AnimatedSprite2D>("WorldMap/PlayerMarker");
        var playerPos = main.GetPlayerPosition();
        // var translatedPosition = new Vector2I((int) Math.Ceiling(playerPos.X / 85), (int) Math.Ceiling(playerPos.Y / 85));
        // translatedPosition = new Vector2I(4, 113); //x 4
        // var translatedPosition = new Vector2(16, 254f);
        // playerMarker.SetPosition(translatedPosition);
        
        var playerPosX = playerPos.X;
        var playerPosY = playerPos.Y;
        var mapPosX = playerPosX * (1.0f / 120.0f);
        var mapPosY = playerPosY * (1.0f / 120.0f);
        
        playerMarker.SetPosition(new  Vector2(mapPosX, mapPosY));

        //tile size: 24px base tile * 20 scale = 480px/tile
        //world width: 480 tiles * 480 tile size = 230400
        //world height: 270 tiles * 480 tile size = 129600

        //conversion ration on 1920x1080 map
        //x: 1920 / 230,400 = 1 / 120
        //y: 1080 / 129,600 = 1 / 120
        //120

        //translate world position
        //actual position (1920, 30510)
        //map Position X: 1920 * (1 / 120) = 16
        //map Position Y: 30510 * (1 / 120) = 254.25
    }
    
    private void OpenMainMenu() {
        mainMenu.Visible = true;
        if (previousPanel == "") { ShowMenuPanel("settings"); }
        else { ShowMenuPanel(previousPanel); }
    }

    private void CloseMainMenu() {
        mainMenu.Visible = false;
        GetTree().Paused = false;
    }
    
    public void ToggleWorldMap() {
        worldMap.Visible = !worldMap.Visible;
        if (worldMap.Visible) { SetWorldMapIcons(); }
        // TogglePause();
    }

    public void CloseWorldMap() {
        worldMap.Visible = false;
    }
    
    public string TrimNumber(double number) {
        switch (number) {
            case <= 9999:
                return number.ToString();
            case > 9999 and <= 999999: {
                double scaledValue = number * 0.001;
                return scaledValue % 1 == 0 ? $"{(int)scaledValue}" + "k" : $"{scaledValue:0.0}" + "k";
            }
            case >= 1000000 and <= 999999999: {
                double scaledValue = number * 0.000001;
                return scaledValue % 1 == 0 ? $"{(int)scaledValue}" + "m" : $"{scaledValue:0.0}" + "m";
            }
            case >= 1000000000 and <= 999999999999: {
                double scaledValue = number * 0.000000001;
                return scaledValue % 1 == 0 ? $"{(int)scaledValue}" + "b" : $"{scaledValue:0.0}" + "b";
            }
            case >= 1000000000 and <= 999999999999999: {
                double scaledValue = number * 0.000000000001;
                return scaledValue % 1 == 0 ? $"{(int)scaledValue}" + "t" : $"{scaledValue:0.0}" + "t";
            }
            case > 999999999999999 and <= 999999999999999999: {
                double scaledValue = number * 0.000000000000001;
                return scaledValue % 1 == 0 ? $"{(int)scaledValue}" + "q" : $"{scaledValue:0.0}" + "q";
            }
            default:
                return $"{number:#.##E+0}"; //number > 999999999999999999
        }
    }
}

// public class TutorialText {
//     public string Header { get; set; }
//     public string Description { get; set; }
//
//     //parameterless constructor
//     public TutorialText() { }
// 	
//     public TutorialText(string id) {
//         // Header = header;
//     }
// }