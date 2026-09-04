using Assets.Scripts;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.Combat;
using Assets.Scripts.Multiplayer.ActivityFramework.Activities.MechInvasion;
using UnityEngine;

namespace SP2FreeCamera
{
    internal enum FocusTargetKind
    {
        Terrain,
        Part,
        Transform,
        DynamicGroundTarget,
        GameTarget,
        Self
    }

    internal sealed class FocusTarget
    {
        private readonly FocusTargetKind _kind;
        private readonly Vector3 _absoluteTerrainPosition;
        private readonly PartScript _part;
        private readonly Transform _colliderTransform;
        private readonly Vector3 _colliderLocalPosition;
        private readonly Vector3 _partLocalPosition;
        private readonly Transform _trackedTransform;
        private readonly Vector3 _trackedLocalPosition;
        private readonly Target _gameTarget;
        private readonly FlightScenePlayer _targetPlayer;
        private readonly FlightScenePlayer _player;
        private readonly string _displayName;

        private FocusTarget(Vector3 absoluteTerrainPosition)
        {
            _kind = FocusTargetKind.Terrain;
            _absoluteTerrainPosition = absoluteTerrainPosition;
            _displayName = "地形点";
        }

        private FocusTarget(
            PartScript part,
            Transform colliderTransform,
            Vector3 colliderLocalPosition,
            Vector3 partLocalPosition)
        {
            _kind = FocusTargetKind.Part;
            _part = part;
            _colliderTransform = colliderTransform;
            _colliderLocalPosition = colliderLocalPosition;
            _partLocalPosition = partLocalPosition;
            _displayName = part != null && !string.IsNullOrEmpty(part.name)
                ? "部件: " + part.name
                : "飞机部件";
        }

        private FocusTarget(
            FocusTargetKind kind,
            Transform trackedTransform,
            Vector3 localPosition,
            string displayName)
        {
            _kind = kind;
            _trackedTransform = trackedTransform;
            _trackedLocalPosition = localPosition;
            _displayName = displayName;
        }

        private FocusTarget(Target gameTarget)
        {
            _kind = FocusTargetKind.GameTarget;
            _gameTarget = gameTarget;
            _targetPlayer = gameTarget != null ? gameTarget.Player : null;
            _displayName = "游戏目标";
        }

        private FocusTarget(FlightScenePlayer player)
        {
            _kind = FocusTargetKind.Self;
            _player = player;
            _displayName = "自己";
        }

        internal FocusTargetKind Kind
        {
            get { return _kind; }
        }

        internal string DisplayName
        {
            get
            {
                if (_kind == FocusTargetKind.Self && _player != null)
                {
                    return _player.Aircraft != null ? "自己的载具" : "自己的玩家";
                }

                if (_kind == FocusTargetKind.GameTarget && _gameTarget != null)
                {
                    if (_gameTarget is LaserTarget)
                    {
                        return Localization.Text("LaserTarget");
                    }

                    return !string.IsNullOrEmpty(_gameTarget.Name)
                        ? _gameTarget.Name
                        : Localization.Text("GameTarget");
                }

                return _displayName;
            }
        }

        internal static FocusTarget CreateTerrain(Vector3 floatingOriginPosition)
        {
            Vector3 absolutePosition = Utility.ConvertFloatingOriginToAbsolutePosition(floatingOriginPosition);
            return new FocusTarget(absolutePosition);
        }

        internal static FocusTarget CreatePart(PartScript part, Collider collider, Vector3 hitPosition)
        {
            Transform colliderTransform = collider != null ? collider.transform : null;
            Vector3 colliderLocalPosition = colliderTransform != null
                ? colliderTransform.InverseTransformPoint(hitPosition)
                : Vector3.zero;
            Vector3 partLocalPosition = part.transform.InverseTransformPoint(hitPosition);
            return new FocusTarget(part, colliderTransform, colliderLocalPosition, partLocalPosition);
        }

        internal static FocusTarget CreateTransform(
            Transform trackedTransform,
            Vector3 worldPosition,
            string displayName)
        {
            if (trackedTransform == null)
            {
                return null;
            }

            return new FocusTarget(
                FocusTargetKind.Transform,
                trackedTransform,
                trackedTransform.InverseTransformPoint(worldPosition),
                displayName);
        }

