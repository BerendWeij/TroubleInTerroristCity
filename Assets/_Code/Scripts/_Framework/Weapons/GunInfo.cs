using UnityEngine;

[CreateAssetMenu(fileName = "Gun Info", menuName = "Equipment/Gun")]
public class GunInfo : ProjectileWeaponInfo
{
    [Group("7: ")] public GunSettings.Shooting Projectile;
}