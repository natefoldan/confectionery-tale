using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Godot;

namespace ConfectioneryTale.scripts;

public partial class Skills : Control {
    private Main main;
    private Variables vars;
    private UI ui;
    private TooltipHandler tooltips;
    private Assignments assignments;
    private Texture2D skillBoxTextureNormal;
    private Texture2D skillBoxTextureDisabled;
    private Texture2D buttonRemoveTextureNormal;
    private Texture2D buttonRemoveTextureDisabled;

    private TextureProgressBar crackingBar;
    private Label crackingLevelLabel;
    private RichTextLabel crackingEffectsLabel;
    
    private Label campingLevelLabel;
    private RichTextLabel campingEffectsLabel;
    private Label campingInfoLabel;
    
    private Control softenerSkills;
    private Control spreaderSkills;
    private Control sniperSkills;
    private Control slowerSkills;
    private Control smasherSkills;
    
    private TextureButton buttonRemoveOne;
    private TextureButton buttonRemoveAll;
    
    private TextureButton skillButtonSprayerOne;
    private TextureButton skillButtonSprayerTwo;
    private TextureButton skillButtonSprayerThree;
    private TextureButton skillButtonSprayerFour;
    
    private TextureButton skillButtonSoftenerOne;
    private TextureButton skillButtonSoftenerTwo;
    private TextureButton skillButtonSoftenerThree;
    private TextureButton skillButtonSoftenerFour;
    
    private TextureButton skillButtonSpreaderOne;
    private TextureButton skillButtonSpreaderTwo;
    private TextureButton skillButtonSpreaderThree;
    private TextureButton skillButtonSpreaderFour;
    
    private TextureButton skillButtonSniperOne;
    private TextureButton skillButtonSniperTwo;
    private TextureButton skillButtonSniperThree;
    private TextureButton skillButtonSniperFour;
    
    private TextureButton skillButtonSlowerOne;
    private TextureButton skillButtonSlowerTwo;
    private TextureButton skillButtonSlowerThree;
    private TextureButton skillButtonSlowerFour;
    
    private TextureButton skillButtonSmasherOne;
    private TextureButton skillButtonSmasherTwo;
    private TextureButton skillButtonSmasherThree;
    private TextureButton skillButtonSmasherFour;
    
    private List<TextureButton> allSkillButtons;
    private List<SkillsData> skillsDataList;
    private Label availablePointsLabel;
    private Label removePointsLabel;
    private Label removeAllCostLabel;
    private Label currentSucroseLabel;
    private Label removeConfirmationCostLabel;
    private TextureRect removeConfirmation;
    private bool removingPoints;

    public override void _Ready() {
        vars = GetNode<Variables>("/root/Variables");
        tooltips = GetNode<TooltipHandler>("/root/TooltipHandler");
        ui = GetNode<UI>("/root/Main/UI");
        main = GetNode<Main>("/root/Main");
        assignments = GetNode<Assignments>("/root/Main/UI/MainMenu/Assignments");
        main.SucroseChanged += HandleSucroseChanged;
        SetupWeaponSkills();
        SetupWorldSkills();
    }

    private void SetupWorldSkills() {
        crackingBar = GetNode<TextureProgressBar>("World/GridContainer/Cracking/TextureProgressBar");
        crackingLevelLabel = GetNode<Label>("World/GridContainer/Cracking/Level");
        crackingEffectsLabel = GetNode<RichTextLabel>("World/GridContainer/Cracking/Effects");
        
        campingLevelLabel = GetNode<Label>("World/GridContainer/Camping/Level");
        campingEffectsLabel = GetNode<RichTextLabel>("World/GridContainer/Camping/Effects");
        campingInfoLabel = GetNode<Label>("World/GridContainer/Camping/TextureProgressBar/Label");
    }
    
