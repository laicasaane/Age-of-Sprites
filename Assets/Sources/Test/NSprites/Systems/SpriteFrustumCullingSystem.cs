﻿using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NSprites
{
    public partial class SpriteFrustumCullingSystem : SystemBase
    {
        private Camera _camera;
        private EntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _camera = Camera.main;
            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var leftBottomPoint = _camera.ScreenToWorldPoint(new Vector3(0f,0f,0f));
            var rightUpPoint = _camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0f));
            var cameraViewBounds = new float4(leftBottomPoint.x, rightUpPoint.x, leftBottomPoint.y, rightUpPoint.y);

            var ecbDisableRendering = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var disableHandle = Entities
                .WithAll<SpriteRendererTag>()
                .WithNone<CullSpriteTag>()
                .ForEach((Entity entity, int entityInQueryIndex, in WorldPosition2D worldPosition, in Scale2D size, in Pivot pivot) =>
                {
                    var viewPosition = worldPosition.value - size.value * pivot.value;
                    if(!IsInsideCameraBounds(GetRect(viewPosition, size.value), cameraViewBounds))
                        ecbDisableRendering.AddComponent<CullSpriteTag>(entityInQueryIndex, entity);
                }).ScheduleParallel(Dependency);

            var ecbEnableRendering = _ecbSystem.CreateCommandBuffer().AsParallelWriter();
            var enableHandle = Entities
                .WithAll<SpriteRendererTag>()
                .WithAll<CullSpriteTag>()
                .ForEach((Entity entity, int entityInQueryIndex, in WorldPosition2D worldPosition, in Scale2D size, in Pivot pivot) =>
                {
                    var viewPosition = worldPosition.value - size.value * pivot.value;
                    if(IsInsideCameraBounds(GetRect(worldPosition.value, size.value), cameraViewBounds))
                        ecbEnableRendering.RemoveComponent<CullSpriteTag>(entityInQueryIndex, entity);
                }).ScheduleParallel(Dependency);

            Dependency = JobHandle.CombineDependencies(disableHandle, enableHandle);
            _ecbSystem.AddJobHandleForProducer(Dependency);
            
        }
        private static float4 GetRect(in float2 position, in float2 size)
        {
            var leftBottomPoint = position;
            var rightUpPoint = position + size;
            return new float4(leftBottomPoint.x, rightUpPoint.x, leftBottomPoint.y, rightUpPoint.y);
        }
        private static bool IsInsideCameraBounds(in float2 position, in float4 cameraViewBounds)
        {
            return position.x > cameraViewBounds.x &&
                position.x < cameraViewBounds.y &&
                position.y > cameraViewBounds.z &&
                position.y < cameraViewBounds.w;
        }
        private static bool IsInsideCameraBounds(in float4 rect, in float4 cameraViewBounds)
        {
            return IsInsideCameraBounds(new float2(rect.x, rect.z), cameraViewBounds) ||
                IsInsideCameraBounds(new float2(rect.x, rect.w), cameraViewBounds) ||
                IsInsideCameraBounds(new float2(rect.y, rect.z), cameraViewBounds) ||
                IsInsideCameraBounds(new float2(rect.y, rect.w), cameraViewBounds);
        }
    }
}