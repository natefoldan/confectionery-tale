using System;
using System.Collections.Generic;
using Godot;

namespace ConfectioneryTale.scripts;

public partial class Player : CharacterBody2D {
    [Export] public PackedScene cutsceneManagerScene { get; set; }
    [Signal] public delegate void ShelteredChangedEventHandler();
    private Variables vars;
    private UI ui;
    private Main main;
    private ExtractManager extractManager;
    private TooltipHandler tooltips;
    private PackedScene popupTextScene;
    // private float pushForce = 200.0f;
    // private float pushForceMin = 10.0f;
    private int dropsCollectSpeed = 20000;
    private List<Node2D> currenciesInRange = new List<Node2D>();
    private PackedScene bulletScene;
    private WeaponData currentWeaponData;
    private Sprite2D sprite;
    private int spriteWidth;
    private int spriteHeight;
    // private TextureProgressBar reloadBar; //keep for option to set bar above player
    // private TextureProgressBar healthBar; //keep for option to set bar above player
    // private TextureProgressBar shieldBar; //keep for option to set bar above player
    private int[] fireModes;
    private Vector2 velocity = Vector2.Zero;
    private Vector2 direction;
    // private float currentReloadTimer;
    private float healthRegenTimer;
    private float shieldRegenTimer;
    private float currentShield;
    private float shieldDisabledTimer;
    private Tween canNotCollectTween;
    private Dictionary<int, float> reloadTimers = new Dictionary<int, float>();
    private AnimationPlayer animationPlayer;
    private bool playerShooting;
    private TileMapLayer tileMapWorld;
    private Vector2I currentTileCoord;
    private Vector2 teleportDestination;
    private string assignmentToGive;

    private bool showCutsceneOnArrival = false; //delete
    
    private readonly Color concealedColor = new Color(1, 1, 1, 0.4f);
    private readonly Color visibleColor = new Color(1, 1, 1, 1);
    
    private enum PlayerState {
        Sheltered,
        AimingToShoot
    }

    private PlayerState currentState = PlayerState.Sheltered;

    public override void _Ready() {
        SetupPlayer();
    }

    public override void _PhysicsProcess(double delta) {
        // if (!vars.GameLoaded) { return; }
        if (vars.CutsceneActive) { return; }
        velocity = Velocity;
        
        PlayerMovement();
        MoveAndSlide();
        // PushObject(delta);
        // RotateWeaponToMouse();
        CheckConcealed();
        FlipSprite();
        
        // FireBullet(delta); //original
        HandleFiring(delta);
        AnimatePlayer();
        ReloadInactiveWeapons(delta);
        RechargeShield(delta);
        RegenHealth(delta);
        CollectDrops();

        
        Vector2I newTileCoord = tileMapWorld.LocalToMap(GlobalPosition);

        // 2. Check if it's different from the last frame
        if (newTileCoord != currentTileCoord) {
            // 3. It's a new tile! Update our tracker and run the check.
            currentTileCoord = newTileCoord;
            CheckCurrentTileData(newTileCoord);
        }
    }
    
    private void SetupPlayer() {
        vars = GetNode<Variables>("/root/Variables");
        vars.ShelteredStateChanged += SetPlayerState;
        ui = GetNode<UI>("/root/Main/UI");
        main = GetNode<Main>("/root/Main");
        extractManager = GetNode<ExtractManager>("/root/ExtractManager");
        tooltips = GetNode<TooltipHandler>("/root/TooltipHandler");
        popupTextScene = GD.Load<PackedScene>("res://scenes/popup_text.tscn");
        sprite = GetNode<Sprite2D>("Sprite2D");
        spriteWidth = sprite.Texture.GetWidth();
        spriteHeight = sprite.Texture.GetHeight();
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        tileMapWorld = main.GetTileMapWorld();
        currentTileCoord = new Vector2I(-999, -999);
        // healthBar = GetNode<TextureProgressBar>("HealthBar");
        // shieldBar = GetNode<TextureProgressBar>("ShieldBar");
        main.GainedPlayerLevel += HandleLevelGained;
        SetupBullets();
        SetupReloadBar();
        ResetHealthBar();
        ResetShieldBar();
        SetPlayerState();
        SetPickupRadius();
        SetCrackingRadius();
        // currentState = PlayerState.AimingToShoot;
        // ProcessMode = ProcessModeEnum.Inherit; //dont use
        vars.PlayerReady = true;
    }

