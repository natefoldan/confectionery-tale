using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;
using FileAccess = Godot.FileAccess;

namespace ConfectioneryTale.scripts;

public partial class Variables : Node {
	private const string FilePath = "user://savedata.tres";
	
	// This will hold the entire loaded/new SavedResources instance.
	// It's the central container for ALL persistent game data.
	public SavedResources savedResources;
	public Array<SavedItemData> SavedItemDataArray { get; set; }
	public Array<SavedPortalData> SavedPortalDataArray { get; set; }
	public Array<SavedWorldObjectData> SavedWorldObjectArray { get; set; }
	public Array<SavedAssignmentData> SavedAssignmentDataArray { get; set; }
	// public List<(string area, bool discovered)> discoveredAreas { get; set; } //delete
	public HashSet<string> DiscoveredAreas { get; set; } = new HashSet<string>();
	//public properties for easy access (will be synced to/from savedResources) ---
	//convenience getters/setters for simple data. For complex data like lists, access savedResources.droppedItems directly.
	
    private int TotalSaves { get; set; }
    public int PlayerLevel { get; set; }
    public double CurrentPlayerExp { get; set; }
    public double CurrentSucrose { get; set; }
    public int CurrentPlayerHealth { get; set; }
    public int CurrentFireMode { get; set; }
    public bool AutoFire { get; set; }
    public bool CondenserBuilt { get; set; }
    public bool RefinerBuilt { get; set; }
    // public bool SprayerOwned { get; set; }
    public bool SoftenerOwned { get; set; }
    public bool SpreaderOwned { get; set; }
    public bool SniperOwned { get; set; }
    public bool SlowerOwned { get; set; }
    public bool SmasherOwned { get; set; }
    public int StoryProgress { get; set; }
    public string CurrentArea { get; set; }
    public int PlainRank { get; set; }
    public int GroveRank { get; set; }
    public int FallsRank { get; set; }
    public int CakeRank { get; set; }
    public int SwampRank { get; set; }
    public int WoodsRank { get; set; }
    public int PointRank { get; set; }
    public bool EnteredPlain { get; set; }
    public bool EnteredGrove { get; set; }
    
    //skills
    public int SkillCrackingLevel { get; set; }
    public int SkillCrackingCurrentExp { get; set; }
    public int SkillCampingLevel { get; set; }
    public int SkillCraftingLevel { get; set; }
    // public int SkillCampingCurrentExp { get; set; } //don't need -delete
    
    public int SkillSprayerDamage { get; set; }
    public int SkillSprayerCritChance { get; set; }
    public int SkillSprayerKnockback { get; set; }
    public int SkillSprayerMelt { get; set; }
    
    public int SkillSoftenerDamage { get; set; }
    public int SkillSoftenerPierce { get; set; }
    public int SkillSoftenerSize { get; set; }
    public int SkillSoftenerChill { get; set; }
    
    public int SkillSpreaderDamage { get; set; }
    public int SkillSpreaderBullets { get; set; }
    public int SkillSpreaderKnockback { get; set; }
    public int SkillSpreaderReload { get; set; }
    
    public int SkillSlowerDamage { get; set; }
    public int SkillSlowerSize { get; set; }
    public int SkillSlowerMelt { get; set; }
    public int SkillSlowerReload { get; set; }
    
    public int SkillSniperDamage { get; set; }
    public int SkillSniperInstant { get; set; }
    public int SkillSniperReload { get; set; }
    public int SkillSniperRestore { get; set; }
    
    public int SkillSmasherDamage { get; set; }
    public int SkillSmasherChill { get; set; }
    public int SkillSmasherKnockback { get; set; }
    public int SkillSmasherMelt { get; set; }
    
    // [Export] public Godot.Collections.Dictionary<string, int> SkillLevels = new Godot.Collections.Dictionary<string, int>();
    
    //consumables and useables
    public double TentCooldownTimer { get; set; }
    public int TinctureHealthAmount { get; set; }
    public int TinctureSpeedAmount { get; set; }
    public int TinctureConcealAmount { get; set; }
    public double TinctureHealthCooldown { get; set; } //not saved
    public double TinctureSpeedCooldown { get; set; } //not saved
    public double TinctureConcealCooldown { get; set; } //not saved
    
    //settings
    public bool MusicOn { get; set; }
    public bool SoundOn { get; set; }
    public float MusicVolume { get; set; }
    public float MusicVolumeSlider { get; set; }
    public float SoundVolume { get; set; }
    public float SoundVolumeSlider { get; set; }
    
