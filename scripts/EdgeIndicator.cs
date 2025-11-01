using Godot;

namespace ConfectioneryTale.scripts;

public partial class EdgeIndicator : Control {
    private Main main;
    private Sprite2D ArrowSprite { get; set; }
    private Vector2 TargetObjectPos { get; set; }
    private string objectId;
    private float EdgePadding = 20.0f;
    private bool indicatorReady;
    private Camera2D playerCamera;
    
    public override void _Ready() {
    }

    public override void _Process(double delta) {
        UpdateDirection();
    }

    public void Setup(Main passedMain, string target) {
        ArrowSprite = GetNode<Sprite2D>("Sprite2D");
        main = passedMain;
        playerCamera = main.GetPlayerCamera();
        objectId = target;
        SetTargetObjectPos();
        // SetIndicatorSprite();
        SetIndicatorObjectSprite();
        indicatorReady = true;
    }

    // private void SetIndicatorSprite() {
    //     var objectTexture = main.GetWorldObjectById(objectId).GetNode<Sprite2D>("Sprite2D").Texture;
    //     GD.Print(ArrowSprite);
    //     ArrowSprite.Texture = objectTexture;
    // }

    private void SetIndicatorObjectSprite() {
        var objectTexture = main.GetWorldObjectById(objectId).GetNode<Sprite2D>("Sprite2D").Texture;
        // ArrowSprite.GetNode<Sprite2D>("ObjectSprite").Texture = objectTexture; //prev

        var targetSize = new Vector2(50, 50);
        
        //original size of the texture ---
        Vector2 originalSize = new Vector2(objectTexture.GetWidth(), objectTexture.GetHeight());

        Vector2 scaleFactor = targetSize / originalSize;

        var sprite = ArrowSprite.GetNode<Sprite2D>("ObjectSprite");
        sprite.Texture = objectTexture;
        sprite.Scale = scaleFactor;
        
    }
    
    private void SetTargetObjectPos() {
        TargetObjectPos = main.GetWorldObjectById(objectId).GlobalPosition;
    }
    
    private void UpdateDirection() {
        if (!indicatorReady) { return; }

        ArrowSprite.Visible = !CheckObjectOnScreen();
        
        //calculate direction vector from PLAYER to TARGET (in world space)
        Vector2 playerGlobalPosition = main.GetPlayerPosition();
        Vector2 targetGlobalPosition = TargetObjectPos;
        Vector2 directionToTargetFromPlayer = targetGlobalPosition - playerGlobalPosition;

        //get the angle of this direction vector
        //Vector2.Angle() returns the angle in radians relative to the positive X-axis (right).
        float angleToTargetRadians = directionToTargetFromPlayer.Angle();

        //rotate the arrow sprite to face the target (relative to player)
        //if sprite points right (0 degrees) by default, directly setting Rotation works.
        ArrowSprite.Rotation = angleToTargetRadians;

        //calculate Arrow Position on Screen Edge ---

        // Vector2 viewportSize = GetViewport().size; //doesn't work
        Vector2 viewportSize = new Vector2(1920, 1080); //probably will break when changing resolution?
        Vector2 screenCenter = viewportSize / 2.0f;
        
        //calculate the effective half-dimensions of the screen rectangle that the arrow's center can touch.
        //subtract padding plus half of the arrow's size so the arrow stays fully on screen.
        float arrowHalfWidth = (ArrowSprite.Texture?.GetWidth() ?? 0) * ArrowSprite.Scale.X / 2.0f;
        float arrowHalfHeight = (ArrowSprite.Texture?.GetHeight() ?? 0) * ArrowSprite.Scale.Y / 2.0f;
        
        float effectiveHalfWidth = (viewportSize.X / 2.0f) - EdgePadding - arrowHalfWidth;
        float effectiveHalfHeight = (viewportSize.Y / 2.0f) - EdgePadding - arrowHalfHeight;

        //ensure effective dimensions don't become negative if padding + arrow size is too large
        effectiveHalfWidth = Mathf.Max(0.1f, effectiveHalfWidth); // Use a tiny minimum to avoid division by zero
        effectiveHalfHeight = Mathf.Max(0.1f, effectiveHalfHeight);

        //get a normalized vector in the direction of the target from the screen center (conceptually)
        Vector2 unitVectorFromCenter = Vector2.FromAngle(angleToTargetRadians);

        //calculate how far along this unit vector we need to go to hit the boundary of the 'safe' inner rectangle.
        //this is done by comparing the ratio of effective screen half-dimensions to the unit vector's components.
        float t; // Parameter 't' for the ray: Center + t * unitVectorFromCenter

        //avoid division by zero if direction is purely horizontal/vertical
        if (Mathf.IsZeroApprox(unitVectorFromCenter.X)) { //vertical direction (straight up/down)
            t = effectiveHalfHeight / Mathf.Abs(unitVectorFromCenter.Y);
        } else if (Mathf.IsZeroApprox(unitVectorFromCenter.Y)) { //horizontal direction (straight left/right)
            t = effectiveHalfWidth / Mathf.Abs(unitVectorFromCenter.X);
        } else { //for diagonal directions, find which edge is hit first
            float tx = effectiveHalfWidth / Mathf.Abs(unitVectorFromCenter.X);
            float ty = effectiveHalfHeight / Mathf.Abs(unitVectorFromCenter.Y);
            t = Mathf.Min(tx, ty); // Take the minimum 't' as that's the first edge hit
        }
        
        // Calculate the final position of the arrow's center
        Vector2 arrowScreenPosition = screenCenter + unitVectorFromCenter * t;
        
        //assign the calculated screen position to the arrow sprite
        //ArrowSprite.Position expects local position if parent is CanvasLayer, which is same as global screen pos
        ArrowSprite.Position = arrowScreenPosition;
        // GD.Print($"Arrow Pos: {ArrowSprite.Position}, Angle: {Mathf.RadToDeg(angleToTargetRadians):0.0} degrees");
    }

    private bool CheckObjectOnScreen() {
        //convert the target's global world position into the camera's local coordinate system. (in the camera's local space, (0,0) is the camera's center)
        Vector2 targetPosRelativeToCamera = playerCamera.ToLocal(TargetObjectPos);
        
        //the camera's viewport is typically centered on its local (0,0), so we define the visible rectangle around (0,0) with its half-dimensions
        var viewportsize = new Vector2(1920, 1080);
        Vector2 viewportSizeInWorldUnits = viewportsize / playerCamera.Zoom;
        Rect2 cameraVisibleRect = new Rect2(
            -viewportSizeInWorldUnits.X / 2.0f, //left edge (negative half-width)
            -viewportSizeInWorldUnits.Y / 2.0f, //top edge (negative half-height)
            viewportSizeInWorldUnits.X, //width
            viewportSizeInWorldUnits.Y //height
        );

        //if the target's position is within the camera's visible rectangle
        if (cameraVisibleRect.HasPoint(targetPosRelativeToCamera)) { return true; }

        return false;
    }

    public void DeleteIndicator() {
        QueueFree();
    }
    
    public string GetObjectId() { return objectId; }
}