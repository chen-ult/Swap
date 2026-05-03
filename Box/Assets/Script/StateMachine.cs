using UnityEngine;

public class StateMachine 
{
    public EntityState currentState { get; private set; }
    public bool canChangeState;

    public void Initialize(EntityState startState) 
    {
        currentState = startState;
        currentState.Enter();
        canChangeState = true;
    }

    public void ChangeState(EntityState newState)
    {
        if (!canChangeState)
            return;

        if(currentState != null)
            currentState.Exit();

        currentState = newState;

        if(currentState != null)
            currentState.Enter();
    }

    public void UpdateActiveState() 
    {
        if (currentState != null)
            currentState.Update();
    }

    public void SwitchOffStateChange() 
    {
        canChangeState = false;
    }
}