    public string currentCompassTarget { get; set; }
    
    [Signal] public delegate void ShelteredStateChangedEventHandler();
    private bool _isSheltered = false; //use a private backing field
    public bool IsSheltered {
	    get { return _isSheltered; }
	    set {
		    if (_isSheltered != value) { //check if the value has changed
			    _isSheltered = value;
			    EmitSignal(SignalName.ShelteredStateChanged); //emit the signal only on change
		    }
	    }
    }
    
    //not saved
    public bool GameLoaded { get; set; }
    public bool CutsceneActive { get; set; }
    // public string CurrentWorldObject { get; set; } //old way -maybe
    public WorldObject CurrentWorldObject { get; set; } = null;
    public bool IsCracking { get; set; }
    
    [Signal] public delegate void InteractStateChangedEventHandler();
    private bool _isInteracting = false;
    public bool IsInteracting {
	    get { return _isInteracting; }
	    set {
		    if (_isInteracting != value) {
			    _isInteracting = value;
			    EmitSignal(SignalName.InteractStateChanged);
		    }
	    }
    }
    
    
    //dev
    public bool VarsReady { get; set; }
    public bool PlayerReady { get; set; }
    public bool HideDev { get; set; }
    public bool HideHud { get; set; }
    
	public override void _Ready() {
		// GD.Print("Variables Autoload is Ready. Loading game data.");
		savedResources = LoadSavedData(); //load data from disk or create new
		SyncDataFromSavedResources(); //pull data from savedResources into public properties

		//dev overrides (apply after syncing from save) FOR TESTING ONLY
		// HideDev = true;
		// HideHud = true;
		GameLoaded = true;
		SoundOn = false;
		MusicOn = false;
		// MusicOn = true;
		PlayerLevel = 1;
		CurrentSucrose = 10000;
		CurrentPlayerExp = 0;
		// SkillCrackingLevel = 1;
		// CurrentWorldObject = ""; //old way
		CurrentWorldObject = null;
		CondenserBuilt = false;
		RefinerBuilt = false;
		
		SoftenerOwned = true;
		SpreaderOwned = true;
		SniperOwned = true;
		SlowerOwned = true;
		SmasherOwned = true;
		TinctureHealthAmount = 4;
		TinctureSpeedAmount = 4;
		TinctureConcealAmount = 4;
		SkillCraftingLevel = 1; //delete
		
		// currentCompassTarget = "navTestOne";
		currentCompassTarget = "";
		VarsReady = true; // Set to true only after ALL setup is complete
		// GD.Print("Variables: Initial Load/Sync Complete. Current Sucrose: " + CurrentSucrose);
	}
	
	    private SavedResources LoadSavedData() {
        // --- 1. Get the GLOBAL file system path for the save directory ---
        string globalFilePath = ProjectSettings.GlobalizePath(FilePath);
        string saveDirPath = System.IO.Path.GetDirectoryName(globalFilePath);

        // --- 2. Create the directory if it doesn't exist ---
        // This will create a path like C:/Users/natef/AppData/Roaming/Godot/app_userdata/shelter/[your_project_name]
        if (!string.IsNullOrEmpty(saveDirPath)) {
            Error dirCreationResult = DirAccess.MakeDirRecursiveAbsolute(saveDirPath);
            if (dirCreationResult != Error.Ok) {
                GD.PushError($"Failed to create save directory '{saveDirPath}'. Error: {dirCreationResult}. Check permissions.");
                // Handle this error gracefully, e.g., return new SavedResources()
                return new SavedResources();
            }
        }

        // --- 3. Now, attempt to load the actual save file ---
        if (FileAccess.FileExists(FilePath)) { // Still use FilePath (user://) for FileAccess
            var savedResourcesTEMP = ResourceLoader.Load(FilePath);
            savedResources = (SavedResources)savedResourcesTEMP;

            if (savedResources != null) {
                GD.Print("Saved data loaded successfully from file.");
                return savedResources;
            } else {
                GD.PushError($"ResourceLoader.Load returned null for '{FilePath}'. File might be corrupted. Creating new.");
            }
        } else {
            GD.Print($"No save file found at '{FilePath}'. Creating new SavedResources instance.");
        }

        //f file doesn't exist or loading failed, create and return a new instance
        return new SavedResources() {
	        TotalSaves = 0,
            PlayerLevel = 1,
            CurrentFireMode = 0,
            AutoFire = false,
            MusicOn = true,
            MusicVolume = .5f,
            MusicVolumeSlider = .5f,
            SoundOn = true,
            SoundVolume = .5f,
            SoundVolumeSlider = .5f,
            CurrentCompassTarget = "",
            CurrentArea = "Plain",
            inventoryExtracts = new Array<BaseExtract>(),
            equippedExtracts = new Array<BaseExtract>(),
            SavedItemDataArray = new Array<SavedItemData>(),
            SavedWorldObjectArray = new Array<SavedWorldObjectData>(),
            SavedPortalDataArray = new Array<SavedPortalData>(),
            SavedAssignmentDataArray = new Array<SavedAssignmentData>()
        };
    }
	
