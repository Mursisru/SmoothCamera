using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NOLoader.SmoothCamera
{
    /// <summary>Compiled field accessors — zero boxing/allocation on hot paths.</summary>
    internal static class OrbitFieldAccess
    {
        private const BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Func<CameraOrbitState, float> GetLookAtTargetLerpFn =
            CompileGetter<CameraOrbitState, float>("lookAtTargetLerp");
        private static readonly Func<CameraOrbitState, float> GetPanViewFn =
            CompileGetter<CameraOrbitState, float>("panView");
        private static readonly Func<CameraOrbitState, float> GetTiltViewFn =
            CompileGetter<CameraOrbitState, float>("tiltView");
        private static readonly Action<CameraOrbitState, float> SetPanViewFn =
            CompileSetter<CameraOrbitState, float>("panView");
        private static readonly Action<CameraOrbitState, float> SetTiltViewFn =
            CompileSetter<CameraOrbitState, float>("tiltView");
        private static readonly Func<CameraOrbitState, Vector3> GetFollowVectorFn =
            CompileGetter<CameraOrbitState, Vector3>("followVector");
        private static readonly Action<CameraOrbitState, Vector3> SetFollowVectorFn =
            CompileSetter<CameraOrbitState, Vector3>("followVector");
        private static readonly Func<CameraOrbitState, Vector3> GetFlatVelSmoothedFn =
            CompileGetter<CameraOrbitState, Vector3>("flatVelSmoothed");
        private static readonly Action<CameraOrbitState, Vector3> SetFlatVelSmoothedFn =
            CompileSetter<CameraOrbitState, Vector3>("flatVelSmoothed");
        private static readonly Func<CameraTVState, Vector2> GetTvPanTiltViewFn =
            CompileGetter<CameraTVState, Vector2>("panTiltView");
        private static readonly Func<CameraTVState, Vector2> GetTvDesiredPanTiltViewFn =
            CompileGetter<CameraTVState, Vector2>("desiredPanTiltView");
        private static readonly Action<CameraTVState, Vector2> SetTvPanTiltViewFn =
            CompileSetter<CameraTVState, Vector2>("panTiltView");
        private static readonly Action<CameraTVState, Vector2> SetTvDesiredPanTiltViewFn =
            CompileSetter<CameraTVState, Vector2>("desiredPanTiltView");
        private static readonly Action<CameraChaseState, bool> SetChaseShowHudFn =
            CompileSetter<CameraChaseState, bool>("showHUD");
        private static readonly Func<HUDBoresightState, Image?> GetBoresightImageFn =
            CompileGetter<HUDBoresightState, Image?>("boresight");

        private static readonly Dictionary<int, Image> BoresightCache = new Dictionary<int, Image>(4);

        internal static float GetLookAtTargetLerp(CameraOrbitState orbit)
            => GetLookAtTargetLerpFn(orbit);

        internal static float GetPanView(CameraOrbitState orbit)
            => GetPanViewFn(orbit);

        internal static float GetTiltView(CameraOrbitState orbit)
            => GetTiltViewFn(orbit);

        internal static void SetPanView(CameraOrbitState orbit, float value)
            => SetPanViewFn(orbit, value);

        internal static void SetTiltView(CameraOrbitState orbit, float value)
            => SetTiltViewFn(orbit, value);

        internal static Vector3 GetFollowVector(CameraOrbitState orbit)
            => GetFollowVectorFn(orbit);

        internal static void SetFollowVector(CameraOrbitState orbit, Vector3 value)
            => SetFollowVectorFn(orbit, value);

        internal static Vector3 GetFlatVelSmoothed(CameraOrbitState orbit)
            => GetFlatVelSmoothedFn(orbit);

        internal static void SetFlatVelSmoothed(CameraOrbitState orbit, Vector3 value)
            => SetFlatVelSmoothedFn(orbit, value);

        internal static Vector2 GetTvPanTiltView(CameraTVState tv)
            => GetTvPanTiltViewFn(tv);

        internal static Vector2 GetTvDesiredPanTiltView(CameraTVState tv)
            => GetTvDesiredPanTiltViewFn(tv);

        internal static void SetTvPanTiltView(CameraTVState tv, Vector2 value)
            => SetTvPanTiltViewFn(tv, value);

        internal static void SetTvDesiredPanTiltView(CameraTVState tv, Vector2 value)
            => SetTvDesiredPanTiltViewFn(tv, value);

        internal static void SetChaseShowHud(CameraChaseState chase, bool value)
            => SetChaseShowHudFn(chase, value);

        internal static Image? GetBoresightImage(HUDBoresightState state)
        {
            int key = state.GetInstanceID();
            if (BoresightCache.TryGetValue(key, out Image cached))
                return cached;

            Image? image = GetBoresightImageFn(state);
            if (image != null)
                BoresightCache[key] = image;
            return image;
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
