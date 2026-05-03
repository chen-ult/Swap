using UnityEngine;

public interface IMomentumSwappable
{
    Rigidbody2D MomentumRigidbody { get; }
    void ApplyMomentum(Vector2 momentum);
    void SetSelectedVisual(bool isSelected);
    void FlashSuccess();
}