    private void CheckCurrentTileData(Vector2I tileCoord) {
        // GD.Print("NEW TILE");
        TileData tileData = tileMapWorld.GetCellTileData(tileCoord);

        if (tileData != null) {
            var isSheltered = (bool)tileData.GetCustomData("Sheltered");
        
            if (isSheltered) {
                if (!vars.IsSheltered) {
                    vars.IsSheltered = true;
                    // GD.Print($"sheltered: {vars.IsSheltered}");
                    EmitSignal(SignalName.ShelteredChanged);
                }
            } else { //tile exists, but "sheltered" is false
                if (vars.IsSheltered) {
                    vars.IsSheltered = false;
                    // GD.Print($"sheltered: {vars.IsSheltered}");
                    EmitSignal(SignalName.ShelteredChanged);
                }
            }
        }
        else { //player is on an empty tile (no data)
            if (vars.IsSheltered) {
                vars.IsSheltered = false;
                EmitSignal(SignalName.ShelteredChanged);
            }
        }

        if (tileData != null && tileData.GetCustomData("AreaName").AsString() is { } areaName && !string.IsNullOrEmpty(areaName)) {
            // GD.Print($"{areaName}");
            //runs every time player enters a different zone
            if (vars.CurrentArea != areaName) {
                vars.CurrentArea = areaName;
                GD.Print($"Entering zone: {areaName}");
                // --- This is where you would trigger new music, etc. ---
            }
            
            if (!vars.DiscoveredAreas.Contains(areaName)) { //first time entering an area
                GD.Print($"New Area Discovered: {areaName}!");
                // Add the name to the set. The next time this check runs,
                // Contains() will be true, and this block will be skipped.
                vars.DiscoveredAreas.Add(areaName);
                // main.GetTotalDiscoveredAreas();
                vars.SaveGameData();
                main.GainCampingLevel();
            }
        } else {
        }
    }
    
    private void SetupBullets() {
        bulletScene = GD.Load<PackedScene>("res://scenes/bullet.tscn");
        fireModes = new[] { 0, 1, 2, 3, 4, 5 };
        vars.CurrentFireMode = fireModes[0]; //load fire mode -not done
        // reloadTimerOne = main.GetAllBulletData()[0].ReloadSpeed; //don't do this, load it based on saved fire mode -delete
    }
    
    private void SetupReloadBar() {
        // reloadBar = GetNode<TextureProgressBar>("ReloadBar");
    }
    
    private void SetPlayerState() {
        if (vars.IsSheltered) {
            currentState = PlayerState.Sheltered;
        } else {
            currentState = PlayerState.AimingToShoot;
        }
        
        currentWeaponData = main.GetCurrentWeaponData();
        // SwapFireMode(0);
        SetMouseCursor();
    }

    public void StartTeleportCutscene(Vector2 destination, string whatAssignment) {
        teleportDestination = destination;
        assignmentToGive = whatAssignment;
    
        //start portal animation
        Visible = false;
        var portal = main.GetPlayerSpawnPortal();
        portal.Visible = true;
        var portalAnimation = portal.GetNode<AnimationPlayer>("AnimationPlayer");
        portalAnimation.Play("portal");
    
        var timer = GetTree().CreateTimer(1.0f);
        timer.Timeout += OnPortalAnimationFinished;
    }

    public void StartFallTween() {
        // GetTree().Paused = false;
        var fallDistance = 800;
        var fallDuration = .3f;

        var fallTween = CreateTween();
        
        fallTween
            .TweenProperty(this, "global_position:y", GlobalPosition.Y + fallDistance, fallDuration)
            .SetEase(Tween.EaseType.In);
        
        fallTween.Finished += OnFallTweenFinished;
        fallTween.Play();
    }

    // --- 5. Fall Finished, Unpause and Finish ---
    private void OnFallTweenFinished() {
        GD.Print("fall finished");
        // GetTree().Paused = false;
        vars.CutsceneActive = false;
        if (!string.IsNullOrEmpty(assignmentToGive)) {
            main.GainAssignment(assignmentToGive);
            // EmitSignal(SignalName.CutsceneFinished, _whatAssignment);
        }
        return;
        // Only emit the signal if we were actually given an assignment
        if (!string.IsNullOrEmpty(assignmentToGive)) {
            main.GainAssignment(assignmentToGive);
            // EmitSignal(SignalName.CutsceneFinished, _whatAssignment);
        }
        
        // EmitSignal(SignalName.CutsceneFinished, _whatAssignment); //delete
        QueueFree(); // The cutscene is over, remove self
    }
    