    private void SetupWeaponSkills() {
        skillBoxTextureNormal = GD.Load<Texture2D>("res://assets/box-teal-100x90.png");
        skillBoxTextureDisabled = GD.Load<Texture2D>("res://assets/box-teal-100x90-concave.png");
        buttonRemoveTextureNormal = GD.Load<Texture2D>("res://assets/btn-blue-248x65.png");
        buttonRemoveTextureDisabled = GD.Load<Texture2D>("res://assets/btn-blue-248x65-disaabled.png");
        availablePointsLabel = GetNode<Label>("Weapon/TextureRect/AvailableLabel");
        removePointsLabel = GetNode<Label>("Weapon/TextureRect/RemoveLabel");
        removeAllCostLabel = GetNode<Label>("Weapon/RemoveAll/Cost/Label");
        currentSucroseLabel = GetNode<Label>("Weapon/CurrentSucrose/Label");
        removeConfirmation = GetNode<TextureRect>("Weapon/RemoveConfirm");
        removeConfirmationCostLabel = GetNode<Label>("Weapon/RemoveConfirm/RemoveLabel");

        allSkillButtons = new List<TextureButton>();

        softenerSkills = GetNode<Control>("Weapon/Types/Softener");
        spreaderSkills = GetNode<Control>("Weapon/Types/Spreader");
        sniperSkills = GetNode<Control>("Weapon/Types/Sniper");
        slowerSkills = GetNode<Control>("Weapon/Types/Slower");
        smasherSkills = GetNode<Control>("Weapon/Types/Smasher");
        
        buttonRemoveOne = GetNode<TextureButton>("Weapon/RemoveOne");
        buttonRemoveAll = GetNode<TextureButton>("Weapon/RemoveAll");

        skillButtonSprayerOne = GetNode<TextureButton>("Weapon/Types/Sprayer/GridContainer/SprayerDamage");
        skillButtonSprayerTwo = GetNode<TextureButton>("Weapon/Types/Sprayer/GridContainer/SprayerCritChance");
        skillButtonSprayerThree = GetNode<TextureButton>("Weapon/Types/Sprayer/GridContainer/SprayerKnockback");
        skillButtonSprayerFour = GetNode<TextureButton>("Weapon/Types/Sprayer/GridContainer/SprayerMelt");

        skillButtonSoftenerOne = GetNode<TextureButton>("Weapon/Types/Softener/GridContainer/SoftenerDamage");
        skillButtonSoftenerTwo = GetNode<TextureButton>("Weapon/Types/Softener/GridContainer/SoftenerPierce");
        skillButtonSoftenerThree = GetNode<TextureButton>("Weapon/Types/Softener/GridContainer/SoftenerChill");
        skillButtonSoftenerFour = GetNode<TextureButton>("Weapon/Types/Softener/GridContainer/SoftenerSize");
        
        skillButtonSpreaderOne = GetNode<TextureButton>("Weapon/Types/Spreader/GridContainer/SpreaderDamage");
        skillButtonSpreaderTwo = GetNode<TextureButton>("Weapon/Types/Spreader/GridContainer/SpreaderBullets");
        skillButtonSpreaderThree = GetNode<TextureButton>("Weapon/Types/Spreader/GridContainer/SpreaderKnockback");
        skillButtonSpreaderFour = GetNode<TextureButton>("Weapon/Types/Spreader/GridContainer/SpreaderReload");
        
        skillButtonSniperOne = GetNode<TextureButton>("Weapon/Types/Sniper/GridContainer/SniperDamage");
        skillButtonSniperTwo = GetNode<TextureButton>("Weapon/Types/Sniper/GridContainer/SniperInstant");
        skillButtonSniperThree = GetNode<TextureButton>("Weapon/Types/Sniper/GridContainer/SniperReload");
        skillButtonSniperFour = GetNode<TextureButton>("Weapon/Types/Sniper/GridContainer/SniperRestore");
        
        skillButtonSlowerOne = GetNode<TextureButton>("Weapon/Types/Slower/GridContainer/SlowerDamage");
        skillButtonSlowerTwo = GetNode<TextureButton>("Weapon/Types/Slower/GridContainer/SlowerSize");
        skillButtonSlowerThree = GetNode<TextureButton>("Weapon/Types/Slower/GridContainer/SlowerMelt");
        skillButtonSlowerFour = GetNode<TextureButton>("Weapon/Types/Slower/GridContainer/SlowerReload");
        
        skillButtonSmasherOne = GetNode<TextureButton>("Weapon/Types/Smasher/GridContainer/SmasherDamage");
        skillButtonSmasherTwo = GetNode<TextureButton>("Weapon/Types/Smasher/GridContainer/SmasherChill");
        skillButtonSmasherThree = GetNode<TextureButton>("Weapon/Types/Smasher/GridContainer/SmasherKnockback");
        skillButtonSmasherFour = GetNode<TextureButton>("Weapon/Types/Smasher/GridContainer/SmasherMelt");
        
        allSkillButtons.Add(skillButtonSprayerOne);
        allSkillButtons.Add(skillButtonSprayerTwo);
        allSkillButtons.Add(skillButtonSprayerThree);
        allSkillButtons.Add(skillButtonSprayerFour);
        
        allSkillButtons.Add(skillButtonSoftenerOne);
        allSkillButtons.Add(skillButtonSoftenerTwo);
        allSkillButtons.Add(skillButtonSoftenerThree);
        allSkillButtons.Add(skillButtonSoftenerFour);

        allSkillButtons.Add(skillButtonSpreaderOne);
        allSkillButtons.Add(skillButtonSpreaderTwo);
        allSkillButtons.Add(skillButtonSpreaderThree);
        allSkillButtons.Add(skillButtonSpreaderFour);
        
        allSkillButtons.Add(skillButtonSniperOne);
        allSkillButtons.Add(skillButtonSniperTwo);
        allSkillButtons.Add(skillButtonSniperThree);
        allSkillButtons.Add(skillButtonSniperFour);
        
        allSkillButtons.Add(skillButtonSlowerOne);
        allSkillButtons.Add(skillButtonSlowerTwo);
        allSkillButtons.Add(skillButtonSlowerThree);
        allSkillButtons.Add(skillButtonSlowerFour);
        
        allSkillButtons.Add(skillButtonSmasherOne);
        allSkillButtons.Add(skillButtonSmasherTwo);
        allSkillButtons.Add(skillButtonSmasherThree);
        allSkillButtons.Add(skillButtonSmasherFour);
        
        foreach (var skillButton in allSkillButtons) {
            skillButton.Pressed += () => OnSkillButtonPressed(skillButton.Name);
            skillButton.MouseEntered += () => ShowWeaponSkillTooltip(skillButton.Name);
            skillButton.MouseExited += () => HideWeaponSkillTooltip(skillButton.Name);
            HideWeaponSkillTooltip(skillButton.Name);
        }

        BuildSkillsData();
    }

