using System.Collections.Generic;
using Godot;

namespace ConfectioneryTale.scripts;

public partial class CutsceneManager : CanvasLayer {
    [Signal] public delegate void CutsceneFinishedEventHandler(string assignment);

    // --- Nodes (from old Cutscenes.tscn) ---
    private ColorRect _background;
    private Label _textLabel;
    private Timer _textTimer;
    
    // --- Text Reveal Logic (from old Cutscenes.cs) ---
    private float _charactersPerSecond = 100f;
    private int _currentCharIndex = 0;

    private string _currentLineToDisplay; // The line we are revealing now
    private List<string> _textSequence;     // The full list of text lines
    private int _currentTextIndex;        // Which line we are on
    
    // --- State Variables (from old CutsceneManager.cs) ---
    private Node _targetNode;
    private Vector2 _startPosition;
    private string _whatAssignment;

    public override void _Ready() {
        // Run when the game is paused
        ProcessMode = ProcessModeEnum.Always;

        // Get all the nodes
        _background = GetNode<ColorRect>("ColorRect");
        _textLabel = GetNode<Label>("Label");
        _textTimer = GetNode<Timer>("Timer");
        
        // Connect the timer's timeout signal
        _textTimer.Timeout += OnTextTimerTimeout;
        
        // Start transparent
        // _background.Color = new Color(0, 0, 0, 0); 
        _textLabel.Visible = false;
    }

    public void StartTextOnlyCutscene(params string[] textLines) {
        _textSequence = new List<string>(textLines); //store the text lines as a List
        StartCutscene(null, Vector2.Zero, null, _textSequence.ToArray());
    }
    
    public void StartCutscene(Node targetNode, Vector2 startPosition, string whatAssignment, params string[] textLines) {
        _targetNode = targetNode;
        _startPosition = startPosition;
        _whatAssignment = whatAssignment;
        _textSequence = new List<string>(textLines); 

        // var tween = CreateTween(); //keep -this fades in the scene
        // tween.TweenProperty(_background, "color", new Color(0, 0, 0, 1), 0.5f);
        // tween.Finished += OnFadeInFinished;
        OnFadeInFinished();
    }
    
    private void ShowNextLine() {
        //check if there is more text to show
        if (_textSequence != null && _currentTextIndex < _textSequence.Count) {
            //get the next line
            _currentLineToDisplay = _textSequence[_currentTextIndex];
            _currentTextIndex++; // Move to the next index

            // Prepare for reveal
            _currentCharIndex = 0;
            _textLabel.Visible = true;
        
            // Add two newlines for a paragraph break
            if (!string.IsNullOrEmpty(_textLabel.Text)) {
                _textLabel.Text += "\n\n";
            }
        
            // Start the timer to reveal this line
            _textTimer.WaitTime = 1.0f / _charactersPerSecond;
            _textTimer.Start();
        } else {
            //no more text, end the cutscene
            if (_targetNode != null) {
                // StartFallTween();
            } else {
                StartFadeOutAndFinish();
            }
        }
    }
    
    // --- 2. Fade-in Finished, Start Text ---
    private void OnFadeInFinished() {
        // _textLabel.Visible = true;
        // _currentCharIndex = 0;
        // _textLabel.Text = "";
        //
        // if (!string.IsNullOrEmpty(_fullTextToDisplay)) {
        //     _textTimer.WaitTime = 1.0f / _charactersPerSecond;
        //     _textTimer.Start();
        // } else {
        //     // No text, just skip to the "fall"
        //     StartFallTween();
        // }
        _currentTextIndex = 0;
        ShowNextLine();
    }

    // --- 3. Text Reveal Timer Logic ---
    private void OnTextTimerTimeout() {
        // Check if we are still revealing the *current line*
        if (_currentCharIndex < _currentLineToDisplay.Length) {
            _textLabel.Text += _currentLineToDisplay[_currentCharIndex];
            _currentCharIndex++;
        }
    
        if (_currentCharIndex >= _currentLineToDisplay.Length) {
            // This line is finished. Stop the timer.
            _textTimer.Stop();
        
            // Create a 1.5 second delay
            var timer = GetTree().CreateTimer(1.5f);
        
            // After the delay, call ShowNextLine to show the *next* line
            timer.Timeout += ShowNextLine; 
        } else {
            _textTimer.Start(); // Continue revealing the current line
        }
    }
    
    private void StartFadeOutAndFinish() {
        var fadeOutTween = CreateTween();
    
        // Fade out the black background
        fadeOutTween.TweenProperty(_background, "color", new Color(0, 0, 0, 0), 0.5f);
    
        // Fade out the text
        fadeOutTween.Parallel().TweenProperty(_textLabel, "modulate", new Color(1, 1, 1, 0), 0.3f);
    
        // When the fade is done, call the same final function
        // fadeOutTween.Finished += OnFallTweenFinished;
    }
    
    // --- 4. Text Finished, Start Fall Tween ---
    // private void StartFallTween() {
    //     var fallDistance = 1000;
    //     var fallDuration = 0.5f;
    //
    //     var fallTween = CreateTween();
    //     
    //     fallTween
    //         .TweenProperty(_targetNode, "global_position:y", _startPosition.Y + fallDistance, fallDuration)
    //         .SetEase(Tween.EaseType.In);
    //     
    //     // Also fade out the text while falling
    //     fallTween.Parallel().TweenProperty(_textLabel, "modulate", new Color(1, 1, 1, 0), 0.3f);
    //
    //     fallTween.Finished += OnFallTweenFinished;
    // }
    //
    // // --- 5. Fall Finished, Unpause and Finish ---
    // private void OnFallTweenFinished() {
    //     GetTree().Paused = false;
    //     
    //     // Only emit the signal if we were actually given an assignment
    //     if (!string.IsNullOrEmpty(_whatAssignment)) {
    //         EmitSignal(SignalName.CutsceneFinished, _whatAssignment);
    //     }
    //     
    //     // EmitSignal(SignalName.CutsceneFinished, _whatAssignment); //delete
    //     QueueFree(); // The cutscene is over, remove self
    // }
}