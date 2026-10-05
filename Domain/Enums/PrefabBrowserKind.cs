namespace JoksterCube.PingMark.Domain.Enums;

[System.Flags]
internal enum PrefabBrowserKind : ulong
{
    None = 0,
    DroppedItem = 1,
    Craftable = 1 << 1,
    Buildable = 1 << 2,
    Pickable = 1 << 3,
    Prop = 1 << 4,
    Stack = 1 << 5,
    Vehicle = 1 << 6,
    Creature = 1 << 7,
    AmbientCreature = 1 << 8,
    Ragdoll = 1 << 9,
    Spawner = 1 << 10,
    TriggerPoint = 1 << 11,
    Projectile = 1 << 12,
    Particle = 1 << 13,
    VisualFx = 1 << 14,
    Audio = 1 << 15,
    Destruction = 1 << 16,
    Fracture = 1 << 17,
    Lod = 1 << 18,
    Planting = 1 << 19,
    Seed = 1 << 20,
    TerrainTool = 1 << 21,
    SystemController = 1 << 22,
    Development = 1 << 23,
    PlayerDeath = 1 << 24,
    BossTrigger = 1 << 25,
    Cinematic = 1 << 26,
    Uncategorized = 1 << 27,
    Location = 1 << 28,
    Dungeon = 1 << 29,
    Vegetation = 1UL << 30,
    Geology = 1UL << 31,
    Mineable = 1UL << 32,
    Ruin = 1UL << 33,
    Container = 1UL << 34,
    Useful = 1UL << 35,
    Resource = 1UL << 36,
    BuildingPiece = 1UL << 37,
    Tree = 1UL << 38,
    Portal = 1UL << 39,
    Turret = 1UL << 40,
    All = DroppedItem | Craftable | Buildable | Pickable | Prop | Stack | Vehicle | Creature | AmbientCreature
        | Ragdoll | Spawner | TriggerPoint | Projectile | Particle | VisualFx | Audio | Destruction | Fracture
        | Lod | Planting | Seed | TerrainTool | SystemController | Development | PlayerDeath | BossTrigger | Cinematic | Uncategorized
        | Location | Dungeon | Vegetation | Geology | Mineable | Ruin | Container | Useful | Resource | BuildingPiece | Tree | Portal | Turret
}