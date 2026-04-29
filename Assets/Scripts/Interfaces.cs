using UnityEngine;

public interface IPickupable
{
    void OnPickedUp(GameObject picker);
    void OnDropped();
}

public interface IUsable
{
    void OnPrimaryUse(GameObject user);
    void OnSecondaryUse(GameObject user);
}

public interface IDamageable
{
    void TakeDamage(int damage);
}