	//syncs public properties from savedResources for easy access
    private void SyncDataFromSavedResources() {
        TotalSaves = savedResources.TotalSaves;
        PlayerLevel = savedResources.PlayerLevel;
        CurrentPlayerExp = savedResources.CurrentPlayerExp;
        CurrentSucrose = savedResources.CurrentSucrose;
        CurrentPlayerHealth = savedResources.CurrentPlayerHealth;
        CurrentFireMode = savedResources.CurrentFireMode;
        AutoFire = savedResources.AutoFire;
        CondenserBuilt = savedResources.CondenserBuilt;
        RefinerBuilt = savedResources.RefinerBuilt;
        SoftenerOwned = savedResources.SoftenerOwned;
        SpreaderOwned = savedResources.SpreaderOwned;
        SniperOwned = savedResources.SniperOwned;
        SlowerOwned = savedResources.SlowerOwned;
        SmasherOwned = savedResources.SmasherOwned;
        
	    // SkillCrackingLevel = savedResources.SkillCrackingLevel;
	    // SkillCrackingCurrentExp = savedResources.SkillCrackingCurrentExp;
        
        SkillSprayerDamage = savedResources.SkillSprayerDamage;
        SkillSprayerCritChance = savedResources.SkillSprayerCritChance;
        SkillSprayerKnockback = savedResources.SkillSprayerKnockback;
        SkillSprayerMelt = savedResources.SkillSprayerMelt;
        
        SkillSoftenerDamage = savedResources.SkillSoftenerDamage;
        SkillSoftenerSize = savedResources.SkillSoftenerSize;
        SkillSoftenerPierce = savedResources.SkillSoftenerPierce;
        SkillSoftenerChill = savedResources.SkillSoftenerChill;
         
        SkillSpreaderDamage = savedResources.SkillSpreaderDamage;
        SkillSpreaderBullets = savedResources.SkillSpreaderBullets;
        SkillSpreaderKnockback = savedResources.SkillSpreaderKnockback;
        SkillSpreaderReload = savedResources.SkillSpreaderReload;
        
        SkillSlowerDamage = savedResources.SkillSlowerDamage;
        SkillSlowerSize = savedResources.SkillSlowerSize;
        SkillSlowerMelt = savedResources.SkillSlowerMelt;
        SkillSlowerReload = savedResources.SkillSlowerReload;
        
        SkillSniperDamage = savedResources.SkillSniperDamage;
        SkillSniperInstant = savedResources.SkillSniperInstant;
        SkillSniperReload = savedResources.SkillSniperReload;
        SkillSniperRestore = savedResources.SkillSniperRestore; 
        
        SkillSmasherDamage = savedResources.SkillSmasherDamage;
        SkillSmasherChill = savedResources.SkillSmasherRadius;
        SkillSmasherKnockback = savedResources.SkillSmasherKnockback;
        SkillSmasherMelt = savedResources.SkillSmasherMelt;
        
        MusicOn = savedResources.MusicOn;
        MusicVolume = savedResources.MusicVolume;
        MusicVolumeSlider = savedResources.MusicVolumeSlider;
        SoundOn = savedResources.SoundOn;
        SoundVolume = savedResources.SoundVolume;
        SoundVolumeSlider = savedResources.SoundVolumeSlider;
        SavedItemDataArray = savedResources.SavedItemDataArray; //MOVE
        SavedWorldObjectArray = savedResources.SavedWorldObjectArray; //MOVE
        SavedPortalDataArray = savedResources.SavedPortalDataArray; //MOVE
        SavedAssignmentDataArray = savedResources.SavedAssignmentDataArray; //MOVE
        // droppedItems (the Array) is handled by reference, not copied here
    }

