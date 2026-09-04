using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers.Weapons;
using UnityEngine;

namespace SP2FreeCamera
{
    internal enum ReleasedWeaponKind
    {
        Missile,
        Bomb
    }

    internal struct ReleasedWeaponSelection
    {
        internal ReleasedWeaponSelection(
            ReleasedWeaponKind kind,
            Transform anchorTransform,
            Vector3 worldPosition,
            float cameraDistance)
        {
            Kind = kind;
            AnchorTransform = anchorTransform;
            WorldPosition = worldPosition;
            CameraDistance = cameraDistance;
        }

        internal ReleasedWeaponKind Kind { get; private set; }

        internal Transform AnchorTransform { get; private set; }

        internal Vector3 WorldPosition { get; private set; }

        internal float CameraDistance { get; private set; }
    }

    /// <summary>
    /// Provides a click-only screen-space fallback for released missiles and bombs.
    /// These weapons keep their PartScript after separation, but their small colliders
    /// are easy for an exact physics ray to miss at long range.
    /// </summary>
    internal static class ReleasedWeaponSelectionHelper
    {
        private const float ProjectionEpsilon = 0.000001f;
        private const float DistanceTieEpsilon = 0.0001f;
        private const float OcclusionSlack = 0.5f;

        internal static bool TryFindNearScreenPoint(
            Camera camera,
            Vector2 screenPosition,
            float maximumDistance,
            float edgeTolerancePixels,
            float nearestOccluderDistance,
            out ReleasedWeaponSelection selection)
        {
            selection = default(ReleasedWeaponSelection);
            if (camera == null ||
                !IsFinite(screenPosition.x) ||
                !IsFinite(screenPosition.y) ||
                !IsFinite(maximumDistance) ||
                maximumDistance <= 0f)
            {
                return false;
            }

            edgeTolerancePixels = IsFinite(edgeTolerancePixels)
                ? Mathf.Clamp(edgeTolerancePixels, 0f, 64f)
                : 0f;
            bool occluderLimitsSelection = IsFinite(nearestOccluderDistance) &&
                nearestOccluderDistance >= 0f;

            Candidate best = default(Candidate);
            best.ScreenDistanceSquared = float.PositiveInfinity;
            best.CameraDistance = float.PositiveInfinity;

            MissileScript[] missiles = Object.FindObjectsByType<MissileScript>(
                FindObjectsSortMode.None);
            for (int i = 0; i < missiles.Length; i++)
            {
                MissileScript missile = missiles[i];
                if (!IsSelectable(missile))
                {
                    continue;
                }

                PartScript part = missile.PartScript;
                Collider collider = GetUsablePrimaryCollider(part);
                ConsiderCandidate(
                    camera,
                    screenPosition,
                    maximumDistance,
                    edgeTolerancePixels,
                    occluderLimitsSelection,
                    nearestOccluderDistance,
                    ReleasedWeaponKind.Missile,
                    missile.transform,
                    collider,
                    ref best);
            }

            BombScript[] bombs = Object.FindObjectsByType<BombScript>(
                FindObjectsSortMode.None);
            for (int i = 0; i < bombs.Length; i++)
            {
                BombScript bomb = bombs[i];
                if (!IsSelectable(bomb))
                {
                    continue;
                }

                PartScript part = bomb.PartScript;
                Collider collider = GetUsablePrimaryCollider(part);
                ConsiderCandidate(
                    camera,
                    screenPosition,
                    maximumDistance,
                    edgeTolerancePixels,
                    occluderLimitsSelection,
                    nearestOccluderDistance,
                    ReleasedWeaponKind.Bomb,
                    bomb.transform,
                    collider,
                    ref best);
            }

            if (best.AnchorTransform == null)
            {
                return false;
            }

            selection = new ReleasedWeaponSelection(
                best.Kind,
                best.AnchorTransform,
                best.WorldPosition,
                best.CameraDistance);
            return true;
        }

        private static bool IsSelectable(MissileScript missile)
        {
            if (missile == null || missile.IsDestroyed ||
                (!missile.Fired && !missile.Launched) ||
                missile.gameObject == null || !missile.gameObject.activeInHierarchy)
            {
                return false;
            }

            return HasUsablePart(missile.PartScript);
        }

        private static bool IsSelectable(BombScript bomb)
        {
            if (bomb == null || bomb.IsDestroyed ||
                (!bomb.Fired && !bomb.Launched) ||
                bomb.gameObject == null || !bomb.gameObject.activeInHierarchy)
            {
                return false;
            }

            return HasUsablePart(bomb.PartScript);
        }

        private static bool HasUsablePart(PartScript part)
        {
            return part != null &&
                part.gameObject != null &&
                part.gameObject.activeInHierarchy &&
                part.Body != null &&
                part.Body.RigidBody != null;
        }

        private static Collider GetUsablePrimaryCollider(PartScript part)
        {
            Collider collider = part != null ? part.PrimaryPartCollider : null;
            if (collider == null || !collider.enabled ||
                collider.gameObject == null || !collider.gameObject.activeInHierarchy)
            {
                return null;
            }

            return collider;
        }

