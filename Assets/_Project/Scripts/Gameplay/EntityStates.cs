namespace TheLastBullet.Gameplay
{
    /// <summary>Death collapse: Falling plays a ~0.6s fall + ragdoll before removal.</summary>
    public enum DeathState
    {
        None,
        Falling
    }

    /// <summary>Attack lunge cycle for melee contact attacks.</summary>
    public enum AttackState
    {
        None,
        Windup,
        Lunge
    }

    /// <summary>How a zombie enters the arena — the spawn animation matches.</summary>
    public enum SpawnVariant
    {
        RiseFromGround,
        BurstManhole,
        DropFromAbove
    }
}
