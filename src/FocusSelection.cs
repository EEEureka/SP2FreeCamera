using Assets.Scripts;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.Combat;
using Assets.Scripts.Flight.WorldObjects.Vehicles.Land;
using Assets.Scripts.Flight.WorldObjects.Vehicles.Sea;
using Assets.Scripts.Multiplayer.ActivityFramework.Activities.MechInvasion;
using UnityEngine;

namespace SP2FreeCamera
{
    // Shared hit classification only. Camera rotation, smoothing and projection
    // remain the responsibility of each camera mode.
    internal static class FocusSelection
    {
        internal static FocusTarget Pick(
            Camera camera, Ray ray, Vector2 screenPosition, float maximumDistance,
            AircraftScript ignoredAircraft = null, Transform ignoredAvatar = null,
            bool includeReleasedWeapons = true)
        {
            int layerMask =
                (1 << Layers.DefaultLayer) |
                (1 << Layers.CarLayer) |
                (1 << Layers.AircraftInteractable) |
                (1 << Layers.TerrainLayer) |
                (1 << Layers.AircraftLayer) |
                (1 << Layers.CarrierDeck) |
                (1 << Layers.AircraftCollisionOnly) |
                (1 << Layers.AircraftCollisionNone) |
                (1 << Layers.RemoteAircraftLayer);
            RaycastHit[] hits = Physics.RaycastAll(
                ray, maximumDistance, layerMask, QueryTriggerInteraction.Collide);

            float partDistance = float.PositiveInfinity;
            float groundDistance = float.PositiveInfinity;
            float terrainDistance = float.PositiveInfinity;
            FocusTarget partTarget = null;
            FocusTarget groundTarget = null;
            FocusTarget terrainTarget = null;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                Collider collider = hit.collider;
                if (collider == null)
                {
                    continue;
                }
                int layer = collider.gameObject.layer;
                if (collider.isTrigger && layer != Layers.AircraftInteractable)
                {
                    continue;
                }
                PartScript part = collider.GetComponentInParent<PartScript>();
                if (IsIgnored(collider.transform, part, ignoredAircraft, ignoredAvatar))
                {
                    continue;
                }
                if (part != null && hit.distance < partDistance)
                {
                    partTarget = FocusTarget.CreatePart(part, collider, hit.point);
                    partDistance = hit.distance;
                    continue;
                }
                FocusTarget groundTargetHit = CreateGroundTarget(collider, hit.point);
                if (groundTargetHit != null && hit.distance < groundDistance)
                {
                    groundTarget = groundTargetHit;
                    groundDistance = hit.distance;
                    continue;
                }
                if (layer == Layers.TerrainLayer && hit.distance < terrainDistance)
                {
                    terrainTarget = FocusTarget.CreateTerrain(hit.point);
                    terrainDistance = hit.distance;
                }
            }

            ReleasedWeaponSelection weapon;
            if (includeReleasedWeapons && ReleasedWeaponSelectionHelper.TryFindNearScreenPoint(
                camera, screenPosition, maximumDistance, 8f,
                Mathf.Min(partDistance, Mathf.Min(groundDistance, terrainDistance)), out weapon))
            {
                FocusTarget weaponTarget = FocusTarget.CreateTransform(
                    weapon.AnchorTransform, weapon.WorldPosition,
                    Localization.Text(weapon.Kind == ReleasedWeaponKind.Missile ? "Missile" : "Bomb"));
                if (weaponTarget != null)
                {
                    return weaponTarget;
                }
            }
            if (partTarget != null && partDistance <= groundDistance && partDistance <= terrainDistance)
            {
                return partTarget;
            }
            return groundTarget != null && groundDistance <= terrainDistance
                ? groundTarget : terrainTarget;
        }

        private static bool IsIgnored(
            Transform hit, PartScript part, AircraftScript aircraft, Transform avatar)
        {
            // Do not change layers or collider state. First-person rays start
            // inside the local cockpit; only this pick ignores that local rig.
            return (aircraft != null &&
                ((part != null && part.Aircraft == aircraft) || hit.IsChildOf(aircraft.transform))) ||
                (avatar != null && hit.IsChildOf(avatar));
        }

        private static FocusTarget CreateGroundTarget(Collider collider, Vector3 hitPosition)
        {
            GroundTarget target = null;
            SinkableShipScript ship = collider.GetComponentInParent<SinkableShipScript>();
            if (ship != null)
            {
                target = ship.Target;
            }
            if (target == null)
            {
                SimpleGroundVehicleScript vehicle = collider.GetComponentInParent<SimpleGroundVehicleScript>();
                if (vehicle != null)
                {
                    target = vehicle.Target;
                }
            }
            if (target == null)
            {
                MechScript mech = collider.GetComponentInParent<MechScript>();
                if (mech != null)
                {
                    target = mech.Target;
                }
            }
            if (target == null || target.IsDead)
            {
                return null;
            }
            return FocusTarget.CreateDynamicGroundTarget(collider.transform, hitPosition,
                string.IsNullOrEmpty(target.Name) ? Localization.Text("GameTarget") : target.Name);
        }
    }
}