    //portal animation finished, show text on screen
    private void OnPortalAnimationFinished() {
        // main.GainAssignment(assignmentToGive);
        
        return;
        // Teleport the player *now*, right before the cutscene
        SetPos(teleportDestination);
        Visible = true;
        var cutscene = cutsceneManagerScene.Instantiate<CutsceneManager>();
        AddChild(cutscene);
    
        cutscene.CutsceneFinished += OnCutsceneFinished;
    
        cutscene.StartCutscene(this, Position, assignmentToGive,
            "line one",
            "line two"
        );
        
    }

    private void OnArrivalAnimationFinished(string whatAssignment) 
    {
        main.GainAssignment(whatAssignment);
    }
    //cutscene finished, give assignment
    private void OnCutsceneFinished(string whatAssignment) {
        main.GainAssignment(whatAssignment);
    }
    
    
    //fall tween finished, give assignment
    private void OnFallTweenFinished(string whatAssignment) {
        main.GainAssignment(whatAssignment);
    }
    
    public void SetMouseCursor() {
        if (currentState == PlayerState.Sheltered) {
            Input.SetCustomMouseCursor(null);
            return;
        }

        // currentWeaponData = main.GetCurrentWeaponData(); //for changing crosshairs -MOVED
        // var crosshairs = currentWeaponData.Crosshairs; //if using custom crosshairs for each bullet

        var allWeaponData = main.GetAllWeaponData(); //temp not using custom crosshairs
        var crosshairs = allWeaponData[0].Crosshairs;
        
        Vector2 hotspot = new Vector2(crosshairs.GetWidth() / 2f, crosshairs.GetHeight() / 2f);
        Input.SetCustomMouseCursor(crosshairs, Input.CursorShape.Arrow, hotspot);
    }

    public void SetCurrentWeaponData() {
        currentWeaponData = main.GetCurrentWeaponData();
    }
    
    private void RegenHealth(double delta) {
        if (!vars.IsSheltered) { return; }
        if (vars.CurrentPlayerHealth >= main.GetPlayerMaxHealth()) { return; }
        healthRegenTimer += (float) delta;
        if (healthRegenTimer < main.GetPlayerHealthRegenRate()) { return; }
        RestoreHealth(1);
    }
    
    private void RechargeShield(double delta) {
        //stop if shield is full
        if (currentShield >= main.GetPlayerFinalShield()) {
            currentShield = main.GetPlayerFinalShield();
            ui.UpdateShieldBar(currentShield);
            return;
        }

        var shieldRegenPerSecond = main.GetPlayerFinalShieldRegen();
        if (vars.IsSheltered) {
            shieldDisabledTimer = 0;
            shieldRegenPerSecond = 10;
        }
        
        //stop if shield recharge is disabled
        if (shieldDisabledTimer > 0) {
            shieldDisabledTimer -= (float) delta;
            return;
        }
        
        currentShield += shieldRegenPerSecond * (float)delta;
        currentShield = Mathf.Min(currentShield, main.GetPlayerFinalShield()); //clamp the shield to its maximum value
        ui.UpdateShieldBar(currentShield);
    }
    
    private void SwitchWeaponWithMouseWheel(int dir) {
        var switchTo = vars.CurrentFireMode + dir;
        if (switchTo > fireModes.Length - 1) { switchTo = fireModes[0]; }
        else if (switchTo < 0) { switchTo = fireModes.Length - 1; }
        main.EquipWeapon(switchTo);
    }
    
    private void ReloadInactiveWeapons(double delta) {
        float slowReloadFactor = 0.7f;
        for (int i = 0; i < fireModes.Length; i++) {
            if (fireModes[i] != vars.CurrentFireMode) {
                if (!reloadTimers.ContainsKey(fireModes[i])) {
                    reloadTimers[fireModes[i]] = 0.0f; // Initialize if not present
                }
                if (reloadTimers[fireModes[i]] < main.GetAllWeaponData()[i].ReloadSpeed) {
                    reloadTimers[fireModes[i]] += (float)delta * slowReloadFactor;
                    // GD.Print($"Reloading inactive weapon {fireModes[i]}: {reloadTimers[fireModes[i]]}");
                }
            }
        }
    }