    // --- Saves current public properties TO savedResources, then saves savedResources to disk ---
    public void SaveGameData() {
        // --- Sync current public properties back to savedResources before saving ---
        // This is crucial because savedResources.droppedItems is modified directly,
        // but other primitive properties (like PlayerLevel) are separate.
        savedResources.TotalSaves = TotalSaves;
        savedResources.PlayerLevel = PlayerLevel;
        savedResources.CurrentPlayerExp = CurrentPlayerExp;
        savedResources.CurrentSucrose = CurrentSucrose;
        savedResources.CurrentPlayerHealth = CurrentPlayerHealth;
        savedResources.CurrentFireMode = CurrentFireMode;
        savedResources.AutoFire = AutoFire;
        savedResources.CondenserBuilt = CondenserBuilt;
        savedResources.RefinerBuilt = RefinerBuilt;
        
        savedResources.SoftenerOwned = SoftenerOwned;
        savedResources.SpreaderOwned = SpreaderOwned;
        savedResources.SniperOwned = SniperOwned;
        savedResources.SlowerOwned = SlowerOwned;
        savedResources.SmasherOwned = SmasherOwned;
        
        // savedResources.SkillCrackingLevel = SkillCrackingLevel;
        // savedResources.SkillCrackingCurrentExp = SkillCrackingCurrentExp;
        
        savedResources.SkillSprayerDamage = SkillSprayerDamage;
        savedResources.SkillSprayerCritChance = SkillSprayerCritChance;
        savedResources.SkillSprayerKnockback = SkillSprayerKnockback;
        savedResources.SkillSprayerMelt = SkillSprayerMelt;

        savedResources.SkillSoftenerDamage = SkillSoftenerDamage;
        savedResources.SkillSoftenerPierce = SkillSoftenerPierce;
        savedResources.SkillSoftenerSize = SkillSoftenerSize;
        savedResources.SkillSoftenerChill = SkillSoftenerChill;
    
        savedResources.SkillSpreaderDamage = SkillSpreaderDamage;
        savedResources.SkillSpreaderBullets = SkillSpreaderBullets;
        savedResources.SkillSpreaderKnockback = SkillSpreaderKnockback;
        savedResources.SkillSpreaderReload = SkillSpreaderReload;

        savedResources.SkillSlowerDamage = SkillSlowerDamage;
        savedResources.SkillSlowerSize = SkillSlowerSize;
        savedResources.SkillSlowerMelt = SkillSlowerMelt;
        savedResources.SkillSlowerReload = SkillSlowerReload;

        savedResources.SkillSniperDamage = SkillSniperDamage;
        savedResources.SkillSniperInstant = SkillSniperInstant;
        savedResources.SkillSniperReload = SkillSniperReload;
        savedResources.SkillSniperRestore = SkillSniperRestore;

        savedResources.SkillSmasherDamage = SkillSmasherDamage;
        savedResources.SkillSmasherRadius = SkillSmasherChill;
        savedResources.SkillSmasherKnockback = SkillSmasherKnockback;
        savedResources.SkillSmasherMelt = SkillSmasherMelt;
        
        savedResources.MusicOn = MusicOn;
        savedResources.MusicVolume = MusicVolume;
        savedResources.MusicVolumeSlider = MusicVolumeSlider;
        savedResources.SoundOn = SoundOn;
        savedResources.SoundVolume = SoundVolume;
        savedResources.SoundVolumeSlider = SoundVolumeSlider;
        savedResources.SavedItemDataArray = SavedItemDataArray;
        savedResources.SavedWorldObjectArray = SavedWorldObjectArray;
        savedResources.SavedPortalDataArray = SavedPortalDataArray;
        savedResources.SavedAssignmentDataArray = SavedAssignmentDataArray;

        Error saveResult = ResourceSaver.Save(savedResources, FilePath); // Save the SavedResources instance
        if (saveResult == Error.Ok) {
            // GD.Print($"Game data saved successfully to '{FilePath}'");
        } else {
            GD.PrintErr($"Error saving game data to '{FilePath}'. Error: {saveResult}");
        }
    }
	
    public SavedResources GetSavedResourcesInstance() { return savedResources; }
	
	// public void AddItemToSavedItemsArray(BaseExtract item) { //not used
	// 	savedResources.AddInventoryItemToResource(item);
	//     SaveGameData();
	// }

	public void DeleteSave() {
		var path = ProjectSettings.GlobalizePath(FilePath);
		if (File.Exists(path)) {
			File.Delete(path);
			GD.Print("Save file deleted successfully.");
			GetTree().Quit(); //uncomment
		} else {
			GD.Print("Save file not found.");
		}
	}
}