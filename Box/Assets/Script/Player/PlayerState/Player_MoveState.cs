using UnityEngine;

public class Player_MoveState : Player_GroundState
{
    public Player_MoveState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }
    // timer-based footstep triggering
    private float stepTimer = 0f;

    public override void Enter()
    {
        base.Enter();
        // initialize timer so footsteps remain evenly spaced after entering move state
        stepTimer = 0f;
    }

    public override void Exit()
    {
        base.Exit();
        // stop any playing footstep immediately (use dedicated source)
        if (player.footstepSource != null)
        {
            try { player.footstepSource.Stop(); } catch { }
        }
    }

    public override void Update()
    {
        base.Update();


        if (player.moveInput.x == 0 )
        {
            stepTimer = 0f;
            stateMachine.ChangeState(player.idleState);
            return;
        }

        float targetVelX = player.moveInput.x * player.movespeed;
        if (Mathf.Abs(rb.linearVelocity.x) > Mathf.Abs(targetVelX) && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(targetVelX))
        {
            player.SetVelocity(rb.linearVelocity.x, rb.linearVelocity.y);
        }
        else
        {
            player.SetVelocity(targetVelX, rb.linearVelocity.y);
        }

        if (player.moveInput.x != 0 && player.groundDetected)
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= player.StepInterval)
            {
                // play footstep on a fixed cadence while moving on the ground
                player.PlayFootstep();
                stepTimer = 0f;
            }
        }
        else
        {
            // reset timer when not moving or not grounded
            stepTimer = 0f;
        }
    }

    // Footsteps are triggered by a simple timer while the player is moving on the ground.
}