    private void UpdateReloadBar() {
        // if (reloadBar == null) { return; } // Check reloadBar directly as instantiatedBullet is for bullet instance

        float fireRate = GetFireRate(); //get the max reload time for the current weapon

        //get current progress for the active weapon
        float currentProgress = 0.0f;
        if (reloadTimers.ContainsKey(vars.CurrentFireMode)) {
            currentProgress = reloadTimers[vars.CurrentFireMode];
        } else {
            // This case should ideally be handled earlier, maybe initialize all timers in SetupBullets
            // Or ensure a default behavior for modes not yet in dictionary
        }

        // Call ui.UpdatePlayerShootBar (assuming this is the correct name from previous turns) ONCE
        // This will handle setting both Value and MaxValue, and the text display.
        ui.UpdateReloadBar(currentProgress, fireRate);
    }

    private float GetFireRate() {
        // return main.GetAllWeaponData()[vars.CurrentFireMode].ReloadSpeed;
        return main.GetFinalWeaponReload();
    }

    public void PlayerGotHit(int amount) {
        var remainingDamage = amount;
        shieldDisabledTimer = 2;
        if (shieldDisabledTimer > 0) { shieldDisabledTimer = 2; }
        if (currentShield > 0) {
            InstantiatePopupText(amount, false, true, false, false);
            currentShield -= amount;
            remainingDamage = (int) -currentShield;
            if (currentShield <= 0) {
                currentShield = 0;
                shieldDisabledTimer = 2;
            }
            ui.UpdateShieldBar(currentShield);
            if (currentShield > 0) { return; }
        }
        
        if (remainingDamage > 0) { //only apply damage to health if there's any remaining
            InstantiatePopupText(remainingDamage, false, false, true, false);
            vars.CurrentPlayerHealth -= remainingDamage;
            ui.UpdateHealthBar(vars.CurrentPlayerHealth);
            if (vars.CurrentPlayerHealth <= 0) {
                PlayerDied();
            }
        }
    }

    public void PlayerRestoreHealthOnHit(float percentHealth) {
        var amount = main.GetPlayerMaxHealth() * percentHealth;
        RestoreHealth((int) amount);
    }

    private void RestoreHealth(int amount) {
        //store the health before the restoration
        var originalHealth = vars.CurrentPlayerHealth;

        //ensures the health never goes above MaxHealth
        vars.CurrentPlayerHealth = Mathf.Min(vars.CurrentPlayerHealth + amount, main.GetPlayerMaxHealth());
    
        //calculate the exact amount restored
        var actualAmountRestored = vars.CurrentPlayerHealth - originalHealth;

        ui.UpdateHealthBar(vars.CurrentPlayerHealth);
    
        InstantiatePopupText(actualAmountRestored, false, false, false, true);
    }

    public void UseTinctureHealth(float percentHealth) {
        var amount = Math.Ceiling(main.GetPlayerMaxHealth() *  percentHealth);
        RestoreHealth((int) amount);
    }

    private void InstantiatePopupText(double amount, bool critHit, bool shield, bool health, bool heal) {
        if (amount == 0) { return; }
    
        var size = 128;
        // if (critHit) { size = 320; }

        var text = $"-{amount}";
        
        var randXPos = GD.RandRange(-spriteWidth + 150, spriteWidth - 150);
        randXPos = 0;
        // var randYPos = GD.RandRange((-spriteHeight + 20), -spriteHeight);
        var randYPos = GD.RandRange(-500, -200); 
        
        var popupText = popupTextScene.Instantiate<PopupText>();
        // popupText.ZIndex = 100;

        var color = tooltips.GetDecimalColor("black");

        // GD.Print("shield: " + shield + " | " + "health: " + health);
        // GD.Print("shield: " + shield + " | " + "health: " + health + " | " + "heal: " + heal);
        
        if (shield) {
            // color = tooltips.GetDecimalColor("blue");
            color = new Color(.161f, .475f, 1f); //same blue as shield bar
        }

        if (health) {
            // color = tooltips.GetDecimalColor("red");
            color = new Color(.957f, .263f, .212f); //same red as health bar
        }

        if (heal) {
            color = tooltips.GetDecimalColor("green");
            text = $"+{amount}";
        }
        
        // color = new Color(.161f, .475f, 1f); //same blue as shield bar
        popupText.SetColor(color);
 
        var gpx = GlobalPosition.X + randXPos;
        var gpy = GlobalPosition.Y + randYPos;
        // GD.Print(gpx + " | " + gpy);
        // GD.Print(GlobalPosition.X + " | " + GlobalPosition.Y);
        
        popupText.SetTextAndSize(text, size);
        popupText.SetWorldPosition(new Vector2(gpx, gpy));
        main.GetWorld().AddChild(popupText);
        popupText.DamageTween();
    }