        internal static FocusTarget CreateDynamicGroundTarget(
            Transform trackedTransform,
            Vector3 worldPosition,
            string displayName)
        {
            if (trackedTransform == null)
            {
                return null;
            }

            return new FocusTarget(
                FocusTargetKind.DynamicGroundTarget,
                trackedTransform,
                trackedTransform.InverseTransformPoint(worldPosition),
                displayName);
        }

        internal static FocusTarget CreateGameTarget(Target gameTarget)
        {
            return gameTarget != null ? new FocusTarget(gameTarget) : null;
        }

        internal static FocusTarget CreateSelf(FlightScenePlayer player)
        {
            return player != null ? new FocusTarget(player) : null;
        }

        internal bool TryGetFloatingOriginPosition(out Vector3 position)
        {
            if (_kind == FocusTargetKind.Terrain)
            {
                position = Utility.ConvertAbsoluteToFloatingOriginPosition(_absoluteTerrainPosition);
                return true;
            }

            if (_kind == FocusTargetKind.Transform ||
                _kind == FocusTargetKind.DynamicGroundTarget)
            {
                if (_trackedTransform == null || _trackedTransform.gameObject == null ||
                    !_trackedTransform.gameObject.activeInHierarchy)
                {
                    position = default(Vector3);
                    return false;
                }

                position = _trackedTransform.TransformPoint(_trackedLocalPosition);
                return true;
            }

            if (_kind == FocusTargetKind.GameTarget)
            {
                return TryGetGameTargetPosition(out position);
            }

            if (_kind == FocusTargetKind.Self)
            {
                return TryGetSelfPosition(out position);
            }

            if (_part == null || _part.gameObject == null || !_part.gameObject.activeInHierarchy)
            {
                position = default(Vector3);
                return false;
            }

            if (_colliderTransform != null &&
                _colliderTransform.gameObject != null &&
                _colliderTransform.gameObject.activeInHierarchy)
            {
                position = _colliderTransform.TransformPoint(_colliderLocalPosition);
                return true;
            }

            position = _part.transform.TransformPoint(_partLocalPosition);
            return true;
        }

        internal bool TryGetKinematicSample(
            out Vector3 position,
            out Vector3 pointVelocity,
            out bool hasDirectVelocity,
            out bool predictsFromFixedTime,
            out int motionSourceId)
        {
            pointVelocity = Vector3.zero;
            hasDirectVelocity = false;
            predictsFromFixedTime = false;
            motionSourceId = 0;

            if (!TryGetFloatingOriginPosition(out position) || !IsFinite(position))
            {
                return false;
            }

            if (_kind == FocusTargetKind.Terrain)
            {
                hasDirectVelocity = true;
                motionSourceId = 1;
                return true;
            }

            if (_kind == FocusTargetKind.Part)
            {
                Transform pointTransform = IsActive(_colliderTransform)
                    ? _colliderTransform
                    : _part != null ? _part.transform : null;
                var body = _part != null ? _part.Body : null;
                motionSourceId = GetMotionSourceId(2, pointTransform, body);
                var rigidBody = body != null ? body.RigidBody : null;
                if (rigidBody != null && !rigidBody.IsDead &&
                    rigidBody.activeInHierarchy)
                {
                    Vector3 velocity = rigidBody.GetPointVelocity(position);
                    if (IsFinite(velocity))
                    {
                        pointVelocity = velocity;
                        hasDirectVelocity = true;
                        Rigidbody physicsBody = rigidBody.PhysxRigidBody;
                        predictsFromFixedTime = physicsBody == null ||
                            physicsBody.interpolation == RigidbodyInterpolation.None;
                    }
                }

                return true;
            }

            if (_kind == FocusTargetKind.Transform ||
                _kind == FocusTargetKind.DynamicGroundTarget)
            {
                Rigidbody rigidBody = _trackedTransform != null
                    ? _trackedTransform.GetComponentInParent<Rigidbody>()
                    : null;
                motionSourceId = GetMotionSourceId(3, _trackedTransform, rigidBody);
                if (rigidBody != null && rigidBody.gameObject != null &&
                    rigidBody.gameObject.activeInHierarchy)
                {
                    Vector3 velocity = rigidBody.GetPointVelocity(position);
                    bool transformDrivenMech = _kind == FocusTargetKind.DynamicGroundTarget &&
                        _trackedTransform.GetComponentInParent<MechScript>() != null;
                    bool stationaryKinematicBody = rigidBody.isKinematic &&
                        IsFinite(velocity) && velocity.sqrMagnitude <= 0.000001f;
                    if (IsFinite(velocity) &&
                        !transformDrivenMech && !stationaryKinematicBody)
                    {
                        pointVelocity = velocity;
                        hasDirectVelocity = true;
                        predictsFromFixedTime =
                            rigidBody.interpolation == RigidbodyInterpolation.None;
                    }
                }

                return true;
            }

            if (_kind == FocusTargetKind.GameTarget)
            {
                if (_targetPlayer != null)
                {
                    TryGetPlayerMotion(
                        _targetPlayer,
                        position,
                        false,
                        out pointVelocity,
                        out hasDirectVelocity,
                        out predictsFromFixedTime,
                        out motionSourceId);
                    return true;
                }

                motionSourceId = 4;
                if (!(_gameTarget is LaserTarget))
                {
                    Vector3 velocity = _gameTarget.Velocity;
                    if (IsMeaningfulVelocity(velocity))
                    {
                        pointVelocity = velocity;
                        hasDirectVelocity = true;
                        predictsFromFixedTime = true;
                    }
                }

                return true;
            }

            TryGetPlayerMotion(
                _player,
                position,
                true,
                out pointVelocity,
                out hasDirectVelocity,
                out predictsFromFixedTime,
                out motionSourceId);
            return true;
        }

