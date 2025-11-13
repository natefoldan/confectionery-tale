using System;
using Godot;

namespace ConfectioneryTale.scripts;

public partial class Enemy : CharacterBody2D {
    [Signal] public delegate void EnemyCollidedEventHandler(Area2D body, Vector2 pos);
    private Variables vars;
    private Main main;
    private ExtractManager extractManager;
    private BaseEnemy thisEnemy;
    private PackedScene popupTextScene;
    private AnimationPlayer animationPlayer;
    public string EnemyId { get; private set; } //unique per enemy
    private float speed { get; set; } = 150.0f;
    private float homeSpeed { get; set; } = 300.0f;
    private float wanderSpeed { get; set; }
    private double currentHealth;
    private double maxHealth;
    private int spriteWidth;
    private int spriteHeight;
    private Sprite2D thisSprite;
    // private Area2D collisionArea;
    private int portalFallDistance = 900; //originally 700
    private float homePointTolerance = 3f;
    private Vector2 homePoint;
    // private Vector2 currentPlayerLoc;
    private ProgressBar healthBar;
    private TextureRect statusChill;
    private TextureRect statusMelt;
    private bool enemyReady;
    private float chilledSpeedModifier;
    private float crackingSpeedModifier;
    private float chilledTimer;
    private float meltDuration;
    private int meltTimer;
    private bool meltActive;
    private bool corrupted;
    
    //temp colors -MOVE
    // private string colorGreen = "[color=#1b5e20]";
    // private Color colorWhite = new Color(1f, 1f, 1f);
    private Color colorRed = new Color(.76f, .2f, .2f); //del
    // private Color colorRed = new Color(.089f, .216f, .216f); //not working
    private Color colorYellow = new Color(.953f, .941f, .031f);
    // private Color colorBlack = new Color(0f, 0f, 0f, 1.0f);
    // private Color colorGreenDec = new Color(.1f, .37f, .13f, 1.0f);
    
    //temp
    private float _directionChangeTimer = 0.0f;
    private Vector2 currentWanderDirection = Vector2.Zero;
    
    public override void _Ready() {
        vars = GetNode<Variables>("/root/Variables");
        main = GetNode<Main>("/root/Main");
        extractManager = GetNode<ExtractManager>("/root/ExtractManager");
        SetupEnemy();
    }
    
    public override void _PhysicsProcess(double delta) {
        // MoveAndCollide(velocity * (float)delta);
        if (!enemyReady) { return; }

        UpdateChilled();
        UpdateMelt();
        MeltEnemy();
        
        
        if (vars.IsSheltered) {
            MoveTowardsHome(delta);
            return;
        }

        if (vars.TinctureConcealCooldown > 0) {
            Wander(delta);
            return;
        }
        
        MoveAndTrack(delta);
    }

    public void SetThisEnemy(BaseEnemy what) { thisEnemy = what; } //original
    
    private void SetupEnemy() {
        popupTextScene = GD.Load<PackedScene>("res://scenes/popup_text.tscn");
        thisSprite = GetNode<Sprite2D>("Sprite2D");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        animationPlayer.Play(thisEnemy.animation);
        thisSprite.Texture = thisEnemy.DisplayTexture;
        spriteWidth = thisSprite.Texture.GetWidth();
        spriteHeight = thisSprite.Texture.GetHeight();
        GenerateNewWanderDirection();
        // GD.Print(thisEnemy.Id + ": " + spriteWidth + " | " + spriteHeight);
        speed = GD.RandRange(120, 400); //use this
        // speed = 500; //delete
        SetHitbox(thisSprite);
        SetPhysicsBlocker();
        SetupHealthBar();
        SpawnFromPortal();
        ResetChilled();
        ResetMelt();
        ResetCrackingSlowed();
        EnemyId = System.Guid.NewGuid().ToString();
        // enemyReady = true;
    }

    private void SetupHealthBar() {
        currentHealth = GetMaxhealth();
        maxHealth = GetMaxhealth();
        healthBar = GetNode<ProgressBar>("ProgressBar");
        statusChill = GetNode<TextureRect>("ProgressBar/StatusEffects/Slow");
        statusMelt = GetNode<TextureRect>("ProgressBar/StatusEffects/Melt");
        healthBar.MaxValue = maxHealth;
        healthBar.Value = currentHealth;
        healthBar.Size = new Vector2(spriteWidth, 20);
        healthBar.Position = new Vector2((spriteWidth / 2) * -1, (20 + 8 + (spriteHeight / 2)) * -1); //6 is y offset
        healthBar.Visible = false;
    }

