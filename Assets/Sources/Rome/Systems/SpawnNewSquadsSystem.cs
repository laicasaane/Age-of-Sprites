﻿using NSprites;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[AlwaysUpdateSystem]
public partial struct SpawnNewSquadsSystem : ISystem
{
    private EntityQuery _soldierRequireQuery;
    private EntityArchetype _squadArchetype;

    public void OnCreate(ref SystemState state)
    {
        _soldierRequireQuery = state.GetEntityQuery(typeof(RequireSoldier));
        _squadArchetype = state.EntityManager.CreateArchetype
        (
            typeof(SoldierLink),
            typeof(RequireSoldier),
            typeof(SquadSettings),

            typeof(WorldPosition2D),
            typeof(PrevWorldPosition2D)
        );
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        if (_soldierRequireQuery.CalculateChunkCount() != 0
            || !state.TryGetSingleton<MapSettings>(out var mapSettings)
            || !state.TryGetSingleton<SquadDefaultSettings>(out var squadDefaultSettings))
            return;

        var rand = new Random((uint)System.DateTime.Now.Ticks);
        var pos = rand.NextFloat2(mapSettings.size.c0, mapSettings.size.c1);
        var resolution = rand.NextInt2(new int2(5), new int2(20));
        var soldierCount = resolution.x * resolution.y;

        var squadEntity = state.EntityManager.CreateEntity(_squadArchetype);
        state.EntityManager.GetBuffer<SoldierLink>(squadEntity).EnsureCapacity(soldierCount);
        state.EntityManager.SetComponentData(squadEntity, new SquadSettings
        {
            squadResolution = resolution,
            soldierMargin = squadDefaultSettings.defaultSettings.soldierMargin
        });
        state.EntityManager.SetComponentData(squadEntity, new RequireSoldier { count = soldierCount });
        state.EntityManager.SetComponentData(squadEntity, new WorldPosition2D { value = pos });
        state.EntityManager.SetComponentData(squadEntity, new PrevWorldPosition2D { value = pos });
    }
}