        private bool TryGetGameTargetPosition(out Vector3 position)
        {
            position = default(Vector3);
            if (_gameTarget == null)
            {
                return false;
            }

            if (_targetPlayer != null)
            {
                if (_targetPlayer.IsUnloaded || _gameTarget.IsDead)
                {
                    return false;
                }

                return TryGetTrackedPlayerPosition(_targetPlayer, out position);
            }

            if (_gameTarget.IsDead)
            {
                return false;
            }

            LaserTarget laserTarget = _gameTarget as LaserTarget;
            if (laserTarget != null &&
                (!laserTarget.IsActive || laserTarget.TargetingPod == null))
            {
                return false;
            }

            position = _gameTarget.Position;
            return IsFinite(position);
        }

        private static bool TryGetTrackedPlayerPosition(
            FlightScenePlayer player,
            out Vector3 position)
        {
            position = default(Vector3);
            if (player == null || player.IsUnloaded)
            {
                return false;
            }

            Transform target = player.RepositionTarget;
            if (target != null)
            {
                position = target.position;
                if (IsFinite(position))
                {
                    return true;
                }
            }

            var aircraft = player.Aircraft;
            if (aircraft != null)
            {
                PartScript cockpit = aircraft.MainCockpit;
                if (cockpit != null)
                {
                    position = cockpit.transform.position;
                    if (IsFinite(position))
                    {
                        return true;
                    }
                }

                target = aircraft.OrientedCenterOfMassRigidBodies;
                if (target != null)
                {
                    position = target.position;
                    if (IsFinite(position))
                    {
                        return true;
                    }
                }

                position = aircraft.Position;
                if (IsFinite(position))
                {
                    return true;
                }
            }

            if (player.AvatarActive)
            {
                target = player.AvatarCameraTarget;
                if (target == null)
                {
                    GameObject avatar = player.Avatar;
                    target = avatar != null ? avatar.transform : null;
                }

                if (target != null)
                {
                    position = target.position;
                    if (IsFinite(position))
                    {
                        return true;
                    }
                }
            }

            position = player.FramePosition;
            return IsFinite(position);
        }