    private double GetMaxhealth() {
        // return 10000; //for testing delete
        var health = Math.Ceiling(Math.Pow(GetEnemyRank(), 1.9) * 10);
        if (corrupted) { health *= 1.2f; }
        return Math.Ceiling(health);
    }
    
    private void SpawnFromPortal() {
        var duration = .3f;
        Tween fallTween = GetTree().CreateTween();
        // fallTween.TweenProperty(GetNode("Sprite2D"), "position", new Vector2(0, 700), .3f); //need to move the characterbody, not the sprite
        fallTween.TweenProperty(this, "global_position", new Vector2(GlobalPosition.X, GlobalPosition.Y + portalFallDistance), duration);
        fallTween.Finished += OnFallTweenFinished;
    }

    private void ReturnIntoPortal() {
        ZIndex = 2;
        var duration = .1f;
        Tween returnTween = GetTree().CreateTween();
        returnTween.TweenProperty(this, "global_position", new Vector2(GlobalPosition.X, GlobalPosition.Y - portalFallDistance), duration);
        // fallTween.Finished += OnReturnTweenFinished;
        // returnTween.TweenCallback(Callable.From(GetNode("Sprite2").QueueFree));
        returnTween.TweenCallback(Callable.From(QueueFree));
    }
    
    private void OnFallTweenFinished() {
        ZIndex = 1;
        homePoint = GlobalPosition;
        enemyReady = true;
    }

    private void SetHitbox(Sprite2D sprite) {
        if (sprite == null) { return; }
        var posY = 0;
        var collisionShape = new CollisionShape2D();
        var boundingBox = new Vector2(spriteWidth / 1f, spriteHeight); //can't use negative if sprite is too small. divide by # of frames
        var collisionRectangle = new RectangleShape2D();
        var collisionArea = GetNode<Area2D>("Hitbox");
        collisionRectangle.Size = boundingBox;
        collisionShape.Shape = collisionRectangle;
        collisionShape.Position = new Vector2(0, posY);
        collisionArea.AddChild(collisionShape);
    }
    
    private void SetPhysicsBlocker() {
        if (thisSprite == null) { return; }
        var posY = 0;
        var collisionShape = new CollisionShape2D();
        var boundingBox = new Vector2(spriteWidth / 1f, spriteHeight); //can't use negative if sprite is too small. divide by # of frames
        var collisionRectangle = new RectangleShape2D();
        collisionRectangle.Size = boundingBox;
        collisionShape.Shape = collisionRectangle;
        collisionShape.Position = new Vector2(0, posY);
        AddChild(collisionShape);
    }
    

    private void GenerateNewWanderDirection() {
        // Get a random angle in radians (0 to 2*PI)
        float randomAngle = GD.Randf() * Mathf.Pi * 2.0f;
    
        // Convert angle to a normalized Vector2
        currentWanderDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
    
        // Reset the timer for the next change
        _directionChangeTimer = 0.0f;
        wanderSpeed = GD.RandRange(100, 200);
        // GD.Print($"Enemy {Name} new wander direction: {_currentWanderDirection}");
    }
    
    private void Wander(double delta) {
        // Update the timer
        _directionChangeTimer += (float)delta;
    
        // Check if it's time to change direction
        if (_directionChangeTimer >= GD.RandRange(2f, 4f)) {
            GenerateNewWanderDirection(); // Get a new random direction
        }
    
        // Use the stored current wander direction
        Vector2 direction = currentWanderDirection;
    
        // Vector2 velocity = direction * 100;
        Vector2 velocity = direction * wanderSpeed * chilledSpeedModifier * crackingSpeedModifier;
        MoveAndCollide(velocity * (float)delta);
    
        if (direction.X < 0) { thisSprite.FlipH = false; }
        else if (direction.X > 0) { thisSprite.FlipH = true; }
    }
    