        private static void ConsiderCandidate(
            Camera camera,
            Vector2 screenPosition,
            float maximumDistance,
            float edgeTolerancePixels,
            bool occluderLimitsSelection,
            float nearestOccluderDistance,
            ReleasedWeaponKind kind,
            Transform anchorTransform,
            Collider collider,
            ref Candidate best)
        {
            if (anchorTransform == null || anchorTransform.gameObject == null ||
                !anchorTransform.gameObject.activeInHierarchy)
            {
                return;
            }

            Bounds bounds;
            bool hasBounds = collider != null;
            if (hasBounds)
            {
                bounds = collider.bounds;
            }
            else
            {
                bounds = new Bounds(anchorTransform.position, Vector3.zero);
            }

            Vector3 worldPosition = bounds.center;
            if (!IsFinite(worldPosition))
            {
                return;
            }

            float cameraDistance = Vector3.Distance(camera.transform.position, worldPosition);
            if (!IsFinite(cameraDistance) || cameraDistance > maximumDistance)
            {
                return;
            }

            float boundsRadius = hasBounds && IsFinite(bounds.extents)
                ? bounds.extents.magnitude
                : 0f;
            float nearestCandidateDistance = Mathf.Max(0f, cameraDistance - boundsRadius);
            if (occluderLimitsSelection &&
                nearestCandidateDistance > nearestOccluderDistance + OcclusionSlack)
            {
                return;
            }

            Rect screenBounds;
            if (!TryGetScreenBounds(camera, bounds, out screenBounds))
            {
                return;
            }

            float screenDistanceSquared = DistanceSquaredToRect(screenPosition, screenBounds);
            float allowedDistanceSquared = edgeTolerancePixels * edgeTolerancePixels;
            if (screenDistanceSquared > allowedDistanceSquared)
            {
                return;
            }

            bool closerToPointer =
                screenDistanceSquared < best.ScreenDistanceSquared - DistanceTieEpsilon;
            bool equallyCloseToPointer =
                Mathf.Abs(screenDistanceSquared - best.ScreenDistanceSquared) <=
                DistanceTieEpsilon;
            if (!closerToPointer &&
                (!equallyCloseToPointer || cameraDistance >= best.CameraDistance))
            {
                return;
            }

            best.Kind = kind;
            best.AnchorTransform = anchorTransform;
            best.WorldPosition = worldPosition;
            best.ScreenDistanceSquared = screenDistanceSquared;
            best.CameraDistance = cameraDistance;
        }

        private static bool TryGetScreenBounds(
            Camera camera,
            Bounds worldBounds,
            out Rect screenBounds)
        {
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            int projectedPointCount = 0;

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector2 screenPoint;
                        if (!TryProjectWithCurrentMatrix(camera, corner, out screenPoint))
                        {
                            continue;
                        }

                        projectedPointCount++;
                        minX = Mathf.Min(minX, screenPoint.x);
                        minY = Mathf.Min(minY, screenPoint.y);
                        maxX = Mathf.Max(maxX, screenPoint.x);
                        maxY = Mathf.Max(maxY, screenPoint.y);
                    }
                }
            }

            if (projectedPointCount == 0)
            {
                screenBounds = default(Rect);
                return false;
            }

            Rect pixelRect = camera.pixelRect;
            if (maxX < pixelRect.xMin || minX > pixelRect.xMax ||
                maxY < pixelRect.yMin || minY > pixelRect.yMax)
            {
                screenBounds = default(Rect);
                return false;
            }

            screenBounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        private static bool TryProjectWithCurrentMatrix(
            Camera camera,
            Vector3 worldPosition,
            out Vector2 screenPosition)
        {
            Vector3 viewPosition = camera.worldToCameraMatrix.MultiplyPoint(worldPosition);
            if (!IsFinite(viewPosition) || viewPosition.z >= -ProjectionEpsilon)
            {
                screenPosition = default(Vector2);
                return false;
            }

            Vector4 clipPosition = camera.projectionMatrix * new Vector4(
                viewPosition.x,
                viewPosition.y,
                viewPosition.z,
                1f);
            if (!IsFinite(clipPosition.x) || !IsFinite(clipPosition.y) ||
                !IsFinite(clipPosition.w) || clipPosition.w <= ProjectionEpsilon)
            {
                screenPosition = default(Vector2);
                return false;
            }

            float inverseW = 1f / clipPosition.w;
            float normalizedX = clipPosition.x * inverseW;
            float normalizedY = clipPosition.y * inverseW;
            if (!IsFinite(normalizedX) || !IsFinite(normalizedY))
            {
                screenPosition = default(Vector2);
                return false;
            }

            Rect pixelRect = camera.pixelRect;
            screenPosition = new Vector2(
                pixelRect.xMin + (normalizedX + 1f) * 0.5f * pixelRect.width,
                pixelRect.yMin + (normalizedY + 1f) * 0.5f * pixelRect.height);
            return IsFinite(screenPosition.x) && IsFinite(screenPosition.y);
        }

        private static float DistanceSquaredToRect(Vector2 point, Rect rect)
        {
            float deltaX = 0f;
            if (point.x < rect.xMin)
            {
                deltaX = rect.xMin - point.x;
            }
            else if (point.x > rect.xMax)
            {
                deltaX = point.x - rect.xMax;
            }

            float deltaY = 0f;
            if (point.y < rect.yMin)
            {
                deltaY = rect.yMin - point.y;
            }
            else if (point.y > rect.yMax)
            {
                deltaY = point.y - rect.yMax;
            }

            return deltaX * deltaX + deltaY * deltaY;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private struct Candidate
        {
            internal ReleasedWeaponKind Kind;
            internal Transform AnchorTransform;
            internal Vector3 WorldPosition;
            internal float ScreenDistanceSquared;
            internal float CameraDistance;
        }
    }
}
