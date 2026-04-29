using UnityEngine;

public interface IWeapon
{
    bool IsBroken();
    void OnPrimaryUse(GameObject user);
    void OnSecondaryUse(GameObject user);
}