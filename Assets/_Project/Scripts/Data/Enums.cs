namespace TheLastBullet.Data
{
    public enum WeaponId
    {
        Pistol, Shotgun, Smg, Rifle, Crossbow, GrenadeLauncher, Flamethrower,
        Minigun, Railgun, FreezeRay, DualPistols, Katana, AcidGun, TeslaCoil,
        HarpoonGun, NailGun, PlasmaCannon, SawbladeLauncher, ToxicSprayer, BoomerangBlade
    }

    public enum ZombieKind
    {
        Walker, Runner, Crawler, Brute, Spitter, Exploder, Leaper, Screamer,
        ZombieDog, Armored, Bloater, Healer, Shieldbearer, Ghost, FireZombie, Spawner
    }

    public enum BossKind
    {
        Charger, Summoner, SpitterKing, TheButcher, PlagueQueen
    }

    public enum PerkId
    {
        FasterReload, BiggerMag, CritChance, MoveSpeed, MaxHealth,
        SecondWeapon, Magnet, DamageUp
    }

    public enum GameMode
    {
        Endless, Timed, DefendPoint, Escort, BossRush, Hardcore, DailyChallenge
    }

    public enum GamePhase
    {
        Menu, Playing, BetweenWaves, GameOver
    }

    public enum Quality
    {
        Low, Medium, High, Ultra
    }

    public enum Rarity
    {
        Common, Rare, Epic, Legendary
    }

    public enum WeaponMod
    {
        ExtendedMag, FasterReload, Scope
    }

    public enum MapId
    {
        CityStreet, Graveyard, Mall, Farm, Hospital, Subway, Rooftop, SnowTown,
        Prison, Airport, Casino, School, Junkyard, Beach, Forest, Laboratory,
        Desert, Castle, Stadium, Sewer
    }

    public enum HairStyle
    {
        Bald, Buzz, Short, Mohawk, Long, Ponytail
    }

    public enum Accessory
    {
        None, Cap, Headphones, Sunglasses, Bandana, Beanie
    }

    public enum PickupKind
    {
        Health, Ammo, Grenade, Molotov, FreezeBomb, InstaKill
    }

    public enum TrapKind
    {
        Spike, Mine, ElectricFence
    }

    /// <summary>Events that can fire during a level. Unlocked progressively (see LevelData).</summary>
    public enum LevelEventId
    {
        Fog,
        SupplyDrop,
        HordeSurge,
        DoubleCoins,
        LastStand,
        MapHazard,
        NightFall,
        RainStorm
    }

    public enum MedalTier
    {
        None, Bronze, Silver, Gold, Platinum
    }
}