    private void MoveAndTrack(double delta) {
        // Vector2 playerPosition = main.GetPlayerPosition(); //original -laggy
        Vector2 playerPosition = main.PlayerGlobalPosition;

        //calculate the direction vector from the enemy to the player
        Vector2 direction = (playerPosition - Position).Normalized();

        //calculate the desired velocity
        Vector2 velocity = direction * (speed * chilledSpeedModifier * crackingSpeedModifier); //no need to multiply by delta here for MoveAndCollide
        MoveAndCollide(velocity * (float)delta);

        if (direction.X < 0) { thisSprite.FlipH = false; }
        else if (direction.X > 0) { thisSprite.FlipH = true; }
        
        // Rotation = velocity.Angle(); //no rotation here
    }
    
    private void MoveTowardsHome(double delta) {
        if (GlobalPosition.DistanceSquaredTo(homePoint) <= homePointTolerance * homePointTolerance) {
            ReturnIntoPortal();
            return;
        }
        Vector2 direction = (homePoint - Position).Normalized();
        // Vector2 velocity = direction * speed; //before speed mod
        Vector2 velocity = direction * (homeSpeed * chilledSpeedModifier * crackingSpeedModifier);
        MoveAndCollide(velocity * (float)delta);
        
        if (direction.X < 0) { thisSprite.FlipH = false; }
        else if (direction.X > 0) { thisSprite.FlipH = true; }
        
        // Rotation = velocity.Angle();
    }

    private BaseMaterial GetItemDrop() {
        var chance = GetEnemyRank();
        if (chance > 40) { chance = 40; }

        // chance = 100; //fore testing -delete
        
        //check essence drop
        if (thisEnemy.ItemDrops != null && thisEnemy.ItemDrops.Length > 0) {
            var itemDropRoll = GD.RandRange(1, 100);
        
            // You had "if (itemDrop <= chance) { return null; }"
            // This means a roll *over* the chance is a success, so let's check that
            if (itemDropRoll <= chance) {
                int randomIndex = GD.RandRange(0, thisEnemy.ItemDrops.Length - 1);
                BaseMaterial specificDrop = thisEnemy.ItemDrops[randomIndex] as BaseMaterial;
                if (specificDrop != null) { return specificDrop; } //specific drop success
            }
        }
    
        //no specific drop -attempt global drop
        var globalDrops = main.GlobalMaterialDrops; 
        if (globalDrops == null || globalDrops.Count == 0) { return null; }

        var globalDropRoll = GD.RandRange(0.0, 100.0);
    
        if (globalDropRoll <= chance) {
            int randomIndex = GD.RandRange(0, globalDrops.Count - 1);
            return globalDrops[randomIndex]; //random drop from global list
        }
        return null;
    }
    
    private void EnemyDied() {
        main.GainPlayerExp(GetExpDrop());
        main.SpawnCurrencySucrose(GlobalPosition, GetSucroseDrop());
        if (!corrupted) {
            extractManager.SpawnExtractDrop(GlobalPosition, GetEnemyRank());
            extractManager.SpawnMaterialDrop(GlobalPosition, GetItemDrop());
        }
        QueueFree();
    }

    private int GetEnemyRank() {
        return thisEnemy.Rank + main.GetAreaRank(thisEnemy.SpawnArea.ToString());
    }
    
    private double GetSucroseDrop() {
        var sucrose = Math.Pow(GetEnemyRank(), 2.4);
        if (corrupted) { sucrose *= .5f; }
        return Math.Ceiling(sucrose);
    }
    
    private double GetExpDrop() {
        var exp = Math.Pow(GetEnemyRank(), 1.4);
        if (corrupted) { exp *= .5f; }
        return Math.Ceiling(exp);
    }
    
    private void BulletHitEnemy(double damage, WeaponData weaponData, Area2D area, bool crit, Bullet bullet) {
        //check if the bullet has already processed this enemy
        if (bullet.HasAlreadyHit(EnemyId)) {
            //the bullet has already hit this enemy once, apply piercing damage, but do not apply knockback again.
            currentHealth -= damage;
            SpawnDamageText(damage, false, colorYellow);
        } else { //this is the first time this bullet has hit this enemy
            currentHealth -= damage;
            SpawnDamageText(damage, false, colorYellow);
            bullet.AddEnemyHit(EnemyId); //tell the bullet it has now hit this enemy
            KnockbackEnemy(area, false); //call knockback logic only on the first hit
        }
        
        CheckChill();
        CheckMelt(); //rename to update dot?
        if (currentHealth < 1) { EnemyDied(); }
        UpdateHealthBar();
    }
    