    private void PlayerDied() {
        ResetHealthBar();
        ResetShieldBar();
    }

    private void ResetHealthBar() {
        vars.CurrentPlayerHealth = main.GetPlayerMaxHealth();
        ui.UpdateHealthBar(main.GetPlayerMaxHealth());
    }
    
    private void ResetShieldBar() {
        currentShield = main.GetPlayerFinalShield();
        ui.UpdateShieldBar(main.GetPlayerFinalShield());
    }
    
    public override void _UnhandledInput(InputEvent @event) {
        if (Input.IsKeyPressed(Key.Shift)) { return; } //shift + scroll wheel zooms map
    
        //handle Weapon Swapping actions
        if (Input.IsActionPressed("swap_fire_mode_down")) {
            SwitchWeaponWithMouseWheel(1);
            GetTree().Root.SetInputAsHandled();
            return;
        }
        if (Input.IsActionPressed("swap_fire_mode_up")) {
            SwitchWeaponWithMouseWheel(-1);
            GetTree().GetRoot().SetInputAsHandled();
            return;
        }
    
        if (Input.IsActionPressed("select_gun_one")) { main.EquipWeapon(0); GetTree().GetRoot().SetInputAsHandled(); return; }
        if (Input.IsActionPressed("select_gun_two")) { main.EquipWeapon(1); GetTree().GetRoot().SetInputAsHandled(); return; }
        if (Input.IsActionPressed("select_gun_three")) { main.EquipWeapon(2); GetTree().GetRoot().SetInputAsHandled(); return; }
        if (Input.IsActionPressed("select_gun_four")) { main.EquipWeapon(3); GetTree().GetRoot().SetInputAsHandled(); return; }
        if (Input.IsActionPressed("select_gun_five")) { main.EquipWeapon(4); GetTree().GetRoot().SetInputAsHandled(); return; }
        if (Input.IsActionPressed("select_gun_six")) { main.EquipWeapon(5); GetTree().GetRoot().SetInputAsHandled(); return; }
        
        //if RMB is just pressed, toggle vars.AutoFire state
        if (Input.IsActionJustPressed("fire_alternate")) {
            vars.AutoFire = !vars.AutoFire;
            // GD.Print($"Autofire toggled: {vars.AutoFire}");
            GetTree().GetRoot().SetInputAsHandled(); //consume the input
            return; //stop processing this event
        }
        // Since LMB is NOT in Input Map, we must check the raw mouse button event.
        // This is crucial to ensure UI buttons don't get the click when it's for firing.
        if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.ButtonIndex == MouseButton.Left) {
            //if it's a press (and not an echo from holding)
            if (mouseButtonEvent.IsPressed() && !mouseButtonEvent.IsEcho()) {
                //this LMB click is for the player's firing action, so consume it
                GetTree().GetRoot().SetInputAsHandled();
                //no return here, as we still need the IsActionPressed("fire") check in HandleFiring to work based on the general state of the button being held.
            }
        }
        