    private void HandleSucroseChanged() {
        if (!ui.GetMainMenuVisible()) { return; }
        currentSucroseLabel.Text = ui.TrimNumber(vars.CurrentSucrose);
        if (vars.CurrentSucrose < GetRemoveAllCost()) { removeConfirmation.Visible = false; }
        UpdateWeaponSkillsUI();
    }
    
    public void RefreshSkillsPanel() {
        CancelRemovePoints();
        CheckSkillButtons();
        UpdateWeaponSkillsUI();
        UpdateWorldSkillsUI();
    }

    //world skills
    public void UpdateWorldSkillsUI() { //make private
        var colorString = tooltips.GetStringColor("green");
        
        crackingLevelLabel.Text = $"Cracking\nLevel {vars.SkillCrackingLevel}";
        crackingEffectsLabel.Text = $"Unlock Speed: {colorString}{(main.GetCrackingSpeed() * 100):F0}%[/color]" +
                                    $"\nEnemy Slow Radius: {colorString}{main.GetCrackingSlowRadius()}m";
        
        campingLevelLabel.Text = $"Camping\nLevel {vars.SkillCampingLevel}";
        campingEffectsLabel.Text = $"Build Time: {colorString}{main.GetTentBuildTime()} sec[/color]" +
                                   $"\nCooldown: {colorString}{main.GetTentBuildCooldown():F1} min";
        if (vars.SkillCampingLevel >= 10) { campingInfoLabel.Visible = false; }
        UpdateExpBars();
    }

    private void UpdateExpBars() {
        var next = main.GetCrackingExpNext();
        crackingBar.MaxValue = next;
        crackingBar.Value = vars.SkillCrackingCurrentExp;
        crackingBar.GetNode<Label>("Label").Text = "EXP " + vars.SkillCrackingCurrentExp + "/" + next;
    }
    
    //weapon skills
    private void CancelRemovePoints() {
        removePointsLabel.Visible = false;
        removeConfirmation.Visible = false;
        removingPoints = false;
        buttonRemoveOne.TextureNormal = buttonRemoveTextureNormal; //not complete. button won't reset properly
    }
    
    private void ShowWeaponSkillTooltip(string skillName) {
        foreach (var skillButton in allSkillButtons) {
            if (!skillButton.Name.Equals(skillName)) { continue; }

            var tooltip = skillButton.GetNode<TextureRect>("Tooltip");
            
            string namePattern = "([a-z])([A-Z])";
            string name = Regex.Replace(skillName, namePattern, "$1 $2");
            tooltip.GetNode<Label>("Name").Text = name.ToUpper();
            
            tooltip.GetNode<RichTextLabel>("Description").Text = GetWeaponSkillDescription(skillName);
            // tooltip.GetNode<RichTextLabel>("Description").Text = GetSkillData(skillButton.Name).Description; //not used
            // tooltip.GetNode<RichTextLabel>("Costs").Text = "Point Cost\n" + GetWeaponSkillCostsString(skillName); //delete
            tooltip.GetNode<RichTextLabel>("Costs").Text = GetWeaponSkillCostsString(skillName);
            tooltip.Visible = true;
        }
    }

    private void HideWeaponSkillTooltip(string skillName) {
        foreach (var skillButton in allSkillButtons) {
            if (!skillButton.Name.Equals(skillName)) { continue; }
            skillButton.GetNode<TextureRect>("Tooltip").Visible = false;
        }
    }

    private void OnSkillButtonPressed(string clickedButtonName) {
        if (removingPoints) { RemoveSkillPoint(clickedButtonName); }
        else { AddSkillPoint(clickedButtonName); }
    }

    private void AddSkillPoint(string skillName) {
        var skillData = GetSkillData(skillName);
        if (skillData.Current >= skillData.Max) { return; } //if not enough skill points in general

        var nextCost = skillData.Costs[skillData.Current];
        if (GetAvailableSkillPoints() < nextCost) { return; } //if not enough skill points for this specific skill

        CompleteAddOrRemovePoints(skillData, skillName, 1);
    }

    private void RemoveSkillPoint(string skillName) {
        var skillData = GetSkillData(skillName);
        if (skillData.Current <= 0) {
            return;
        } //no points allocated

        SpendRemovePointSucrose(100);
        CompleteAddOrRemovePoints(skillData, skillName, -1);
        if ((vars.CurrentSucrose - 100) < 100) {
            removingPoints = true; //bad way to do this -forces false
            ToggleRemoveOnePoint();
        }
    }
    
