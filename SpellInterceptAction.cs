using HutongGames.PlayMaker;
using Modding;
using UnityEngine;

namespace StubbornKnight;

public class SpellInterceptAction : FsmStateAction
{
    public override void OnEnter()
    {
        if (!StubbornKnight.IsModEnabled)
        {
            Finish();
            return;
        }

        var arrowGame = HeroController.instance?.GetComponent<ArrowGame>();
        if (arrowGame == null)
        {
            Finish();
            return;
        }

        float verticalInput = UnityEngine.Input.GetAxisRaw("Vertical");
        float horizontalInput = UnityEngine.Input.GetAxisRaw("Horizontal");

        ArrowDirection spellDir;
        string spellName;

        if (verticalInput > 0.1f)
        {
            spellDir = ArrowDirection.Up;
            spellName = "Shriek(吼)";
        }
        else if (verticalInput < -0.1f)
        {
            spellDir = ArrowDirection.Down;
            spellName = "Quake(砸)";
        }
        else if (horizontalInput > 0.1f)
        {
            spellDir = ArrowDirection.Right;
            spellName = "Fireball(波右)";
        }
        else if (horizontalInput < -0.1f)
        {
            spellDir = ArrowDirection.Left;
            spellName = "Fireball(波左)";
        }
        else
        {
            bool facingRight = HeroController.instance.cState.facingRight;
            spellDir = facingRight ? ArrowDirection.Right : ArrowDirection.Left;
            spellName = facingRight ? "Fireball(波右 - 默认)" : "Fireball(波左 - 默认)";
        }

        ArrowDirection expected = arrowGame.CurrentTargetArrow;
        bool isSuccess = (spellDir == expected);

        if (!isSuccess)
        {
            arrowGame.TriggerErrorEffect();
            Fsm.Event("FSM CANCEL");
            StubbornKnight.instance.Log($"[Spell] Expected: {expected}, Actual: {spellName}({spellDir}), Result: FAILED");
        }
        else
        {
            arrowGame.OnSuccessfulAction();
            StubbornKnight.instance.Log($"[Spell] Expected: {expected}, Actual: {spellName}({spellDir}), Result: SUCCESS");
        }

        Finish();
    }
}
