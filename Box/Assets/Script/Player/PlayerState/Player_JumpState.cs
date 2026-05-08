using UnityEngine;

public class Player_JumpState : Player_AiredState
{
    public Player_JumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.SetVelocity(rb.linearVelocity.x, player.jumpforce);
        // play jump sound
        if (player.audioSource != null && player.sfx_PlayerJump != null)
            player.audioSource.PlayOneShot(player.sfx_PlayerJump, player.sfxVolume);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        if (rb.linearVelocity.y < 0 )
            stateMachine.ChangeState(player.fallState);
    }
}
