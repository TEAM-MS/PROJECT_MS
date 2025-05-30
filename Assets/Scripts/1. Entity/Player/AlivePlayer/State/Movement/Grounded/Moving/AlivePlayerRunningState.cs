using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class AlivePlayerRunningState : AlivePlayerMovingState
{
    private static readonly string _clickSound = "Sound/Player/Footstep_01_01.mp3";
    private Coroutine _footstepCoroutine;
    private const float FOOTSTEP_INTERVAL = 0.35f;
    private const float FOOTSTEP_VOLUME = 0.6f; // 발걸음 소리 볼륨

    public AlivePlayerRunningState(AlivePlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.ReusableData.MovementSpeed = stateMachine.Player.AlivePlayerSO.MoveSpeed;

        base.Enter();

        StartAnimation(stateMachine.Player.AnimationData.RunningParameterHash);

        _footstepCoroutine = stateMachine.Player.StartCoroutine(PlayFootstepRoutine());
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.Player.AnimationData.RunningParameterHash);
                
        if (_footstepCoroutine != null)
        {
            stateMachine.Player.StopCoroutine(_footstepCoroutine);
            _footstepCoroutine = null;
        }
    }

    public override void Update()
    {
        base.Update();

        if(stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            return;
        }

        if(stateMachine.ReusableData.ShouldSprint && stateMachine.Player.Stamina.Current.Value > stateMachine.Player.Stamina.Maximum.Value * 0.1f)
        {
            movementStateMachine.ChangeState(movementStateMachine.SprintingState);
            return;
        }
    }
    
    private IEnumerator PlayFootstepRoutine()
    {
        while (true)
        {
            Managers.Sound.Play3D(_clickSound, stateMachine.Player.transform.position, FOOTSTEP_VOLUME);
            yield return new WaitForSeconds(FOOTSTEP_INTERVAL);
        }
    }

    #region Input Methods

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        movementStateMachine.ChangeState(movementStateMachine.IdlingState);

        base.OnMovementCanceled(context);
    }
    #endregion
}
