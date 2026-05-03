using UnityEngine;

public class Player_AiredState : PlayerState
{
    public Player_AiredState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
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

        if (player.moveInput.x != 0)
        {
            float targetVelX = player.moveInput.x * (player.movespeed * player.inAirMoveMultiplier);

            // 如果原来的速度大于目标速度并且同向，为了保留弹簧等外力巨大的动能，不要直接粗暴减速
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
