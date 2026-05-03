using UnityEngine;

public class Entity_AnimationTrigger : MonoBehaviour
{
    private Entity entity;
    
    protected virtual void Awake()
    {
        entity = GetComponentInParent<Entity>();
        
    }

    private void CurrentStateTrigger()//¶¯»­×´Ì¬´¥·¢Æ÷
    {
        entity.CurrentStateAnimationTrigger();
    }

    private void AttackTrigger()//¹¥»÷´¥·¢Æ÷
    {
        
    }
}
