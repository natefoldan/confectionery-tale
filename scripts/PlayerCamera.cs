using Godot;

namespace ConfectioneryTale.scripts;

public partial class PlayerCamera : Camera2D {

	private Vector2 viewportSize;
	private Vector2 currentZoom;
	private float currentZoomX;
	private float currentZoomY;

	// private const float ZOOM_INCREMENT = 0.01f; //origial
	private const float ZOOM_INCREMENT = 0.1f;
	private const float ZOOM_MIN = 0.2f;
	private const float ZOOM_MAX = 0.4f; 
	private float zoomLevel = 1.0f;
	// private bool panning = false;
	
	public override void _Ready() {
		viewportSize = new Vector2(1920, 1080); //get size
		// currentZoomX = 1.0f;
		// currentZoomY = 1.0f;
		// currentZoomX = .3f;
		// currentZoomY = .3f;
		currentZoomX = .25f;
		currentZoomY = .25f;
		
		// currentZoomX = .01f;
		// currentZoomY = .01f;
		
		currentZoom = new Vector2(currentZoomX, currentZoomY);
	}

	public override void _Process(double delta) {
		// Position = new Vector2(1000, 1000);
		Zoom = currentZoom;
		// GD.Print(Zoom);
	}

	public override void _UnhandledInput(InputEvent @event) {
		//panning code
		// if (@event is InputEventMouseButton mouseButtonEvent) {
		// 	if (mouseButtonEvent.ButtonIndex == MouseButton.Left) {
		// 		//start panning only if Ctrl is pressed with LMB
		// 		panning = mouseButtonEvent.Pressed && Input.IsActionPressed("ctrl_down");
		// 		GetTree().GetRoot().SetInputAsHandled();
		// 	}
		// 	else if (mouseButtonEvent.ButtonIndex == MouseButton.Middle) { //middle mouse button
		// 		panning = mouseButtonEvent.Pressed; // Start panning when pressed
		// 		GetTree().GetRoot().SetInputAsHandled();
		// 	}
		// 	else if (mouseButtonEvent.ButtonIndex == MouseButton.Left && !mouseButtonEvent.Pressed) {
		// 		// Stop panning when LMB is released (for Ctrl + LMB panning)
		// 		panning = false;
		// 		GetTree().GetRoot().SetInputAsHandled();
		// 	}
		// 	//might need an additional check here to stop panning 
		// 	//when the middle mouse button is released, similar to the LMB check.
		// }
		// else if (@event is InputEventMouseMotion mouseMotion && panning) {
		// 	// Pan the camera (no need to check for Ctrl if using middle mouse button)
		// 	GlobalPosition -= mouseMotion.Relative / new Vector2(currentZoomX, currentZoomY);
		// 	GetTree().GetRoot().SetInputAsHandled();
		// }

		if (Input.IsActionPressed("zoom_map_in")) {
			// Clamp zoomLevel directly
			zoomLevel = Mathf.Clamp(zoomLevel + ZOOM_INCREMENT, ZOOM_MIN, ZOOM_MAX);
			currentZoom = zoomLevel * Vector2.One; // Apply to the Vector2 for camera
			GetTree().GetRoot().SetInputAsHandled();
		}

		if (Input.IsActionPressed("zoom_map_out")) {
			// Clamp zoomLevel directly
			// zoomLevel = zoomLevel - 1;
			zoomLevel = Mathf.Clamp(zoomLevel - ZOOM_INCREMENT, ZOOM_MIN, ZOOM_MAX); //CURRENT
			currentZoom = zoomLevel * Vector2.One; // Apply to the Vector2 for camera
			GetTree().GetRoot().SetInputAsHandled();
		}
		
		
		// if (Input.IsActionPressed("zoom_map_in")) {
		// 	if (currentZoomX >= ZOOM_MAX) { return; }
		// 	currentZoomX += ZOOM_INCREMENT;
		// 	currentZoomY += ZOOM_INCREMENT;
		// 	zoomLevel = Mathf.Clamp(zoomLevel + ZOOM_INCREMENT, ZOOM_MIN, ZOOM_MAX);
		// 	currentZoom = zoomLevel * Vector2.One;
		// 	GetTree().GetRoot().SetInputAsHandled();
		// }
		//
		// if (Input.IsActionPressed("zoom_map_out")) {
		// 	if (currentZoomX <= ZOOM_MIN) { return; }
		// 	currentZoomX -= ZOOM_INCREMENT;
		// 	currentZoomY -= ZOOM_INCREMENT;
		// 	zoomLevel = Mathf.Clamp(zoomLevel - ZOOM_INCREMENT, ZOOM_MIN, ZOOM_MAX);
		// 	currentZoom = zoomLevel * Vector2.One;
		// 	GetTree().GetRoot().SetInputAsHandled();
		// }
		// currentZoom = new Vector2(currentZoomX, currentZoomY);
		// // GD.Print(currentZoom);
	}

}