using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Collections;

namespace ConfectioneryTale.scripts;

public partial class EnemySpawner : StaticBody2D {
    private Variables vars;
    private Main main;
    private PackedScene enemyScene;
    private float spawnTimer;
    [Export] public string id;
    [Export] public BaseEnemy[] enemies;
    
    [Export] public Type PortalType { get; set; }
    public enum Type { Blue, Red, Purple }
    
    [Export] public float spawnRate;
    [Export] public float spawnRateVariation = 0.2f;
    [Export] public Texture2D blueTexture;
    [Export] public Texture2D redTexture;
    [Export] public Texture2D purpleTexture;
    [Export] public Texture2D blueSwirlTexture;
    [Export] public Texture2D redSwirlTexture;
    [Export] public Texture2D purpleSwirlTexture;
    [Export] public float parallaxFactor { get; set; } = 1.02f; // Values > 1 for closer
    // [Export] public float parallaxRatio { get; set; } = .95f; //best -not currently using ratio
    // [Export] public float parallaxRatio { get; set; } = 1.05f;
    // [Export] public float parallaxRatio { get; set; } = 2.05f;
    [Export] public int detectionRadius = 10000;
    [Export] public bool disabled;
    [Export] public bool corrupted;

    private TextureProgressBar cleanseBar;
    private AnimationPlayer animationPlayer;
    private Camera2D _camera;
    private Sprite2D sprite;
    private Sprite2D swirl;
    private Vector2 _initialPosition;
    private Vector2 _lastCameraPosition = Vector2.Zero;
    private bool active;
    private float currentSpawnTime;
    
    //cleansing
    private Array<SavedPortalData> savedPortalDataArray = new Array<SavedPortalData>();
    private bool cleansing;
    private bool cleansed;
    private double cleanseProgress;

    private int totalEnemies;
    
    public override void _Ready() {
        vars = GetNode<Variables>("/root/Variables");
        main = GetNode<Main>("/root/Main");
        
        if (vars.SavedPortalDataArray == null) { SavePortalData(); }
        LoadSavedPortalData();
    }

    public override void _PhysicsProcess(double delta) {
        // GD.Print($"disabled: {disabled} active: {active} sheltered: {vars.IsSheltered}");
        if (disabled) { return; }
        if (!vars.GameLoaded) { return; }
        if (vars.IsSheltered) { return; }
        if (!active) { return; }
        SpawnNewEnemy(delta, null);
        CleansePortal(delta);
        
        //WORKS - DON'T DELETE
        // if (_camera != null) {
        //     Vector2 cameraPosition = _camera.GlobalPosition;
        //     Vector2 cameraDelta = cameraPosition - _lastCameraPosition;
        //     Vector2 offset = cameraDelta * (parallaxRatio - 1);
        //     GlobalPosition += offset; // Apply offset to the current position
        //     _lastCameraPosition = cameraPosition;
        // }
        
        //use parallax factor for floating effect (foreground)
        
        if (_camera != null) {
            Vector2 cameraPosition = _camera.GlobalPosition;
            Vector2 cameraDelta = cameraPosition - _lastCameraPosition;
            Vector2 offset = cameraDelta * (parallaxFactor - 1);
            GlobalPosition += offset;
            _lastCameraPosition = cameraPosition;
            // GD.Print(GlobalPosition);
        }
    }

    public void SetupSpawner(Camera2D camera) {
        enemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
        sprite = GetNode<Sprite2D>("Sprite2D");
        swirl = GetNode<Sprite2D>("Swirl");
        currentSpawnTime = GetNewSpawnTime();
        cleanseBar = GetNode<TextureProgressBar>("CleanseBar");
        ResetCleanseBar();
        
        SetTexture();
        SetCollisionCircle();
        // SetSpawnRate(2.0f);
        //parallax
        // Find the active camera in the scene
        // _camera = GetViewport().GetCamera2D();
        _camera = camera; //THIS IS BEING PASSED FROM MAIN NOW
        // GD.Print("spawner tried to get camera " + _camera);
        // if (_camera == null)
        // {
        //     GD.PushWarning("No active Camera2D found in the viewport.");
        // }
        // _initialPosition = GlobalPosition; // Store the initial world position
        // GD.Print(_initialPosition);
        if (_camera != null) {
            _lastCameraPosition = _camera.GlobalPosition;
        }
        _initialPosition = GlobalPosition;

        // SavePortalData(); //delete
    }

    private void SetTexture() {

        switch (PortalType) {
            case Type.Blue:
                sprite.Texture = blueTexture;
                swirl.Texture = blueSwirlTexture;
                break;
            case Type.Red:
                sprite.Texture = redTexture;
                swirl.Texture = redSwirlTexture;
                break;
            case Type.Purple:
                sprite.Texture = purpleTexture;
                swirl.Texture = purpleSwirlTexture;
                break;
        }
    }
    