        if (Input.IsActionJustPressed("interact_with")) {
            
            if (vars.CurrentWorldObject == null) { return; }
            // 2. Get the ID from the object instance
            string objectId = vars.CurrentWorldObject.objectId;
            
            // switch (vars.CurrentWorldObject) { //old way
            switch (objectId) {
                case "woDistillery":
                    ui.ToggleDistillery();
                    // if (vars.DistilleryBuilt) { ToggleDistillery(); } //if not enabling interaction before built -not used
                    // else { BuildDistillery(); }
                    break;
                
                case "woSoftener":
                    main.RemoveWorldObject("woSoftener");
                    ui.GainNewBullet(1);
                    break;
                
                case "playerPortalTrain":
                    main.TeleportPlayer("startingSpot");
                    break;
                
                case "worldExtract":
                    vars.CurrentWorldObject.PickupFixedExtract();
                    vars.CurrentWorldObject = null;
                    break;
            }
            GetTree().GetRoot().SetInputAsHandled();
        }
    }
    
    private void HandleFiring(double delta) {
        int currentMode = vars.CurrentFireMode;
        
        //ensure timer exists for current weapon
        if (!reloadTimers.ContainsKey(currentMode)) { reloadTimers[currentMode] = 0.0f; }

        UpdateReloadBar(); //always update UI bar, even if not firing or reloaded

        if (vars.IsSheltered) {
            playerShooting = false;
            return;
        } //player can't fire when sheltered

        //determine if player *intends* to fire based on auto-fire state and current input
        bool playerIntendsToFire = false;
        if (vars.AutoFire) {
            // If autofire is ON, player intends to fire continuously.
            // The gun will fire at its rate even if LMB is NOT held.
            playerIntendsToFire = true;
        } else {
            // If autofire is OFF, player intends to fire only if LMB is currently held down.
            playerIntendsToFire = Input.IsMouseButtonPressed(MouseButton.Left); // Directly check raw LMB state
            // playerShooting = true;
        }

        playerShooting = playerIntendsToFire;
        
        //check if weapon is reloaded AND player intends to fire
        float weaponReloadSpeed = GetFireRate(); //max reload time for current weapon

        if (reloadTimers[currentMode] >= weaponReloadSpeed) {
            if (playerIntendsToFire) {
                switch (currentMode) {
                    case 2: //spread shot
                        FireSpreadShot();
                        break;
                    case 5: //static bullet at cursor position (smasher)
                        var staticBullet = bulletScene.Instantiate<Bullet>();
                        staticBullet.SetBulletTexture(currentWeaponData.Texture, main.GetPlayerFinalBulletSize());
                        GetTree().Root.AddChild(staticBullet);
                        // Set the bullet's position to the global mouse position
                        staticBullet.SetupStaticBullet(GetGlobalMousePosition());
                        break;
                    default:
                        var normalBullet = bulletScene.Instantiate<Bullet>();
                        normalBullet.SetBulletTexture(currentWeaponData.Texture, main.GetPlayerFinalBulletSize());
                        direction = (GetGlobalMousePosition() - Position).Normalized();
                        GetTree().Root.AddChild(normalBullet);
                        var firePoint = GetNode<Node2D>("Sprite2D/FirePoint");
                        normalBullet.SetupMouseBullet(firePoint.GlobalPosition, direction);
                        break;
                }

                reloadTimers[currentMode] = 0.0f;
                UpdateReloadBar();
            }
        }
        
        //always increment reload progress for the currently active weapon, this allows the bar to fill up continuously, whether a shot was just fired,
        reloadTimers[currentMode] += (float)delta;
    }
    
    private void FireSpreadShot() {
        if (bulletScene == null) {
            GD.PushError("BulletScene is not assigned!");
            return;
        }
        var firePoint = GetNode<Node2D>("Sprite2D/FirePoint");
        if (firePoint == null) {
            GD.PushError("FirePoint not found");
            return;
        }
    
        int numberOfBullets = main.GetPlayerFinalBulletAmount();
    
        // Set a total spread angle. You can make this an exported variable too.
        float totalSpreadAngleDegrees = 30.0f; 

        Vector2 baseDirection = (GetGlobalMousePosition() - firePoint.GlobalPosition).Normalized();
        float baseAngleRad = baseDirection.Angle();
    
        //calculate the angle between each bullet
        float angleStepRad = Mathf.DegToRad(totalSpreadAngleDegrees);
        if (numberOfBullets > 1) {
            angleStepRad /= (numberOfBullets - 1);
        }
    
        // Calculate the starting angle for the first bullet
        float startAngleRad = baseAngleRad - (Mathf.DegToRad(totalSpreadAngleDegrees) / 2.0f);
    
        //fire each bullet dynamically
        for (int i = 0; i < numberOfBullets; i++) {
            var instantiatedBullet = bulletScene.Instantiate<Bullet>();
            instantiatedBullet.SetBulletTexture(currentWeaponData.Texture, main.GetPlayerFinalBulletSize());
        
            //calculate the specific angle for this bullet
            float currentAngleRad = startAngleRad + (angleStepRad * i);
        
            // Create the direction vector from the angle
            Vector2 bulletDirection = new Vector2(Mathf.Cos(currentAngleRad), Mathf.Sin(currentAngleRad));

            GetTree().Root.AddChild(instantiatedBullet);
            instantiatedBullet.SetupMouseBullet(firePoint.GlobalPosition, bulletDirection.Normalized());
        }
    }
    
    private void PlayerMovement() {
        direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        // if (direction != Vector2.Zero) { PlayerMoving(); }
        if (direction != Vector2.Zero) {
            direction = direction.Normalized();
        }
        velocity.X = direction.X * main.GetPlayerFinalSpeed();
        velocity.Y = direction.Y * main.GetPlayerFinalSpeed();
        Velocity = velocity;
    }

    private void AnimatePlayer() {
        if (animationPlayer == null) { return; }

        //Velocity.LengthSquared() is more efficient than Velocity.Length() for checking if magnitude > 0
        bool playerMoving = Velocity.LengthSquared() > 0.01f; //use a small threshold to account for floating-point inaccuracies
        
        if (playerMoving && !playerShooting) { //player moving and NOT shooting
            // GD.Print("MOVE: true | SHOOT: false");
            if (animationPlayer.CurrentAnimation != "player_run") { animationPlayer.Play("player_run"); } //only play if not already running
        } else if (!playerMoving && playerShooting) { //player NOT moving and IS shooting
            // GD.Print("MOVE: false | SHOOT: true");
            if (animationPlayer.CurrentAnimation != "player_idle_shoot") { animationPlayer.Play("player_idle_shoot"); }
        } else if (playerMoving && playerShooting) { //player IS moving and IS shooting
            // GD.Print("MOVE: true | SHOOT: true");
            if (animationPlayer.CurrentAnimation != "player_run_shoot") { animationPlayer.Play("player_run_shoot"); }
        } else { //player NOT moving and NOT shooting
            // GD.Print("MOVE: false | SHOOT: false");
            if (animationPlayer.CurrentAnimation != "player_idle") { animationPlayer.Play("player_idle"); }
        }
    }
    
    // private void PushObject(double delta) {
    //    if (!IsOnFloor()) { return; }
    //     for (int i = 0; i < GetSlideCollisionCount(); i++) { //get every collision
    //         KinematicCollision2D collision = GetSlideCollision(i);
    //         if (collision.GetCollider() is RigidBody2D pushable && Math.Abs(pushable.GetLinearVelocity().X) < 100) {
    //             pushable.ApplyCentralImpulse(collision.GetNormal() * -pushForce);
    //         }
    //     }
    // }

    private void CheckConcealed() {
        Modulate = vars.TinctureConcealCooldown > 0 ? concealedColor : visibleColor;
    }
    
    private void FlipSprite() {
        switch (Math.Sign(Velocity.X)) {
            case 1:
                sprite.Scale = new Vector2(1, 1);
                // sprite.FlipH = false;
                break;
            case -1:
                sprite.Scale = new Vector2(-1, 1);
                // sprite.FlipH = true;
                break;
            case 0:
                // Do nothing (or set a default flip state if needed)
                break;
        }
    }

    public void SetPos(Vector2 pos) {
        Position = pos;
    }
    
    public Vector2 GetPlayerPosition() {
        return Position;
    }

    public Camera2D GetPlayerCamera() {
        return GetNode<Camera2D>("Camera2D");
    }
    
    // private void CollisionAreaEntered(Area2D area) { //delete
    //     // GD.Print("player collided with enemy");
    //     // var aname = area.Name;
    //     // GD.Print("player collided with " + aname);
    // }

    private void HandleLevelGained() { SetPickupRadius(); }
    
    private void SetPickupRadius() {
        var pickupArea = GetNode<Area2D>("PickupRadius");
        
        foreach (Node child in pickupArea.GetChildren()) {
            if (child is CollisionShape2D oldShape) { oldShape.QueueFree(); } //free the old shape before adding a new one
        }
        
        var collisionShape = new CollisionShape2D();
        var collisionCircle = new CircleShape2D();

        // float radius = Mathf.Max(sprite.Texture.GetWidth(), sprite.Texture.GetHeight()) + 20f;
        float radius = main.GetPlayerFinalPickupRange();
        collisionCircle.Radius = radius;
        collisionShape.Shape = collisionCircle;
        collisionShape.Position = Vector2.Zero; //center the circle on the Area2D's origin
        pickupArea.AddChild(collisionShape);
    }
    
    public void SetCrackingRadius() {
        var crackingArea = GetNode<Area2D>("CrackingRadius");
        
        foreach (Node child in crackingArea.GetChildren()) {
            if (child is CollisionShape2D oldShape) { oldShape.QueueFree(); } //free the old shape before adding a new one
        }
        
        var collisionShape = new CollisionShape2D();
        var collisionCircle = new CircleShape2D();

        // float radius = Mathf.Max(sprite.Texture.GetWidth(), sprite.Texture.GetHeight()) + 20f;
        float radius = main.GetCrackingSlowRadius();
        collisionCircle.Radius = radius;
        collisionShape.Shape = collisionCircle;
        collisionShape.Position = Vector2.Zero; //center the circle on the Area2D's origin
        crackingArea.AddChild(collisionShape);
    }
    
    private void CanNotCollectTween(Node2D body) {
        var bounceHeight = 30f;
        var bounceDuration = 0.2f;
        Tween.EaseType bounceEaseUp = Tween.EaseType.Out;
        Tween.EaseType bounceEaseDown = Tween.EaseType.In;
        
        if (canNotCollectTween == null || !canNotCollectTween.IsRunning()) { //gets screwy if drop tween is running
            canNotCollectTween = CreateTween();
            float originalY = body.GlobalPosition.Y;
        
            // Bounce upwards
            canNotCollectTween.TweenProperty(body, "global_position:y", originalY - bounceHeight, bounceDuration / 2)
                .SetEase(bounceEaseUp);

            // Bounce back down
            canNotCollectTween.TweenProperty(body, "global_position:y", originalY, bounceDuration / 2)
                .SetEase(bounceEaseDown)
                .SetDelay(bounceDuration / 2); // Start after the upward movement

            canNotCollectTween.Play();
        }
    }
    
    private void CollectCurrency(CurrencySucrose sucrose) { //rename to use actual currency name
        main.ChangeSucrose(sucrose.GetValue());
        sucrose.QueueFree();
    }

    //this method receives the ItemDrop NODE from CollectDrops
    private void CollectExtract(ItemDrop itemDropNode) {
        if (!itemDropNode.GetIsExtract()) {
            CollectMaterial(itemDropNode.GetThisMaterial()); //bad way to do this
            return;
        }
        if (main.GetInventoryFull()) {
            // If inventory is full, and it's an ItemDrop node, play the bounce
            if (itemDropNode.GetExtractData() != null) {
                CanNotCollectTween(itemDropNode); //this needs the ItemDrop node
            }
            return; //don't try to collect if full
        }

        // --- CRUCIAL: Get the BaseExtract DATA from the ItemDrop node ---
        BaseExtract itemData = itemDropNode.GetExtractData();

        if (itemData == null) {
            GD.PushError($"Player.CollectItem: ItemDrop node {itemDropNode.Name} has no BaseExtract data assigned! Cannot collect.");
            itemDropNode.QueueFree(); // Free the problematic node
            return;
        }

        // Now call Main.GainItem with the actual BaseExtract DATA (the resource)
        extractManager.GainExtract(itemData); // Pass the BaseExtract DATA, not the ItemDrop NODE

        // The ItemDrop NODE itself will be QueueFree'd by Main.RemoveItemDropNodeAndData,
        // which is called by Main.GainItem if successfully added to inventory.
    }

    private void CollectMaterial(BaseMaterial material) {
        main.GainMaterial(material, GetMaterialDropAmount());
    }

    private int GetMaterialDropAmount() {
        return 1;
    }
    
    private void CollectDrops() {
        foreach (var body in currenciesInRange) {
            if (IsInstanceValid(body)) {
                Vector2 directionToPlayer = (GlobalPosition - body.GlobalPosition).Normalized();
                float distanceToPlayerSq = body.GlobalPosition.DistanceSquaredTo(GlobalPosition); //for braking
                if (body is RigidBody2D currencyRB) {
                    currencyRB.ApplyCentralForce(directionToPlayer * dropsCollectSpeed);
                    //braking force
                    if (distanceToPlayerSq <= 50f * 50f) {
                        // Apply braking force (opposite to velocity)
                        currencyRB.ApplyCentralForce(-currencyRB.LinearVelocity * 100); //adjust magnitude
                        currencyRB.LinearVelocity = Vector2.Zero;

                        if (body is CurrencySucrose sucrose) { CollectCurrency(sucrose); }
                        else if (body is ItemDrop item) { CollectExtract(item); }
                    }
                } else if (body is Area2D currencyArea) {
                    currencyArea.GlobalPosition += directionToPlayer * dropsCollectSpeed;
                }
    
                //check for pickup distance
                if (body.GlobalPosition.DistanceSquaredTo(GlobalPosition) <= 50f * 50f) {
                    // Potentially a redundant QueueFree if handled in the specific type check
                    body.QueueFree();
                }
            }
        }
    }
    
    private void PickupObjectInRange(Node2D body) {
        if (body.IsInGroup("drops") && !currenciesInRange.Contains(body)) {
            if (body is ItemDrop item && main.GetInventoryFull()) { //don't add item to drops if inventory full
                // GD.Print(item.GetTweenRunning());
                if (!item.GetTweenRunning()) { CanNotCollectTween(body); }
                return;
            }
            currenciesInRange.Add(body);
        }
    }
}