    private void CompleteAddOrRemovePoints(SkillsData skillData, string skillName, int number) {
        //number needs new name
        switch (skillData.Skill) {
            case "SprayerDamage":
                vars.SkillSprayerDamage += 1 * number;
                skillData.Current = vars.SkillSprayerDamage;
                break;
            case "SprayerCritChance":
                vars.SkillSprayerCritChance += 1 * number;
                skillData.Current = vars.SkillSprayerCritChance;
                break;
            case "SprayerKnockback":
                vars.SkillSprayerKnockback += 1 * number;
                skillData.Current = vars.SkillSprayerKnockback;
                break;
            case "SprayerMelt":
                vars.SkillSprayerMelt += 1 * number;
                skillData.Current = vars.SkillSprayerMelt;
                break;
            
            case "SoftenerDamage":
                vars.SkillSoftenerDamage += 1 * number;
                skillData.Current = vars.SkillSoftenerDamage;
                break;
            case "SoftenerPierce":
                vars.SkillSoftenerPierce += 1 * number;
                skillData.Current = vars.SkillSoftenerPierce;
                break;
            case "SoftenerChill":
                vars.SkillSoftenerChill += 1 * number;
                skillData.Current = vars.SkillSoftenerChill;
                break;
            case "SoftenerSize":
                vars.SkillSoftenerSize += 1 * number;
                skillData.Current = vars.SkillSoftenerSize;
                break;
                        
            case "SpreaderDamage":
                vars.SkillSpreaderDamage += 1 * number;
                skillData.Current = vars.SkillSpreaderDamage;
                break;
            case "SpreaderBullets":
                vars.SkillSpreaderBullets += 1 * number;
                skillData.Current = vars.SkillSpreaderBullets;
                break;
            case "SpreaderKnockback":
                vars.SkillSpreaderKnockback += 1 * number;
                skillData.Current = vars.SkillSpreaderKnockback;
                break;
            case "SpreaderReload":
                vars.SkillSpreaderReload += 1 * number;
                skillData.Current = vars.SkillSpreaderReload;
                break;
                        
            case "SniperDamage":
                vars.SkillSniperDamage += 1 * number;
                skillData.Current = vars.SkillSniperDamage;
                break;
            case "SniperInstant":
                vars.SkillSniperInstant += 1 * number;
                skillData.Current = vars.SkillSniperInstant;
                break;
            case "SniperReload":
                vars.SkillSniperReload += 1 * number;
                skillData.Current = vars.SkillSniperReload;
                break;
            case "SniperRestore":
                vars.SkillSniperRestore += 1 * number;
                skillData.Current = vars.SkillSniperRestore;
                break;
                        
            case "SlowerDamage":
                vars.SkillSlowerDamage += 1 * number;
                skillData.Current = vars.SkillSlowerDamage;
                break;
            case "SlowerSize":
                vars.SkillSlowerSize += 1 * number;
                skillData.Current = vars.SkillSlowerSize;
                break;
            case "SlowerMelt":
                vars.SkillSlowerMelt += 1 * number;
                skillData.Current = vars.SkillSlowerMelt;
                break;
            case "SlowerReload":
                vars.SkillSlowerReload += 1 * number;
                skillData.Current = vars.SkillSlowerReload;
                break;
            
            case "SmasherDamage":
                vars.SkillSmasherDamage += 1 * number;
                skillData.Current = vars.SkillSmasherDamage;
                break;
            case "SmasherChill":
                vars.SkillSmasherChill += 1 * number;
                skillData.Current = vars.SkillSmasherChill;
                break;
            case "SmasherKnockback":
                vars.SkillSmasherKnockback += 1 * number;
                skillData.Current = vars.SkillSmasherKnockback;
                break;
            case "SmasherMelt":
                vars.SkillSmasherMelt += 1 * number;
                skillData.Current = vars.SkillSmasherMelt;
                break;
        }
        vars.SaveGameData();
        ShowWeaponSkillTooltip(skillName); //update the tooltip being shown
        CheckSkillButtons();
        UpdateWeaponSkillsUI();
    }

    private void UpdateWeaponSkillsUI() {
        if (!ui.GetMainMenuVisible()) { return; }
        availablePointsLabel.Text = "Available " + GetAvailableSkillPoints() + "/" + GetMaxSkillPoints();
        removeAllCostLabel.Text = GetRemoveAllCost().ToString("N0");
        buttonRemoveAll.Disabled = vars.CurrentSucrose < GetRemoveAllCost();
        if (GetRemoveAllCost() < 100) { buttonRemoveAll.Disabled = true; }
        buttonRemoveOne.Disabled = vars.CurrentSucrose < 100;

        softenerSkills.Visible = vars.SoftenerOwned;
        spreaderSkills.Visible = vars.SpreaderOwned;
        sniperSkills.Visible = vars.SniperOwned;
        slowerSkills.Visible = vars.SlowerOwned;
        smasherSkills.Visible = vars.SmasherOwned;
    }

