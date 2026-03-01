/*
 *  空洞骑士 Mod 入门到进阶指南/配套模版
 *  作者：近环（https://space.bilibili.com/1224243724）
 */

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
        On.HeroController.Attack += HeroController_Attack;
        On.HeroController.CanNailArt += HeroController_CanNailArt;
        On.PlayMakerFSM.OnEnable += PlayMakerFSM_OnEnable;

        ModHooks.LanguageGetHook += changeName;
    }

    private void HeroController_Start(On.HeroController.orig_Start orig, HeroController self)
    {
        orig(self);

        var arrowGame = self.gameObject.AddComponent<ArrowGame>();
        arrowGame.SetModEnabled(mySettings.on);
    }

    private void HeroController_Attack(On.HeroController.orig_Attack orig, HeroController self, AttackDirection dir)
    {
        if (!mySettings.on)
        {
            orig(self, dir);
            return;
        }

        var arrowGame = self.GetComponent<ArrowGame>();
        if (arrowGame == null)
        {
            orig(self, dir);
            return;
        }

        ArrowDirection expected = arrowGame.CurrentTargetArrow;
        string actualDir = dir.ToString();
        if (dir == AttackDirection.normal)
        {
            actualDir = HeroController.instance.cState.facingRight ? "Right" : "Left";
        }

        bool isSuccess = arrowGame.IsAttackAllowed(dir);

        if (isSuccess)
        {
            orig(self, dir);
            arrowGame.OnSuccessfulAction();
            Log($"[Attack] Expected: {expected}, Actual: {actualDir}, Result: SUCCESS");
        }
        else
        {
            Log($"[Attack] Expected: {expected}, Actual: {actualDir}, Result: FAILED");
        }
    }

    private bool HeroController_CanNailArt(On.HeroController.orig_CanNailArt orig, HeroController self)
    {
        bool result = orig(self);

        if (!mySettings.on || !result)
        {
            return result;
        }

        var arrowGame = self.GetComponent<ArrowGame>();
        if (arrowGame == null)
        {
            return result;
        }

        float verticalInput = Input.GetAxisRaw("Vertical");
        ArrowDirection arrowDir;
        string nailArtName;

        if (verticalInput > 0.1f)
        {
            arrowDir = ArrowDirection.Up;
            nailArtName = "Cyclone Slash";
        }
        else if (verticalInput < -0.1f)
        {
            arrowDir = ArrowDirection.Down;
            nailArtName = "Dash Slash";
        }
        else
        {
            arrowDir = self.cState.facingRight ? ArrowDirection.Right : ArrowDirection.Left;
            nailArtName = "Great Slash";
        }

        ArrowDirection expected = arrowGame.CurrentTargetArrow;
        bool isSuccess = arrowGame.IsSpellAllowed(arrowDir);

        if (!isSuccess)
        {
            Log($"[NailArt] Expected: {expected}, Actual: {nailArtName}({arrowDir}), Result: FAILED");
            return false;
        }
        else
        {
            arrowGame.OnSuccessfulAction();
            Log($"[NailArt] Expected: {expected}, Actual: {nailArtName}({arrowDir}), Result: SUCCESS");
            return result;
        }
    }

    private void PlayMakerFSM_OnEnable(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self)
    {
        orig(self);

        if (self.FsmName == "Spell Control" && self.gameObject == HeroController.instance?.gameObject)
        {
            ModifySpellControlFSM(self);
        }
    }

    private void ModifySpellControlFSM(PlayMakerFSM fsm)
    {
        InjectSpellAction(fsm, "QC");
        InjectSpellAction(fsm, "Spell Choice");

        Log("Spell Control FSM modified");
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

    public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? menu)
    {
        List<IMenuMod.MenuEntry> menus = new();
        menus.Add(
            new()
            {
                Values = new string[]
                {
                    Language.Language.Get("MOH_ON", "MainMenu"),
                    Language.Language.Get("MOH_OFF", "MainMenu"),
                },
                Saver = i => {
                    mySettings.on = i == 0;
                    ToggleModSetting(mySettings.on);
                },
                Loader = () => mySettings.on ? 0 : 1,
                Name = "StubbornKnight"
            }
        );
        return menus;
    }
}
