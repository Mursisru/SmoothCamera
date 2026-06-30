using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NOLoader.SmoothCamera
{
    internal static class PatchReflection
    {
        private const BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Action<CameraCockpitState, Vector3> SetCockpitCamRelativePosFn =
            CompileSetter<CameraCockpitState, Vector3>("camRelativePos");
        private static readonly Action<CameraCockpitState, Vector3> SetCockpitCamRelativeVelFn =
            CompileSetter<CameraCockpitState, Vector3>("camRelativeVel");
        private static readonly Action<CameraCockpitState, float> SetCockpitAntiSlumpFn =
            CompileSetter<CameraCockpitState, float>("antiSlump");
        private static readonly Func<GLOC, float> GetGlocBloodPressureFn =
            CompileGetter<GLOC, float>("bloodPressure");
        private static readonly Func<GLOC, Image?> GetGlocBlackoutImageFn =
            CompileGetter<GLOC, Image?>("blackoutImage");
        private static readonly Func<GLOC, ColorAdjustments?> GetGlocColorAdjustmentsFn =
            CompileGetter<GLOC, ColorAdjustments?>("colorAdjustments");
        private static readonly Func<GLOC, Vignette?> GetGlocVignetteFn =
            CompileGetter<GLOC, Vignette?>("vignette");

        private static readonly Dictionary<int, GlocUiCache> GlocCaches = new Dictionary<int, GlocUiCache>(2);

        private sealed class GlocUiCache
        {
            internal Image? Blackout;
            internal ColorAdjustments? ColorAdjustments;
            internal Vignette? Vignette;
        }

        internal static void ResetCockpitEnterState(CameraCockpitState state)
        {
            SetCockpitCamRelativePosFn(state, Vector3.zero);
            SetCockpitCamRelativeVelFn(state, Vector3.zero);
            SetCockpitAntiSlumpFn(state, 0f);
        }

        internal static float GetGlocBloodPressure(GLOC gloc)
            => GetGlocBloodPressureFn(gloc);

        internal static Image? GetGlocBlackoutImage(GLOC gloc)
            => ResolveGlocCache(gloc).Blackout;

        internal static ColorAdjustments? GetGlocColorAdjustments(GLOC gloc)
            => ResolveGlocCache(gloc).ColorAdjustments;

        internal static Vignette? GetGlocVignette(GLOC gloc)
            => ResolveGlocCache(gloc).Vignette;

        private static GlocUiCache ResolveGlocCache(GLOC gloc)
        {
            int key = gloc.GetInstanceID();
            if (GlocCaches.TryGetValue(key, out GlocUiCache? cache))
                return cache;

            cache = new GlocUiCache
            {
                Blackout = GetGlocBlackoutImageFn(gloc),
                ColorAdjustments = GetGlocColorAdjustmentsFn(gloc),
                Vignette = GetGlocVignetteFn(gloc)
            };
            GlocCaches[key] = cache;
            return cache;
        }

        private static Func<TTarget, TField> CompileGetter<TTarget, TField>(string fieldName)
        {
            FieldInfo field = typeof(TTarget).GetField(fieldName, InstanceAny)!;
            ParameterExpression target = Expression.Parameter(typeof(TTarget), "target");
            return Expression.Lambda<Func<TTarget, TField>>(
                Expression.Field(target, field), target).Compile();
        }

        private static Action<TTarget, TField> CompileSetter<TTarget, TField>(string fieldName)
        {
            FieldInfo field = typeof(TTarget).GetField(fieldName, InstanceAny)!;
            ParameterExpression target = Expression.Parameter(typeof(TTarget), "target");
            ParameterExpression value = Expression.Parameter(typeof(TField), "value");
            return Expression.Lambda<Action<TTarget, TField>>(
                Expression.Assign(Expression.Field(target, field), value), target, value).Compile();
        }
    }
}