    private void ToggleRemoveOnePoint() {
        removingPoints = !removingPoints;
        buttonRemoveOne.TextureNormal = removingPoints ? buttonRemoveTextureDisabled : buttonRemoveTextureNormal;
        removePointsLabel.Visible = removingPoints;
    }

    private void ToggleRemoveAllPoints() {
        removeConfirmationCostLabel.Text = $"REMOVE ALL SKILL POINTS?\nCost: {GetRemoveAllCost():N0}";
        removeConfirmation.Visible = !removeConfirmation.Visible;
    }

    private int GetRemoveAllCost() {
        // return 100 * (GetAvailableSkillPoints() - GetMaxSkillPoints()); //only use if cost is based on how many points
        var pointsAllocated = 0;
        foreach (var skillData in skillsDataList) {
            pointsAllocated += skillData.Current;
        }
        return pointsAllocated * 100;
    }

    private void SpendRemovePointSucrose(int amount) {
        main.ChangeSucrose(-amount);
    }

    private void RemoveAllPoints() {
        var totalCost = GetRemoveAllCost();

        if (vars.CurrentSucrose < totalCost) { return; }

        SpendRemovePointSucrose(totalCost);

        foreach (var skillButton in allSkillButtons) {
            var skillData = GetSkillData(skillButton.Name);

            if (skillData == null) { continue; }

            int pointsToRefund = skillData.Current;
            if (pointsToRefund < 1) {
                continue;
            }

            ResetSkillToZero(skillData);
        }
        removeConfirmation.Visible = false;
        vars.SaveGameData();
    }

    private void ResetSkillToZero(SkillsData skillData) {
        skillData.Current = 0;
        vars.SkillSprayerDamage = 0;
        vars.SkillSprayerCritChance = 0;
        vars.SkillSprayerKnockback = 0;
        vars.SkillSprayerMelt = 0;
        
        vars.SkillSoftenerDamage = 0;
        vars.SkillSoftenerSize = 0;
        vars.SkillSoftenerPierce = 0;
        vars.SkillSoftenerChill = 0;
        
        vars.SkillSpreaderDamage = 0;
        vars.SkillSpreaderBullets = 0;
        vars.SkillSpreaderKnockback = 0;
        vars.SkillSpreaderReload = 0;
        
        vars.SkillSlowerDamage = 0;
        vars.SkillSlowerSize = 0;
        vars.SkillSlowerMelt = 0;
        vars.SkillSlowerReload = 0;
        
        vars.SkillSniperDamage = 0;
        vars.SkillSniperInstant = 0;
        vars.SkillSniperReload = 0;
        vars.SkillSniperRestore = 0;
        
        vars.SkillSmasherDamage = 0;
        vars.SkillSmasherChill = 0;
        vars.SkillSmasherKnockback = 0;
        vars.SkillSmasherMelt = 0;
        
        CheckSkillButtons();
        UpdateWeaponSkillsUI();
    }

    public SkillsData GetSkillData(string skillName) {
        foreach (var skillData in skillsDataList) {
            if (skillData.Skill.Equals(skillName)) { return skillData; }
        }
        return null;
    }

    private void CheckSkillButtons() {
        foreach (var skillButton in allSkillButtons) {
            foreach (var skillData in skillsDataList) {
                if (!skillData.Skill.Equals(skillButton.Name)) { continue; }

                skillButton.GetNode<Label>("Letter").Text = skillData.Letter;
                skillButton.GetNode<Label>("Letter/Amount").Text = skillData.Current + "/" + skillData.Max;
                skillButton.TextureNormal = skillBoxTextureNormal;
                if (skillData.Current >= skillData.Costs.Length) { //check if the skill is maxed
                    skillButton.TextureNormal = skillBoxTextureDisabled;
                    break;
                }

                var nextCost = skillData.Costs[skillData.Current];
                if (GetAvailableSkillPoints() < nextCost) {
                    //if not enough skill points
                    skillButton.TextureNormal = skillBoxTextureDisabled;
                    break;
                }
            }
        }
    }

    private int GetAvailableSkillPoints() {
        var available = GetMaxSkillPoints();
        var totalCost = 0;

        foreach (var skillData in skillsDataList) {
            var currentVariable = skillData.Current;
            for (int i = 0; i < currentVariable; i++) {
                var nextCost = skillData.Costs[i];
                // GD.Print("cost: " + nextCost);
                totalCost += nextCost;
            }
        }

        return available - totalCost;
    }

    private int GetMaxSkillPoints() {
        // return 100;
        var level = Math.Floor(vars.PlayerLevel / 2.0f);
        var assignmentPoints = 0;
        var assignmentData = assignments.GetAllAssignmentData();
        foreach (var data in assignmentData) {
            if (!data.Complete) { continue; }
            assignmentPoints += data.PointReward;
        }
        
        return (int) level + assignmentPoints;
    }
    
