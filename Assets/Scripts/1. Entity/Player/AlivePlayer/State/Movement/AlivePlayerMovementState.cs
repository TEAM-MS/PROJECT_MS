using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class AlivePlayerMovementState : AlivePlayerState
{
    protected AlivePlayerMovementStateMachine movementStateMachine { get; private set; }

    public AlivePlayerMovementState(AlivePlayerStateMachine stateMachine) : base(stateMachine)
    {
        movementStateMachine = stateMachine.MovementStateMachine;
    }

    #region IState Methods
    public override void Enter()
    {
        Debug.Log($"MovementStateMachine : Enter {GetType().Name}");

        base.Enter();

        stateMachine.Player.onDamaged += OnDamage;
    }

    public override void Exit()
    {
        base.Exit();

        stateMachine.Player.onDamaged -= OnDamage;
    }

    public override void Update()
    {
        base.Update();

        ReadMovementInput();
        Move();
    }
    #endregion

    #region Main Methods
    private void ReadMovementInput()
    {
        stateMachine.ReusableData.MovementInput = Managers.Input.GetInput(EPlayerInput.Move).ReadValue<Vector2>();
        stateMachine.ReusableData.ShouldSprint = Managers.Input.GetInput(EPlayerInput.Sprint).IsPressed();
    }

    private void Move()
    {
        stateMachine.Player.CharacterController.Move(stateMachine.ReusableData.VerticalVelocity * Time.deltaTime);

        if(stateMachine.ReusableData.MovementInput == Vector2.zero || stateMachine.ReusableData.MovementSpeedModifier == 0f)
        {
            return;
        }

        Vector3 movementDirection = GetMovementInputDirection();

        RotationPlayer(movementDirection);

        stateMachine.Player.CharacterController.Move(movementDirection * stateMachine.ReusableData.MovementSpeedModifier * Time.deltaTime);
    }
    #endregion

    #region Reusable Methods
    protected Vector3 GetMovementInputDirection()
    {
        return new Vector3(stateMachine.ReusableData.MovementInput.x, 0f, stateMachine.ReusableData.MovementInput.y);
    }

    public void SetInteractAnimation(AnimationClip animationClip, float speed = 1f)
    {
        stateMachine.Player.overrideController["Interaction"] = animationClip;
        stateMachine.Player.Animator.SetFloat("InteractingSpeed", speed);
    }

    protected override void AddInputActionCallbacks()
    {
        Managers.Input.GetInput(EPlayerInput.Move).canceled += OnMovementCanceled;
    }

    protected override void RemoveInputActionCallbacks()
    {
        Managers.Input.GetInput(EPlayerInput.Move).canceled -= OnMovementCanceled;
    }

    protected void RotationPlayer(Vector3 direction)
    {
        float targetRotationYAngle = GetRotationAngle(direction);

        stateMachine.Player.transform.rotation = Quaternion.Slerp(stateMachine.Player.transform.rotation, Quaternion.Euler(0f, targetRotationYAngle, 0f), stateMachine.Player.AlivePlayerSO.RotationSpeed * Time.deltaTime);
    }

    protected float GetRotationAngle(Vector3 direction)
    {
        return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
    }
    #endregion

    #region Input Methods
    protected virtual void OnMovementCanceled(InputAction.CallbackContext context)
    {

    }
    #endregion

    private void OnDamage()
    {
        DamageEffect().Forget();
    }

    async UniTaskVoid DamageEffect()
    {
        float elapsedTime = 0f;
        while(elapsedTime < 1.5f)
        {
            stateMachine.ReusableData.MovementSpeedPercentage = 0.7f;
            elapsedTime += Time.deltaTime;
            await UniTask.Yield();
        }

        stateMachine.ReusableData.MovementSpeedPercentage = 1f;
    }
}