    private void SetCollisionCircle() {
        if (sprite == null) { return; }

        var collisionShape = new CollisionShape2D();
        var collisionCircle = new CircleShape2D();

        float radius = Mathf.Max(detectionRadius, detectionRadius / 2);
        collisionCircle.Radius = radius;
        collisionShape.Shape = collisionCircle;
        collisionShape.Position = Vector2.Zero; //center the circle on the Area2D's origin
        
        if (!disabled) {
            GetNode<Area2D>("DetectionArea").AddChild(collisionShape);
        }
    }
    
    private void SpawnNewEnemy(double delta, BaseEnemy what) {
        // return;
        // if (totalEnemies > 0) { return; } //for testing delete
        // if (enemies.Length < 1) { return; }
        spawnTimer += (float)delta;
    
        if (spawnTimer >= currentSpawnTime) { 
            BaseEnemy newEnemy = enemies[GD.RandRange(0, enemies.Length - 1)];
            if (what != null) { newEnemy = what; }
            Enemy enemy = enemyScene.Instantiate<Enemy>();
            totalEnemies += 1;
            enemy.SetThisEnemy(newEnemy);
            enemy.GlobalPosition = GlobalPosition;
            GetTree().Root.AddChild(enemy);
        
            //reset the timer (keeping the overflow)
            spawnTimer -= currentSpawnTime; 
        
            //get a new random spawn time for the *next* enemy
            currentSpawnTime = GetNewSpawnTime(); 
        }
        
        
        // if (spawnTimer >= spawnRate) { //original -delete
        //     BaseEnemy newEnemy = enemies[GD.RandRange(0, enemies.Length - 1)];
        //     if (what != null) { newEnemy = what; }
        //     Enemy enemy = enemyScene.Instantiate<Enemy>();
        //     totalEnemies += 1;
        //     // enemy.SetupEnemy(newEnemy);
        //     enemy.SetThisEnemy(newEnemy);
        //     enemy.GlobalPosition = GlobalPosition; //enemy position in middle of spawner
        //     GetTree().Root.AddChild(enemy);
        //     spawnTimer -= GetSpawnRate(); //reset the timer, potentially with a remainder
        // }
    }

    private float GetNewSpawnTime() {
        float min = spawnRate - spawnRateVariation;
        float max = spawnRate + spawnRateVariation;
        float randomPercent = GD.Randf(); 
        return (randomPercent * (max - min)) + min;
    }
    
    //cleansing
    private void CleansePortal(double delta) {
        // GD.Print("corrupted: " + corrupted + " cleansing: " + cleansing + " COMPLETE: " + cleansed);
        if (cleansed) { return; }
        if (!cleansing) { return; }
        cleanseProgress += delta * 100;
        UpdateCleanseBar();
    }
    
    private void UpdateCleanseBar() {
        cleanseBar.Value = cleanseProgress;
        if (cleanseBar.Value >= GetCleanseTime()) { CompleteCleanse(); }
    }

    private void ResetCleanseBar() {
        cleanseBar.Value = 0;
        cleanseBar.MaxValue = GetCleanseTime();
        if (cleansed) { cleanseBar.Visible = false; }
    }
    
    private double GetCleanseTime() {
        return main.GetCleanseTime() * 100;
    }
    
    private void CompleteCleanse() {
        cleansing = false;
        cleanseBar.Visible = false;
        cleansed = true;
        SavePortalData();
    }
    
    //collisions
    private void PlayerInRange(Area2D body) {
        // GD.Print("player in range");
        active = true;
    }
    
    private void PlayerNotInRange(Area2D body) {
        // GD.Print("player NOT in range");
        active = false;
    }
    
    private void PlayerInCleanseRange(Area2D body) {
        if (cleansed) { return; }
        cleansing = true;
    }
    
    private void PlayerNotInCleanseRange(Area2D body) {
        if (cleansed) { return; }
        cleansing = false;
    }
    
    
    //saving and loading
    private void SavePortalData() {
        var savedPortalDataArray = vars.SavedPortalDataArray;
        if (savedPortalDataArray == null) {
            savedPortalDataArray = new Array<SavedPortalData>();
            vars.SavedPortalDataArray = savedPortalDataArray;
        }

        //try to find an existing entry for this portal's ID
        var existingData = savedPortalDataArray.FirstOrDefault(p => p.PortalId.Equals(id));

        if (existingData != null) {
            existingData.Cleansed = cleansed;
            GD.Print($"Updated portal data for ID: {id}. Cleansed is now {cleansed}");
        } else {
            //if the data does not exist, create a new entry and add it
            var newPortalData = new SavedPortalData {
                PortalId = id,
                Cleansed = cleansed,
            };
            savedPortalDataArray.Add(newPortalData);
            GD.Print($"Added new portal data with ID: {id}");
        }

        // GD.Print($"Saving portal ID: {id} with Cleansed: {cleansed}");
        vars.SaveGameData();
    }
    
    private void LoadSavedPortalData() {
        if (vars.SavedPortalDataArray == null) {
            GD.Print("No saved portal data to load.");
            return;
        }
    
        foreach (var loadedData in vars.SavedPortalDataArray) {
            if (!loadedData.PortalId.Equals(id)) { continue; }
            // GD.Print("found match: " + loadedData.PortalId);
            cleansed = loadedData.Cleansed;
        }
        // GD.Print(cleansed);
    }
}