    private string GetWeaponSkillCostsString(string skillName) {
        var skillData = GetSkillData(skillName);

        if (skillData == null || skillData.Costs == null || skillData.Costs.Length == 0) {
            return "N/A (Skill Data Missing)"; 
        }
        
        //check if the skill is fully upgraded (maxed)
        // if (skillData.Current >= skillData.Costs.Length) { return $"{greenColorOpenTag}MAXED{greenColorCloseTag}"; } //not used

        StringBuilder costsStringBuilder = new StringBuilder();
        
        for (int i = 0; i < skillData.Costs.Length; i++) {
            int rankNumber = i + 1;
            int costForThisRank = skillData.Costs[i];
            
            string line;
            
            //conditionally build the line based on the skill's FormatType
            if (skillData.FormatType == SkillsData.EffectType.TextOnly) { //text-only skills use the TypePrefix as the full description.
                line = $"{skillData.TypePrefix} ({costForThisRank} SP)"; //with sp at end
                // line = $"({costForThisRank} SP) {skillData.TypePrefix}"; //with sp at beginning
            } else {
                float effectForThisRank = skillData.Effects[i];
                
                if (skillData.FormatType == SkillsData.EffectType.Percent) {
                    // line = $"Rank {rankNumber}: {skillData.TypePrefix}{effectForThisRank:P0} ({costForThisRank} SP)"; //using word "rank"
                    line = $"{skillData.TypePrefix}{effectForThisRank:P0} ({costForThisRank} SP)";
                } else if (skillData.FormatType == SkillsData.EffectType.Chance) {
                    effectForThisRank = skillData.Effects[i];
                    line = $"{skillData.TypePrefix}{effectForThisRank}% ({costForThisRank} SP)";
                } else { //flatValue
                    // line = $"Rank {rankNumber}: {skillData.TypePrefix}+{effectForThisRank} ({costForThisRank} SP)"; //using word "rank"
                    line = $"{skillData.TypePrefix}{effectForThisRank} ({costForThisRank} SP)";
                }
            }

            // if (i == skillData.Current - 1) { //apply green color to the line representing the currently achieved rank
            if (i < skillData.Current) { //apply green color to all lines up to and including currently achieved rank
                costsStringBuilder.Append(tooltips.GetStringColor("green"));
                costsStringBuilder.Append(line);
                costsStringBuilder.Append("[/color]");
            } else {
                costsStringBuilder.Append(line);
            }

            //add a newline character if it's not the very last line
            if (i < skillData.Costs.Length - 1) { costsStringBuilder.Append("\n"); }
        }
        return costsStringBuilder.ToString();
    }
    
