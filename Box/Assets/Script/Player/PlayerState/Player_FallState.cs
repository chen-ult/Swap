using UnityEngine;

public class Player_FallState : Player_AiredState
{
    public Player_FallState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
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
        if (player.groundDetected)
        {
            // play landing sound if available
            if (player.audioSource != null && player.sfx_PlayerLand != null)
                player.audioSource.PlayOneShot(player.sfx_PlayerLand, player.sfxVolume);

            stateMachine.ChangeState(player.idleState);
        }
    }
}
