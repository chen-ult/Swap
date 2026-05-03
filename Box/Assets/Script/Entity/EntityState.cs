using UnityEngine;

public abstract class EntityState 
{
    protected StateMachine stateMachine;//状态机
    protected string animBoolName;//动画布尔参数名称
    protected Animator anim;//动画组件
    protected Rigidbody2D rb;//刚体组件
    protected Entity_Stats stats;//实体属性组件


    protected float stateTimer;//状态计时器

    protected bool triggerCalled;//动画触发器是否被调用

    public EntityState(StateMachine stateMachine, string animBoolName)
    {
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        anim.SetBool(animBoolName, true);
        triggerCalled = false;
    }
    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters();
    }

    public virtual void Exit()
    {
        anim.SetBool(animBoolName, false);
    }

    public void AnimationTrigger()//动画触发器
    {
        triggerCalled = true;
    }

    public virtual void UpdateAnimationParameters()//更新动画参数
    {

    }

    
}