    private string GetWeaponSkillDescription(string skillName) {
        var colorAllocated = tooltips.GetStringColor("green");
        var colorUnallocated = tooltips.GetStringColor("black");
        
        switch (skillName) {
            case "SprayerDamage":
                var sprayerDamage = main.GetSkillDamageEffect("bulletSprayer") - 1;
                return $"Sprayer Damage:\n{(sprayerDamage > 0 ? colorAllocated : colorUnallocated)}+{sprayerDamage:P0}[/color]"; //will display skill only (50)
                // return $"Total Damage:\n{tooltips.GetStringColor("green")}{1.0f + main.GetTotalSkillDamageEffect("SprayerDamage", vars.SkillSprayerDamage):P0}[/color]"; //will display total (100 + 50)
            case "SprayerCritChance":
                var sprayerCrit = main.GetSkillCritChanceEffect("bulletSprayer");
                return $"Crit Chance:\n{(sprayerCrit > 0 ? colorAllocated : colorUnallocated)}+{sprayerCrit}%[/color]";
            case "SprayerMelt":
                var sprayerMelt = main.GetSkillMeltEffect("bulletSprayer");
                return $"MELT Duration:\n{(sprayerMelt > 0 ? colorAllocated : colorUnallocated)}{sprayerMelt} Seconds[/color]";
            case "SprayerKnockback":
                var sprayerDistance = main.GetSkillKnockbackEffect("bulletSprayer");
                return $"KNOCKBACK Distance:\n{(sprayerDistance > 0 ? colorAllocated : colorUnallocated)}{sprayerDistance}m[/color]";
            
            case "SoftenerDamage":
                var softenerDamage = main.GetSkillDamageEffect("bulletSoftener") - 1;
                return $"Softener Damage:\n{(softenerDamage > 0 ? colorAllocated : colorUnallocated)}+{softenerDamage:P0}[/color]";
            case "SoftenerPierce":
                var softenerPierce = main.GetSkillPierceEffect("bulletSoftener");
                return $"Softener Pierce:\n{(softenerPierce > 0 ? colorAllocated : colorUnallocated)}+{softenerPierce}[/color]";
            case "SoftenerChill":
                var softenerChill = main.GetSkillChillEffect("bulletSoftener");
                return $"CHILL Duration:\n{(softenerChill > 0 ? colorAllocated : colorUnallocated)}{softenerChill} Seconds[/color]";
            case "SoftenerSize":
                var softenerSize = main.GetSkillBulletSize("bulletSoftener");
                return $"Bullet Size:\n{(softenerSize > 0 ? colorAllocated : colorUnallocated)}{softenerSize}x[/color]";
            
            case "SpreaderDamage":
                var spreaderDamage = main.GetSkillDamageEffect("bulletSpreader") - 1;
                return $"Spreader Damage:\n{(spreaderDamage > 0 ? colorAllocated : colorUnallocated)}+{spreaderDamage:P0}[/color]";
            case "SpreaderKnockback":
                var spreaderDistance = main.GetSkillKnockbackEffect("bulletSpreader");
                return $"KNOCKBACK Distance:\n{(spreaderDistance > 0 ? colorAllocated : colorUnallocated)}{spreaderDistance}m[/color]";
            case "SpreaderReload":
                var spreaderReload = main.GetSkillReloadEffect("bulletSpreader");
                return $"Reload Multiplier:\n{(spreaderReload > 0 ? colorAllocated : colorUnallocated)}{spreaderReload}[/color]";
            case "SpreaderBullets":
                var spreaderBullets = main.GetSkillSpreaderBulletAmount();
                return $"Additional Bullets:\n{(spreaderBullets > 0 ? colorAllocated : colorUnallocated)}{spreaderBullets}[/color]";
            
            case "SniperDamage":
                var sniperDamage = main.GetSkillDamageEffect("bulletSniper") - 1;
                return $"Sniper Damage:\n{(sniperDamage > 0 ? colorAllocated : colorUnallocated)}+{sniperDamage:P0}[/color]";
            case "SniperRestore":
                var sniperRestore = main.GetSkillHealthOnHitEffect("bulletSniper");
                return $"Health on Hit:\n{(sniperRestore > 0 ? colorAllocated : colorUnallocated)}{sniperRestore:P0}[/color]";
            case "SniperReload":
                var sniperReload = main.GetSkillReloadEffect("bulletSniper");
                return $"Reload Multiplier:\n{(sniperReload > 0 ? colorAllocated : colorUnallocated)}{sniperReload}[/color]";
            case "SniperInstant":
                var sniperInstant = main.GetSkillInstantKillEffect("bulletSniper");
                return $"Instant Kill Chance:\n{(sniperInstant > 0 ? colorAllocated : colorUnallocated)}{sniperInstant}%[/color]";
            
            case "SlowerDamage":
                var slowerDamage = main.GetSkillDamageEffect("bulletSlower") - 1;
                return $"Slower Damage:\n{(slowerDamage > 0 ? colorAllocated : colorUnallocated)}+{slowerDamage:P0}[/color]";
            case "SlowerReload":
                var slowerReload = main.GetSkillReloadEffect("bulletSlower");
                return $"Reload Multiplier:\n{(slowerReload > 0 ? colorAllocated : colorUnallocated)}{slowerReload}[/color]";
            case "SlowerMelt":
                var slowerMelt = main.GetSkillMeltEffect("bulletSlower");
                return $"MELT Duration:\n{(slowerMelt > 0 ? colorAllocated : colorUnallocated)}{slowerMelt} Seconds[/color]";
            case "SlowerSize":
                var slowerSize = main.GetSkillBulletSize("bulletSlower");
                return $"Bullet Size:\n{(slowerSize > 0 ? colorAllocated : colorUnallocated)}{slowerSize}x[/color]";
            
            case "SmasherDamage":
                var smasherDamage = main.GetSkillDamageEffect("bulletSmasher") - 1;
                return $"Smasher Damage:\n{(smasherDamage > 0 ? colorAllocated : colorUnallocated)}+{smasherDamage:P0}[/color]";
            case "SmasherKnockback":
                var smasherDistance = main.GetSkillKnockbackEffect("bulletSmasher");
                return $"KNOCKBACK Distance:\n{(smasherDistance > 0 ? colorAllocated : colorUnallocated)}{smasherDistance}m[/color]";
            case "SmasherMelt":
                var smasherMelt = main.GetSkillMeltEffect("bulletSmasher");
                return $"MELT Duration:\n{(smasherMelt > 0 ? colorAllocated : colorUnallocated)}{smasherMelt} Seconds[/color]";
            case "SmasherChill":
                var smasherChill = main.GetSkillChillEffect("bulletSmasher");
                return $"CHILL Duration:\n{(smasherChill > 0 ? colorAllocated : colorUnallocated)}{smasherChill} Seconds[/color]";
        }
        return "invalid skill name";
    }
    