    //slow
    private void CheckChill() {
        if (main.GetPlayerChillDuration() < 1) { return; }
        ActivateChilled();
    }
    
    private void UpdateChilled() {
        if (chilledSpeedModifier == 1.0f) { return; }
        chilledTimer--;
        if (chilledTimer > 0) { return; }
        ResetChilled();
    }
    
    private void ActivateChilled() {
        if(chilledTimer >  0) { return; }
        chilledSpeedModifier = .2f;
        chilledTimer = main.GetPlayerChillDuration() * 60; //can be modded
        statusChill.Visible = true;
    }
    
    private void ResetChilled() {
        chilledSpeedModifier = 1;
        statusChill.Visible = false;
        chilledTimer = 0;
    }

    private void ResetCrackingSlowed() {
        crackingSpeedModifier = 1;
    }
    
    //melt
    private void CheckMelt() {
        if (main.GetPlayerMeltDuration() < 1) { return; }
        ActivateMelt();
    }
    
    private void MeltEnemy() {
        if (!meltActive) { return; }

        meltTimer--;
        if(meltTimer > 0) { return; }

        meltTimer = 60;

        var meltDamage = main.GetPlayerMeltDamage();
        
        var critHit = false;
        var rollCrit = GD.RandRange(1, 100);
        if (rollCrit <= main.GetPlayerFinalCritChance()) {
            meltDamage *= main.GetPlayerFinalCritDamage() / 100;
            critHit = true;
        }
        
        currentHealth -= meltDamage;
        SpawnDamageText(meltDamage, critHit, colorRed);
        if (currentHealth < 1) { EnemyDied(); }
        UpdateHealthBar();
    }
    
    private void UpdateMelt() {
        if (!meltActive) { return; }
        meltDuration--;
        if (meltDuration > 0) { return; }
        ResetMelt();
    }
    
    private void ResetMelt() {
        meltActive = false;
        statusMelt.Visible = false;
        meltDuration = 0;
    }
    
    private void ActivateMelt() {
        meltActive = true;
        meltDuration = main.GetPlayerMeltDuration() * 60;
        statusMelt.Visible = true;
        meltTimer = 60;
    }
    
    private void SpawnDamageText(double amount, bool critHit, Color color) {
        if (amount == 0) { return; }

        var size = 128;
        if (critHit) { size = 320; }
		  
        // var text = main.TrimNumber(Math.Abs(amount));
        var text = Math.Abs(amount).ToString();
        // var pos = new Vector2(0, (-spriteHeight - 50));
        
        var randXPos = GD.RandRange(-spriteWidth + 100, spriteWidth - 100);
        randXPos = 0;
        // GD.Print(randXPos);
        // randXPos = -spriteWidth;
        // GD.Print(GetNode<Sprite2D>("Sprite2D").Offset.X);
        // var randYPos = GD.RandRange((-spriteHeight - 5), (-spriteHeight - 10));
        var randYPos = GD.RandRange((-spriteHeight +20), (-spriteHeight));
        // var pos = new Vector2(randXPos, randYPos);
        
        var popupText = popupTextScene.Instantiate<PopupText>();
        // popupText.SetColor(colorRed); //might need
        popupText.SetColor(color);
    
        // if (amount > 0) {
        //     text = "+" + main.TrimNumber(amount);
        //     popupText.SetColor(colorGreenDec);
        // }
    
        //         var randX = GD.RandRange(450, 520);
        //         var randY = GD.RandRange(160, 190);
        //         // pos = new Vector2(480, 180);
        //         pos = new Vector2(randX, randY);
        //         if (amount > 0) { pos = new Vector2(500, 180); }
        //         if (crit) {
        //             size = 48;
        //             crit = false;
        //         }
        
        var gpx = GlobalPosition.X + randXPos;
        var gpy = GlobalPosition.Y + randYPos;
        // GD.Print(gpx + " | " + gpy);
        
        popupText.SetTextAndSize(text, size);
        // popupText.SetPosition(pos); //current -set on enemy
        // AddChild(popupText); //current -set on enemy
        popupText.SetWorldPosition(new Vector2(gpx, gpy));
        main.GetWorld().AddChild(popupText);
        popupText.DamageTween();
    }
    
