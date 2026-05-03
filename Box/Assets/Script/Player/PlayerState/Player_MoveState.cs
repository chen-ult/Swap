using UnityEngine;

public class Player_MoveState : Player_GroundState
{
    public Player_MoveState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();


        if (player.moveInput.x == 0 )
        {
            stateMachine.ChangeState(player.idleState);
        }
        else
        {
            float targetVelX = player.moveInput.x * player.movespeed;
            if (Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetVelX) && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(targetVelX))
            {
                player.SetVelocity(rb.linearVelocity.x, rb.linearVelocity.y);
            }
            else
            {
                player.SetVelocity(targetVelX, rb.linearVelocity.y);
            }
        }
    }
}