    private void BuildSkillsData() {
        skillsDataList = [
            //sprayer
            new SkillsData("SprayerDamage", "Damage +", SkillsData.EffectType.Percent, vars.SkillSprayerDamage, 5, "D",
                [1, 1, 1, 1, 1], [.1f, .1f, .1f, .1f, .1f]), //[.05f, .05f, .05f, .05f, .05f]
            new SkillsData("SprayerCritChance", "Crit +", SkillsData.EffectType.Chance, vars.SkillSprayerCritChance, 5, "C",
                [1, 2, 3, 4, 5], [1, 1, 1, 1, 1]),
            new SkillsData("SprayerMelt", "Melt on hit ", SkillsData.EffectType.TextOnly, vars.SkillSprayerMelt, 1, "M",
                [5], [5]),
            new SkillsData("SprayerKnockback", "Knockback on hit", SkillsData.EffectType.TextOnly, vars.SkillSprayerKnockback, 1, "K",
                [3], [1]),
            
            //softener
            new SkillsData("SoftenerDamage", "Damage +", SkillsData.EffectType.Percent, vars.SkillSoftenerDamage, 5, "D",
                [2, 2, 2, 2, 2], [.05f, .05f, .05f, .05f, .05f]),
            new SkillsData("SoftenerPierce", "Pierce +", SkillsData.EffectType.FlatValue, vars.SkillSoftenerPierce, 5, "P",
                [1, 1, 1, 1, 1], [1f, 1f, 1f, 1f, 1f]),
            new SkillsData("SoftenerChill", "Chill on hit ", SkillsData.EffectType.TextOnly, vars.SkillSoftenerChill, 1, "C",
                [5], [5]),
            new SkillsData("SoftenerSize", "Double bullet size", SkillsData.EffectType.TextOnly, vars.SkillSoftenerSize, 1, "S",
                [5], [1]), //size needs to be 1 less than multipler (effect 1 = 2x size)
            
            //spreader
            new SkillsData("SpreaderDamage", "Damage +", SkillsData.EffectType.Percent, vars.SkillSpreaderDamage, 5, "D",
                [1, 1, 1, 1, 1], [.05f, .05f, .05f, .05f, .05f]),
            new SkillsData("SpreaderKnockback", "Knockback on hit", SkillsData.EffectType.TextOnly, vars.SkillSpreaderKnockback, 1, "K",
                [5], [5]),
            new SkillsData("SpreaderReload", "2x Reload Speed ", SkillsData.EffectType.TextOnly, vars.SkillSpreaderReload, 1, "R",
                [5], [.5f]), //multiplier
            new SkillsData("SpreaderBullets", "Bullet +", SkillsData.EffectType.FlatValue, vars.SkillSpreaderBullets, 3, "B",
                [2, 4, 4], [1, 1, 1]),
            
            //sniper
            new SkillsData("SniperDamage", "Damage +", SkillsData.EffectType.Percent, vars.SkillSniperDamage, 5, "D",
                [1, 1, 1, 1, 1], [.1f, .1f, .1f, .1f, .1f]),
            new SkillsData("SniperRestore", "Restore health on hit ", SkillsData.EffectType.Percent, vars.SkillSniperRestore, 1, "H",
                [5], [.05f]),
            new SkillsData("SniperReload", "Reload twice as fast ", SkillsData.EffectType.TextOnly, vars.SkillSniperReload, 1, "R",
                [5], [.5f]),
            new SkillsData("SniperInstant", "Instant kill +", SkillsData.EffectType.Chance, vars.SkillSniperInstant, 3, "I",
                [3, 4, 5], [10, 10, 10]),
            
            //slower
            new SkillsData("SlowerDamage", "Damage +", SkillsData.EffectType.Percent, vars.SkillSlowerDamage, 5, "D",
                [1, 1, 1, 1, 1], [.1f, .1f, .1f, .1f, .1f]),
            new SkillsData("SlowerReload", "3x Reload Speed ", SkillsData.EffectType.TextOnly, vars.SkillSlowerReload, 1, "R",
                [5], [.66f]),
            new SkillsData("SlowerMelt", "Melt on hit ", SkillsData.EffectType.TextOnly, vars.SkillSlowerMelt, 1, "M",
                [5], [5]),
            new SkillsData("SlowerSize", "Triple bullet size", SkillsData.EffectType.TextOnly, vars.SkillSlowerSize, 1, "S",
                [5], [2]),
            
            //smasher
            new SkillsData("SmasherDamage", "Damage +", SkillsData.EffectType.Percent, vars.SkillSmasherDamage, 5, "D",
                [1, 1, 1, 1, 1], [.1f, .1f, .1f, .1f, .1f]),
            new SkillsData("SmasherKnockback", "Knockback on hit", SkillsData.EffectType.TextOnly, vars.SkillSmasherKnockback, 1, "K",
                [2], [30]),
            new SkillsData("SmasherMelt", "Melt on hit ", SkillsData.EffectType.TextOnly, vars.SkillSmasherMelt, 1, "M",
                [4], [5]),
            new SkillsData("SmasherChill", "Chill on hit", SkillsData.EffectType.TextOnly, vars.SkillSmasherChill, 1, "C",
                [4], [5]),
        ];
    }
}


public class SkillsData {
    public enum EffectType {
        Percent, //format as a percentage (e.g., 5%)
        FlatValue, //format as a raw number (e.g., +3)
        Chance,
        TextOnly //no number, just a descriptive text
    }
    
    public string Skill; //must match button name
    public string TypePrefix;
    public EffectType FormatType { get; set; }
    public int Current;
    public int Max;
    public string Letter;
    public int[] Costs;
    public float[] Effects;
    
    public SkillsData() { }

    public SkillsData(string skill, string prefix, EffectType formatType, int current, int max, string letter, int[] costs, float[] effects) {
        Skill = skill;
        TypePrefix = prefix;
        FormatType = formatType;
        Current = current;
        Max = max;
        Letter = letter;
        Costs = costs;
        Effects = effects;
    }
}