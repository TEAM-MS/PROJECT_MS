using UnityEngine;
using UnityEngine.InputSystem;

public class AttackingState : AlivePlayerCombatState
{
    private static readonly string madSound = "Sound/Player/Whoosh_02.mp3";
    private bool onPlaySound = false;
    public AttackingState(AlivePlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods
    public override void Enter()
    {
        base.Enter();

        StartAnimation(stateMachine.Player.AnimationData.AttackingParameterHash);

        onPlaySound = false;
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.Player.AnimationData.AttackingParameterHash);
    }

    public override void Update()
    {
        base.Update();

        float normalizedTime = GetNormalizedTime(stateMachine.Player.Animator, "Attack", 2);

        if(normalizedTime >= 0.4f && normalizedTime < 0.6f)
        {
            stateMachine.Player.WeaponHandler.SetIsAttacking(true);
            if(!onPlaySound)
            {
                Managers.Sound.Play(madSound);
                onPlaySound = true;
            }
        }
        else
        {
            stateMachine.Player.WeaponHandler.SetIsAttacking(false); 
        }

        if(normalizedTime >= 0.8f)
        {
            combatStateMachine.ChangeState(combatStateMachine.CombatIdlingState);
        }
    }
    #endregion
    
    #region Input Methods
    protected override void OnInputAttack(ItemController itemController)
    {

    }
    #endregion
}
