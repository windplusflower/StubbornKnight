using HutongGames.PlayMaker;
using Modding;
using UnityEngine;

namespace StubbornKnight;

public class NailArtInterceptAction : FsmStateAction
{
    public ArrowDirection ExpectedDirection;
    public string NailArtName;

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

        ArrowDirection actualDir;
        
        if (ExpectedDirection == ArrowDirection.Left || ExpectedDirection == ArrowDirection.Right)
        {
            actualDir = HeroController.instance.cState.facingRight ? ArrowDirection.Right : ArrowDirection.Left;
        }
        else if (ExpectedDirection == ArrowDirection.Up)
        {
            float verticalInput = UnityEngine.Input.GetAxisRaw("Vertical");
            actualDir = verticalInput < -0.1f ? ArrowDirection.Down : ArrowDirection.Up;
        }
        else
        {
            actualDir = ExpectedDirection;
        }

        ArrowDirection expected = arrowGame.CurrentTargetArrow;
        bool isSuccess = arrowGame.IsSpellAllowed(actualDir);

        if (!isSuccess)
        {
            arrowGame.TriggerErrorEffect();
            Fsm.Event("CANCEL");
            StubbornKnight.instance.Log($"[NailArt] State:{Fsm.ActiveStateName} Expected: {expected}, Actual: {NailArtName}({actualDir}), Result: FAILED");
        }
        else
        {
            arrowGame.OnSuccessfulAction();
            StubbornKnight.instance.Log($"[NailArt] State:{Fsm.ActiveStateName} Expected: {expected}, Actual: {NailArtName}({actualDir}), Result: SUCCESS");
        }

        Finish();
    }
}