    private void KnockbackEnemy(Area2D area, bool player) {
        if (main.GetPlayerFinalKnockback() < 1 && !player) { return; }
        
        // Get the global position of the bullet at impact
        Vector2 bulletHitPosition = area.GlobalPosition;

        Rect2 localRect = GetNode<Sprite2D>("Sprite2D").GetRect();
        Transform2D globalTransform = GetNode<Sprite2D>("Sprite2D").GlobalTransform;
        Rect2 globalRect = globalTransform * localRect;

        Vector2 enemyCenter = globalRect.GetCenter();
        Vector2 enemySize = globalRect.Size;
        
        // GD.Print("hit: " + bulletHitPosition);
        // GD.Print(enemyCenter);
        
        // Calculate the relative hit position from the enemy's center
        Vector2 relativeHit = bulletHitPosition - enemyCenter;

        // Normalize the relative hit by half the enemy's size
        // Vector2 normalizedHit = new Vector2(relativeHit.X / (enemyRect.Size.X / 2), relativeHit.Y / (enemyRect.Size.Y / 2));
        Vector2 normalizedHit = new Vector2(relativeHit.X / (enemySize.X / 2), relativeHit.Y / (enemySize.Y / 2));
        
        // GD.Print($"Normalized Hit: X={normalizedHit.X}, Y={normalizedHit.Y}");
        
        //determine the hit side based on thresholds
        float horizontalThreshold = .5f;
        float verticalThreshold = .5f;

        Vector2 pushDirection = Vector2.Zero;
        
        if (normalizedHit.X < -horizontalThreshold) {
            // GD.Print("Hit Left");
            pushDirection = Vector2.Right;
        } else if (normalizedHit.X > horizontalThreshold) {
            // GD.Print("Hit Right");
            pushDirection = Vector2.Left;
        } else if (normalizedHit.Y < -verticalThreshold) {
            // GD.Print("Hit Top");
            pushDirection = Vector2.Down;
        } else if (normalizedHit.Y > verticalThreshold) {
            // GD.Print("Hit Bottom");
            pushDirection = Vector2.Up;
        } else {
            GD.Print("Hit Center");
        }
        
        var pushForce = main.GetPlayerFinalKnockback();
        if (player) { pushForce = 10; }
        Position += pushDirection * pushForce * 10; //adjust multiplier for strength
    }

    private void UpdateHealthBar() {
        if (!healthBar.Visible) { healthBar.Visible = true; }

        healthBar.Value = currentHealth;

    }
    
    private void CollisionAreaEntered(Area2D area) {
        // GD.Print("enemy collided with something");
        if (area.IsInGroup("shelter")) {
            QueueFree();
            return;
        }
        
        if (area.IsInGroup("bullets")) {
            //directly cast the 'area' to the 'Bullet' class
            if (area is not Bullet bullet) {
                GD.PrintErr("Collision with a non-bullet in the 'bullets' group.");
                return;
            }

            var healthOnHit = main.GetPlayerFinalHealthOnHit();
            if (healthOnHit > 0) { main.PlayerRestoreHealthOnHit(healthOnHit); }
            
            var finalDamage = main.GetPlayerFinalDamage();
            var critHit = false;
            var rollCrit = GD.RandRange(1, 100);
            // GD.Print("chance: " + main.GetPlayerFinalCritChance() + " roll: " + rollCrit);
            if (rollCrit <= main.GetPlayerFinalCritChance()) {
                finalDamage *= main.GetPlayerFinalCritDamage();
                // GD.Print("crit hit: " + finalDamage);
                critHit = true;
            }
            
            finalDamage = Math.Ceiling(finalDamage);
            
            var rollInstant = GD.RandRange(1, 100);
            if (rollInstant <= main.GetPlayerFinalInstantKillChance()) { finalDamage = GetMaxhealth(); }
            
            BulletHitEnemy(finalDamage, main.GetCurrentWeaponData(), area, critHit, bullet);
            return;
        }
        
        if (area.IsInGroup("player")) {
            KnockbackEnemy(area, true);
            main.EnemyCollidedWithPlayer(thisEnemy);
            // EmitSignal(SignalName.EnemyCollided, area, GlobalPosition); // Use GlobalPosition for hit position
        }
        
        if (area.IsInGroup("slowers")) {
            if (vars.IsCracking) { crackingSpeedModifier = .3f; }
        }
    }

    private void CollisionAreaExited(Area2D area) {
        if (area.IsInGroup("slowers")) {
            if (vars.IsCracking) { ResetCrackingSlowed(); }
        }
    }
}