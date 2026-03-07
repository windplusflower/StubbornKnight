using System.Collections.Generic;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using Satchel;
using Satchel.Futils;
using UnityEngine;

namespace StubbornKnight;

[Serializable]
public class Settings
{
    public bool on = true;
    public int arrowCount = 3;
    public float arrowOpacity = 1.0f;
    public int soundVolume = 5;
}

public class StubbornKnight : Mod, IGlobalSettings<Settings>, IMenuMod
{
    public static StubbornKnight instance;
    private Settings mySettings = new();

    public static bool IsModEnabled => instance != null && instance.mySettings.on;

    public StubbornKnight() : base("StubbornKnight")
    {
        instance = this;
    }

    public override string GetVersion() => "1.0";

    public override List<(string, string)> GetPreloadNames()
    {
        return new List<(string, string)>();
    }

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        On.HeroController.Start += HeroController_Start;
        On.HeroController.DoAttack += HeroController_DoAttack;
        On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;

        ModHooks.LanguageGetHook += changeName;
    }

    private void HeroController_Start(On.HeroController.orig_Start orig, HeroController self)
    {
        orig(self);

        var arrowGame = self.gameObject.AddComponent<ArrowGame>();
        arrowGame.SetModEnabled(mySettings.on);
        arrowGame.SetConfig(mySettings.arrowCount + 1, mySettings.arrowOpacity, mySettings.soundVolume);
    }

    private void HeroController_DoAttack(On.HeroController.orig_DoAttack orig, HeroController self)
    {
        try
        {
            if (!mySettings.on)
            {
                orig(self);
                return;
            }

            var arrowGame = self.GetComponent<ArrowGame>();
            if (arrowGame == null)
            {
                orig(self);
                return;
            }

            float verticalInput = UnityEngine.Input.GetAxisRaw("Vertical");
            float horizontalInput = UnityEngine.Input.GetAxisRaw("Horizontal");

            AttackDirection attackDir;
            if (verticalInput > 0.1f)
            {
                attackDir = AttackDirection.upward;
            }
            else if (verticalInput < -0.1f && self.hero_state != ActorStates.idle && self.hero_state != ActorStates.running)
            {
                attackDir = AttackDirection.downward;
            }
            else if (horizontalInput > 0.1f || horizontalInput < -0.1f)
            {
                attackDir = AttackDirection.normal;
            }
            else
            {
                attackDir = AttackDirection.normal;
            }

            bool isAllowed = arrowGame.IsAttackAllowed(attackDir);

            if (isAllowed)
            {
                orig(self);
                ArrowDirection targetArrow = arrowGame.CurrentTargetArrow;
                ArrowDirection actualDir = attackDir == AttackDirection.normal 
                    ? (self.cState.facingRight ? ArrowDirection.Right : ArrowDirection.Left)
                    : (attackDir == AttackDirection.upward ? ArrowDirection.Up : ArrowDirection.Down);
                
                if (actualDir == targetArrow)
                {
                    arrowGame.OnSuccessfulAction();
                }
            }
            else
            {
                arrowGame.TriggerErrorEffect();
            }
        }
        catch
        {
            orig(self);
        }
    }

    private void PlayMakerFSM_OnEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        orig(self);

        if (self.FsmName == "Spell Control" && self.gameObject == HeroController.instance?.gameObject)
        {
            ModifySpellControlFSM(self);
        }

        if (self.FsmName == "Nail Arts" && self.gameObject == HeroController.instance?.gameObject)
        {
            ModifyNailArtsFSM(self);
        }
    }

    private void ModifySpellControlFSM(PlayMakerFSM fsm)
    {
        InjectSpellAction(fsm, "QC");
        InjectSpellAction(fsm, "Spell Choice");

        Log("Spell Control FSM modified");
    }

    private void ModifyNailArtsFSM(PlayMakerFSM fsm)
    {
        InjectNailArtAction(fsm, "Has Dash?", ArrowDirection.Left, "Dash Slash");
        InjectNailArtAction(fsm, "Has Cyclone?", ArrowDirection.Up, "Cyclone Slash");
        InjectNailArtAction(fsm, "Has G Slash?", ArrowDirection.Left, "Great Slash");

        Log("Nail Arts FSM modified");
    }

    private void InjectNailArtAction(PlayMakerFSM fsm, string stateName, ArrowDirection direction, string nailArtName)
    {
        var state = fsm.Fsm.GetState(stateName);
        if (state == null) return;

        foreach (var existingAction in state.Actions)
        {
            if (existingAction is NailArtInterceptAction)
            {
                return;
            }
        }

        var action = new NailArtInterceptAction
        {
            ExpectedDirection = direction,
            NailArtName = nailArtName
        };

        var newActions = new FsmStateAction[state.Actions.Length + 1];
        newActions[0] = action;
        for (int i = 0; i < state.Actions.Length; i++)
        {
            newActions[i + 1] = state.Actions[i];
        }
        state.Actions = newActions;
    }

    private void CheckAndInterceptNailArt(ArrowDirection expectedDir, string nailArtName, PlayMakerFSM fsm)
    {
    }

    private void InjectSpellAction(PlayMakerFSM fsm, string stateName)
    {
        var state = fsm.Fsm.GetState(stateName);
        if (state == null) return;

        if (state.Actions.Length > 0 && state.Actions[0] is SpellInterceptAction)
        {
            return;
        }

        var action = new SpellInterceptAction();

        var newActions = new FsmStateAction[state.Actions.Length + 1];
        newActions[0] = action;
        for (int i = 0; i < state.Actions.Length; i++)
        {
            newActions[i + 1] = state.Actions[i];
        }
        state.Actions = newActions;
    }

    private string changeName(string key, string title, string orig)
    {
        return orig;
    }

    public bool ToggleButtonInsideMenu => true;

    public void OnLoadGlobal(Settings settings) => mySettings = settings;

    public Settings OnSaveGlobal() => mySettings;

    private void ToggleModSetting(bool enabled)
    {
        if (HeroController.instance != null)
        {
            var arrowGame = HeroController.instance.GetComponent<ArrowGame>();
            arrowGame?.SetModEnabled(enabled);
        }
    }

    private void ApplyArrowSettings()
    {
        if (HeroController.instance != null)
        {
            var arrowGame = HeroController.instance.GetComponent<ArrowGame>();
            arrowGame?.UpdateConfig(mySettings.arrowCount + 1, mySettings.arrowOpacity, mySettings.soundVolume);
        }
    }

    public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? menu)
    {
        List<IMenuMod.MenuEntry> menus = new();

        string[] onOffValues = new string[]
        {
            Language.Language.Get("MOH_ON", "MainMenu"),
            Language.Language.Get("MOH_OFF", "MainMenu"),
        };
        menus.Add(
            new()
            {
                Values = onOffValues,
                Saver = i => {
                    mySettings.on = i == 0;
                    ToggleModSetting(mySettings.on);
                },
                Loader = () => mySettings.on ? 0 : 1,
                Name = "StubbornKnight"
            }
        );

        string[] arrowCountValues = new string[30];
        for (int i = 0; i < 30; i++)
        {
            arrowCountValues[i] = (i + 1).ToString();
        }
        menus.Add(
            new()
            {
                Values = arrowCountValues,
                Saver = i => {
                    mySettings.arrowCount = i + 1;
                    ApplyArrowSettings();
                },
                Loader = () => mySettings.arrowCount - 1,
                Name = "Arrow Queue Length"
            }
        );

        string[] opacityValues = new string[10];
        for (int i = 0; i < 10; i++)
        {
            opacityValues[i] = ((i + 1) * 0.1f).ToString("F1");
        }
        menus.Add(
            new()
            {
                Values = opacityValues,
                Saver = i => {
                    mySettings.arrowOpacity = (i + 1) * 0.1f;
                    ApplyArrowSettings();
                },
                Loader = () => (int)(mySettings.arrowOpacity * 10) - 1,
                Name = "Arrow Opacity"
            }
        );

        string[] volumeValues = new string[11];
        for (int i = 0; i <= 10; i++)
        {
            volumeValues[i] = i.ToString();
        }
        menus.Add(
            new()
            {
                Values = volumeValues,
                Saver = i => {
                    mySettings.soundVolume = i;
                    ApplyArrowSettings();
                },
                Loader = () => mySettings.soundVolume,
                Name = "Sound Volume"
            }
        );

        return menus;
    }
}