        private bool TryGetSelfPosition(out Vector3 position)
        {
            position = default(Vector3);
            FlightSceneScript scene = FlightSceneScript.Instance;
            if (_player == null || _player.IsUnloaded || scene == null ||
                !ReferenceEquals(scene.LocalPlayer, _player))
            {
                return false;
            }

            Transform target = _player.RepositionTarget;
            if (target != null)
            {
                position = target.position;
                return IsFinite(position);
            }

            var aircraft = _player.Aircraft;
            if (aircraft != null)
            {
                target = aircraft.OrientedCenterOfMassRigidBodies;
                if (target != null)
                {
                    position = target.position;
                }
                else if (aircraft.MainCockpit != null)
                {
                    position = aircraft.MainCockpit.transform.position;
                }
                else
                {
                    position = aircraft.Position;
                }

                return IsFinite(position);
            }

            if (!_player.AvatarActive)
            {
                return false;
            }

            target = _player.AvatarCameraTarget;
            if (target == null)
            {
                GameObject avatar = _player.Avatar;
                target = avatar != null ? avatar.transform : null;
            }

            if (target == null || target.gameObject == null ||
                !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            position = target.position;
            return IsFinite(position);
        }

        private static void TryGetPlayerMotion(
            FlightScenePlayer player,
            Vector3 position,
            bool preferCenterOfMass,
            out Vector3 pointVelocity,
            out bool hasDirectVelocity,
            out bool predictsFromFixedTime,
            out int motionSourceId)
        {
            pointVelocity = Vector3.zero;
            hasDirectVelocity = false;
            predictsFromFixedTime = false;
            motionSourceId = 0;
            if (player == null)
            {
                return;
            }

            Transform repositionTarget = player.RepositionTarget;
            if (repositionTarget != null)
            {
                motionSourceId = GetMotionSourceId(5, repositionTarget, null);
                return;
            }

            var aircraft = player.Aircraft;
            if (aircraft != null)
            {
                PartScript cockpit = aircraft.MainCockpit;
                var body = cockpit != null ? cockpit.Body : null;
                Transform anchor = preferCenterOfMass
                    ? aircraft.OrientedCenterOfMassRigidBodies
                    : cockpit != null ? cockpit.transform : aircraft.OrientedCenterOfMassRigidBodies;
                motionSourceId = GetMotionSourceId(6, anchor, body);

                var rigidBody = body != null ? body.RigidBody : null;
                if (rigidBody != null && !rigidBody.IsDead &&
                    rigidBody.activeInHierarchy)
                {
                    Vector3 velocity = rigidBody.GetPointVelocity(position);
                    if (IsFinite(velocity))
                    {
                        pointVelocity = velocity;
                        hasDirectVelocity = true;
                        Rigidbody physicsBody = rigidBody.PhysxRigidBody;
                        predictsFromFixedTime = physicsBody == null ||
                            physicsBody.interpolation == RigidbodyInterpolation.None;
                        return;
                    }
                }

                Vector3 aircraftVelocity = player.Velocity;
                if (IsMeaningfulVelocity(aircraftVelocity))
                {
                    pointVelocity = aircraftVelocity;
                    hasDirectVelocity = true;
                    predictsFromFixedTime = true;
                }
                return;
            }

            Transform avatarAnchor = player.AvatarCameraTarget;
            if (avatarAnchor == null)
            {
                GameObject avatar = player.Avatar;
                avatarAnchor = avatar != null ? avatar.transform : null;
            }

            motionSourceId = GetMotionSourceId(7, avatarAnchor, null);
            Vector3 playerVelocity = player.Velocity;
            if (IsMeaningfulVelocity(playerVelocity))
            {
                pointVelocity = playerVelocity;
                hasDirectVelocity = true;
            }
        }

        private static int GetMotionSourceId(
            int category,
            UnityEngine.Object primary,
            UnityEngine.Object secondary)
        {
            unchecked
            {
                int result = category * 486187739;
                result = result * 397 + (primary != null ? primary.GetInstanceID() : 0);
                result = result * 397 + (secondary != null ? secondary.GetInstanceID() : 0);
                return result;
            }
        }

        private static bool IsActive(Transform transform)
        {
            return transform != null && transform.gameObject != null &&
                transform.gameObject.activeInHierarchy;
        }

        private static bool IsFinite(Vector3 value)
        {
            return NumericUtility.IsFinite(value.x) &&
                NumericUtility.IsFinite(value.y) &&
                NumericUtility.IsFinite(value.z);
        }

        private static bool IsMeaningfulVelocity(Vector3 value)
        {
            return IsFinite(value) && value.sqrMagnitude > 0.000001f;
        }
    }
}
