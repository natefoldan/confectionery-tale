using Godot;

namespace ConfectioneryTale.scripts;

public partial class Settings : Node {
	[Export] private AudioStreamPlayer musicPlayer;
	[Export] private AudioStreamPlayer soundPlayer;
	private Variables vars;
	private int musicBusVolume;
	private int soundBusVolume;
	
	public override void _Ready() {
		SetupSettings();
	}

	private void SetupSettings() {
		vars = GetNode<Variables>("/root/Variables");
		StartMusic(); //not here
	}

	private void ToggleMusic(bool load) {
		if (!load) {
			vars.MusicOn = !vars.MusicOn;
			if (vars.MusicOn) { StartMusic(); }
			else { StopMusic(); }
		}
		// GetNode<TextureButton>("VBoxContainer/SettingMusic/Toggle").TextureNormal = vars.MusicOn ? toggleOn : toggleOff;
	}

	private void ToggleSound(bool load) {
		if (!load) { vars.SoundOn = !vars.SoundOn; }
		// GetNode<TextureButton>("VBoxContainer/SettingSound/Toggle").TextureNormal = vars.SoundOn ? toggleOn : toggleOff;
	}
	
	private void LoadVolume() {
		AudioServer.SetBusVolumeDb(musicBusVolume, Mathf.LinearToDb(vars.MusicVolume));
		AudioServer.SetBusVolumeDb(soundBusVolume, Mathf.LinearToDb(vars.SoundVolume));
		vars.MusicVolumeSlider = vars.MusicVolume;
		// vars.SoundVolumeSlider = vars.SoundVolume; //prev
		// vars.SoundVolumeSlider = 1;
	}
	
	private void SetMusicVolume(float value) {
		var musicSlider = GetNode<HSlider>("VBoxContainer/SettingMusic/HSliderMusic");
		var linear = Mathf.LinearToDb(value);
		vars.MusicVolume = value;
		if (musicSlider.Value < .001f) { musicSlider.Value = .001f; } 
		vars.MusicVolumeSlider = (float) musicSlider.Value;
		AudioServer.SetBusVolumeDb(musicBusVolume, linear);
	}
	
	private void SetVolumeSliders() {
		GetNode<HSlider>("VBoxContainer/SettingMusic/HSliderMusic").Value = vars.MusicVolumeSlider;
		GetNode<HSlider>("VBoxContainer/SettingSound/HSliderSound").Value = vars.SoundVolumeSlider;
		// SetSoundVolumeDrag(false);
	}
	
	private void SetSoundVolumeDrag(bool value) {
		var soundSlider = GetNode<HSlider>("VBoxContainer/SettingSound/HSliderSound");
		// GD.Print(vars.SoundVolume);
		// GD.Print("slider: " + vars.SoundVolumeSlider);
		if (soundSlider.Value < .001f) { soundSlider.Value = .001f; }
		// soundSlider.Value = vars.SoundVolumeSlider; //dont uncomment
		vars.SoundVolumeSlider = (float) soundSlider.Value;
		var linearValue = Mathf.LinearToDb(soundSlider.Value);
		vars.SoundVolume = (float) linearValue;
		AudioServer.SetBusVolumeDb(soundBusVolume, vars.SoundVolume);
		// PlayLevelSound(); //
	}

	private void StartMusic() {
		if (!vars.MusicOn) { return; }
		musicPlayer.Play();
	}

	private void StopMusic() {
		musicPlayer.Stop();
	}

	private void MusicFinished() {
		StartMusic();
	}
}