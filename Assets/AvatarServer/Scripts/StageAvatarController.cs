using UnityEngine;
//using Windows.Kinect;

using System;
using System.Collections;
using System.Collections.Generic;
using com.rfilkov.kinect;


namespace com.rfilkov.components
{
    /// <summary>
    /// Avatar controller is the component that transfers the captured user motion to a humanoid model (avatar).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class StageAvatarController : MonoBehaviour
    {
        [Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, 2 - the 3rd one, etc.")]
        public int playerIndex = 0;

        [Tooltip("Whether the avatar is facing the player or not.")]
        public bool mirroredMovement = false;

        [Tooltip("Whether the avatar is allowed to move vertically or not.")]
        public bool verticalMovement = false;

        [Tooltip("Whether the finger orientations are allowed or not.")]
        public bool fingerOrientations = false;

        [Tooltip("Rate at which the avatar will move through the scene.")]
        public float moveRate = 1f;

        [Tooltip("Smooth factor used for avatar movements and joint rotations.")]
        public float smoothFactor = 10f;

        [Tooltip("Whether to update the avatar in LateUpdate(), instead of in Update(). Needed for Mecanim animation blending.")]
        public bool lateUpdateAvatar = false;

        [Tooltip("Whether z-axis movement needs to be inverted (Pos-Relative mode only).")]
        public bool posRelInvertedZ = false;

        [Tooltip("Whether the avatar's feet must stick to the ground.")]
        public bool groundedFeet = false;

        [Tooltip("Whether to apply the humanoid model's muscle limits or not.")]
        public bool applyMuscleLimits = false;

        [Tooltip("Whether to flip left and right, relative to the sensor.")]
        public bool flipLeftRight = false;

        [Tooltip("The transform that acts as the origin of the stage. This is used to calibrate the avatar's relative offset to a point in space.")]
        public Transform stageOrigin;

        [Tooltip("Horizontal offset of the avatar with respect to the position of user's spine-base.")]
        [Range(-0.5f, 0.5f)]
        public float horizontalOffset = 0f;

        [Tooltip("Vertical offset of the avatar with respect to the position of user's spine-base.")]
        [Range(-0.5f, 0.5f)]
        public float verticalOffset = 0f;

        [Tooltip("Forward offset of the avatar with respect to the position of user's spine-base.")]
        [Range(-0.5f, 0.5f)]
        public float forwardOffset = 0f;

        // userId of the player
        [NonSerialized]
        public ulong playerId = 0;


        // The body root node
        protected Transform bodyRoot;

        // Variable to hold all them bones. It will initialize the same size as initialRotations.
        protected Transform[] bones;
        //protected Transform[] fingerBones;

        // Rotations of the bones when the Kinect tracking starts.
        protected Quaternion[] initialRotations;
        protected Quaternion[] localRotations;
        protected bool[] isBoneDisabled;

        // Local rotations of finger bones
        protected Dictionary<HumanBodyBones, Quaternion> fingerBoneLocalRotations = new Dictionary<HumanBodyBones, Quaternion>();
        protected Dictionary<HumanBodyBones, Vector3> fingerBoneLocalAxes = new Dictionary<HumanBodyBones, Vector3>();

        // Initial position and rotation of the transform
        protected Vector3 initialPosition;
        protected Quaternion initialRotation;
        protected Vector3 initialHipsPosition;
        protected Quaternion initialHipsRotation;

        protected Vector3 bodyRootPosition;

        // Calibration Offset Variables for Character Position.
        [NonSerialized]
        public bool offsetCalibrated = false;
        protected Vector3 offsetPos = Vector3.zero;
        //protected float xOffset, yOffset, zOffset;
        //private Quaternion originalRotation;

        private Animator animatorComponent = null;
        private HumanPoseHandler humanPoseHandler = null;
        private HumanPose humanPose = new HumanPose();

        // whether the parent transform obeys physics
        protected bool isRigidBody = false;

        // private instance of the KinectManager
        protected KinectManager kinectManager;

        //// last hand events
        //private InteractionManager.HandEventType lastLeftHandEvent = InteractionManager.HandEventType.Release;
        //private InteractionManager.HandEventType lastRightHandEvent = InteractionManager.HandEventType.Release;

        //// fist states
        //private bool bLeftFistDone = false;
        //private bool bRightFistDone = false;

        // grounder constants and variables
        private const int raycastLayers = ~2;  // Ignore Raycast
        private const float maxFootDistanceGround = 0.02f;  // maximum distance from lower foot to the ground
        private const float maxFootDistanceTime = 0.2f; // 1.0f;  // maximum allowed time, the lower foot to be distant from the ground
        private Transform leftFoot, rightFoot;

        private float fFootDistanceInitial = 0f;
        private float fFootDistance = 0f;
        private float fFootDistanceTime = 0f;

        // background plane rectangle
        private Rect planeRect = new Rect();
        private bool planeRectSet = false;


        /// <summary>
        /// Gets the number of bone transforms (array length).
        /// </summary>
        /// <returns>The number of bone transforms.</returns>
        public int GetBoneTransformCount()
        {
            return bones != null ? bones.Length : 0;
        }

        /// <summary>
        /// Gets the bone transform by index.
        /// </summary>
        /// <returns>The bone transform.</returns>
        /// <param name="index">Index</param>
        public Transform GetBoneTransform(int index)
        {
            if (index >= 0 && bones != null && index < bones.Length)
            {
                return bones[index];
            }

            return null;
        }

        /// <summary>
        /// Disables the bone and optionally resets its orientation.
        /// </summary>
        /// <param name="index">Bone index.</param>
        /// <param name="resetBone">If set to <c>true</c> resets bone orientation.</param>
        public void DisableBone(int index, bool resetBone)
        {
            if (index >= 0 && index < bones.Length)
            {
                isBoneDisabled[index] = true;

                if (resetBone && bones[index] != null)
                {
                    bones[index].rotation = localRotations[index];
                }
            }
        }

        /// <summary>
        /// Enables the bone, so AvatarController could update its orientation.
        /// </summary>
        /// <param name="index">Bone index.</param>
        public void EnableBone(int index)
        {
            if (index >= 0 && index < bones.Length)
            {
                isBoneDisabled[index] = false;
            }
        }

        /// <summary>
        /// Determines whether the bone orientation update is enabled or not.
        /// </summary>
        /// <returns><c>true</c> if the bone update is enabled; otherwise, <c>false</c>.</returns>
        /// <param name="index">Bone index.</param>
        public bool IsBoneEnabled(int index)
        {
            if (index >= 0 && index < bones.Length)
            {
                return !isBoneDisabled[index];
            }

            return false;
        }

        /// <summary>
        /// Gets the bone index by joint type.
        /// </summary>
        /// <returns>The bone index.</returns>
        /// <param name="joint">Joint type</param>
        /// <param name="bMirrored">If set to <c>true</c> gets the mirrored joint index.</param>
        public int GetBoneIndexByJoint(KinectInterop.JointType joint, bool bMirrored)
        {
            int boneIndex = -1;

            if (jointMap2boneIndex.ContainsKey(joint))
            {
                boneIndex = !bMirrored ? jointMap2boneIndex[joint] : mirrorJointMap2boneIndex[joint];
            }

            return boneIndex;
        }

        /// <summary>
        /// Gets the list of AC-controlled mecanim bones.
        /// </summary>
        /// <returns>List of AC-controlled mecanim bones</returns>
        public List<HumanBodyBones> GetMecanimBones()
        {
            List<HumanBodyBones> alMecanimBones = new List<HumanBodyBones>();

            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                if (!boneIndex2MecanimMap.ContainsKey(boneIndex) || boneIndex >= 21)
                    continue;

                alMecanimBones.Add(boneIndex2MecanimMap[boneIndex]);
            }

            return alMecanimBones;
        }


        // transform caching gives performance boost since Unity calls GetComponent<Transform>() each time you call transform 
        private Transform _transformCache;
        public new Transform transform
        {
            get
            {
                if (!_transformCache)
                {
                    _transformCache = base.transform;
                }

                return _transformCache;
            }
        }


        public void Awake()
        {
            // check for double start
            if (bones != null)
                return;
            if (!gameObject.activeInHierarchy)
                return;

            // inits the bones array
            bones = new Transform[25];

            // get the animator reference
            animatorComponent = GetComponent<Animator>();

            // Map bones to the points the Kinect tracks
            MapBones();

            // Set model's arms to be in T-pose, if needed
            SetModelArmsInTpose();

            // Initial rotations and directions of the bones.
            initialRotations = new Quaternion[bones.Length];
            localRotations = new Quaternion[bones.Length];
            isBoneDisabled = new bool[bones.Length];

            // Get initial bone rotations
            GetInitialRotations();

            // enable all bones
            for (int i = 0; i < bones.Length; i++)
            {
                isBoneDisabled[i] = false;
            }

            // get initial distance to ground
            fFootDistanceInitial = GetDistanceToGround();
            fFootDistance = 0f;
            fFootDistanceTime = 0f;

            // if parent transform uses physics
            isRigidBody = (gameObject.GetComponent<Rigidbody>() != null);

            // get the pose handler reference
            if (animatorComponent && animatorComponent.avatar && animatorComponent.avatar.isHuman)
            {
                //Transform hipsTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
                //Transform rootTransform = hipsTransform.parent;
                Transform rootTransform = transform;

                humanPoseHandler = new HumanPoseHandler(animatorComponent.avatar, rootTransform);
                humanPoseHandler.GetHumanPose(ref humanPose);

                initialHipsPosition = (humanPose.bodyPosition - rootTransform.position);  // hipsTransform.position
                initialHipsRotation = humanPose.bodyRotation;
            }
        }

        public void MoveStageOriginToCurrentPosition() {
            if (stageOrigin == null) {
                return;
            }

            var newPos = transform.position;
            newPos.y = 0.0f;

            stageOrigin.position = newPos;
        }


        public void Update()
        {
            if(kinectManager == null)
            {
                kinectManager = KinectManager.Instance;
            }

            ulong userId = kinectManager ? kinectManager.GetUserIdByIndex(playerIndex) : 0;
            if (playerId != userId)
            {
                if (/**playerId == 0 &&*/ userId != 0)
                    SuccessfulCalibration(userId, false);
                else if (/**playerId != 0 &&*/ userId == 0)
                    ResetToInitialPosition();
            }

            if (!lateUpdateAvatar && playerId != 0)
            {
                UpdateAvatar(playerId);
            }
        }


        void LateUpdate()
        {
            if (lateUpdateAvatar && playerId != 0)
            {
                UpdateAvatar(playerId);
            }
        }


        // applies the muscle limits for humanoid avatar
        private void CheckMuscleLimits()
        {
            if (humanPoseHandler == null)
                return;

            humanPoseHandler.GetHumanPose(ref humanPose);

            //Debug.Log(playerId + " - Trans: " + transform.position + ", body: " + humanPose.bodyPosition);

            bool isPoseChanged = false;

            float muscleMin = -1f;
            float muscleMax = 1f;

            for (int i = 0; i < humanPose.muscles.Length; i++)
            {
                if (float.IsNaN(humanPose.muscles[i]))
                {
                    //humanPose.muscles[i] = 0f;
                    continue;
                }

                if (humanPose.muscles[i] < muscleMin)
                {
                    humanPose.muscles[i] = muscleMin;
                    isPoseChanged = true;
                }
                else if (humanPose.muscles[i] > muscleMax)
                {
                    humanPose.muscles[i] = muscleMax;
                    isPoseChanged = true;
                }
            }

            if (isPoseChanged)
            {
                //Quaternion localBodyRot = Quaternion.Inverse(transform.rotation) * humanPose.bodyRotation;
                Quaternion localBodyRot = Quaternion.Inverse(initialHipsRotation) * humanPose.bodyRotation;

                // recover the body position & orientation
                //humanPose.bodyPosition = Vector3.zero;
                //humanPose.bodyPosition.y = initialHipsPosition.y;
                humanPose.bodyPosition = initialHipsPosition;
                humanPose.bodyRotation = localBodyRot; // Quaternion.identity;

                humanPoseHandler.SetHumanPose(ref humanPose);
                //Debug.Log("  Human pose updated.");
            }

        }


        /// <summary>
        /// Updates the avatar each frame.
        /// </summary>
        /// <param name="UserID">User ID</param>
        public void UpdateAvatar(ulong UserID)
        {
            if (!gameObject.activeInHierarchy)
                return;

            // Get the KinectManager instance
            if (kinectManager == null)
            {
                kinectManager = KinectManager.Instance;
            }

            // move the avatar to its Kinect position
            MoveAvatar(UserID);

            // rotate the avatar bones
            for (var boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                if (!bones[boneIndex] || isBoneDisabled[boneIndex])
                    continue;

                if (boneIndex2JointMap.ContainsKey(boneIndex))
                {
                    KinectInterop.JointType joint = !(mirroredMovement ^ flipLeftRight) ?
                        boneIndex2JointMap[boneIndex] : boneIndex2MirrorJointMap[boneIndex];

                    TransformBone(UserID, joint, boneIndex, !(mirroredMovement ^ flipLeftRight));
                }
                else if (boneIndex >= 21 && boneIndex <= 24)
                {
                    // fingers or thumbs
                    if (fingerOrientations)
                    {
                        KinectInterop.JointType joint = !(mirroredMovement ^ flipLeftRight) ?
                            boneIndex2FingerMap[boneIndex] : boneIndex2MirrorFingerMap[boneIndex];

                        TransformSpecialBoneFingers(UserID, (int)joint, boneIndex, !(mirroredMovement ^ flipLeftRight));
                    }
                }
            }

            if (applyMuscleLimits && kinectManager && kinectManager.IsUserTracked(UserID))
            {
                // check for limits
                CheckMuscleLimits();
            }
        }

        /// <summary>
        /// Resets bones to their initial positions and rotations. This also releases avatar control from KM, by settings playerId to 0 
        /// </summary>
        public virtual void ResetToInitialPosition()
        {
            playerId = 0;
            //Debug.Log("ResetToInitialPosition. UserId: " + playerId);

            if (bones == null)
                return;

            // For each bone that was defined, reset to initial position.
            transform.rotation = Quaternion.identity;

            for (int pass = 0; pass < 2; pass++)  // 2 passes because clavicles are at the end
            {
                for (int i = 0; i < bones.Length; i++)
                {
                    if (bones[i] != null)
                    {
                        bones[i].rotation = initialRotations[i];
                    }
                }
            }

            // reset finger bones to initial position
            //Animator animatorComponent = GetComponent<Animator>();
            foreach (HumanBodyBones bone in fingerBoneLocalRotations.Keys)
            {
                Transform boneTransform = animatorComponent ? animatorComponent.GetBoneTransform(bone) : null;

                if (boneTransform)
                {
                    boneTransform.localRotation = fingerBoneLocalRotations[bone];
                }
            }

            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }

        /// <summary>
        /// Invoked on the successful calibration of the player.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        public virtual void SuccessfulCalibration(ulong userId, bool resetInitialTransform)
        {
            playerId = userId;
            //Debug.Log("SuccessfulCalibration. UserId: " + playerId);

            // reset initial position / rotation if needed 
            if (resetInitialTransform)
            {
                bodyRootPosition = transform.position;
                initialPosition = transform.position;
                initialRotation = transform.rotation;
            }

            transform.position = initialPosition;
            transform.rotation = initialRotation;

            // re-calibrate the position offset
            offsetCalibrated = false;
        }

        /// <summary>
        /// Moves the avatar to its initial/base position 
        /// </summary>
        /// <param name="position"> world position </param>
        /// <param name="rotation"> rotation offset </param>
        public void ResetInitialTransform(Vector3 position, Vector3 rotation)
        {
            bodyRootPosition = position;
            initialPosition = position;
            initialRotation = Quaternion.Euler(rotation);

            transform.position = initialPosition;
            transform.rotation = initialRotation;

            offsetCalibrated = false;       // this cause also calibrating kinect offset in moveAvatar function 
        }

        // Checks if the given joint is part of the legs
        protected bool IsLegJoint(KinectInterop.JointType joint)
        {
            return ((joint == KinectInterop.JointType.HipLeft) || (joint == KinectInterop.JointType.HipRight) ||
                    (joint == KinectInterop.JointType.KneeLeft) || (joint == KinectInterop.JointType.KneeRight) ||
                    (joint == KinectInterop.JointType.AnkleLeft) || (joint == KinectInterop.JointType.AnkleRight));
        }

        // Apply the rotations tracked by kinect to the joints.
        protected void TransformBone(ulong userId, KinectInterop.JointType joint, int boneIndex, bool flip)
        {
            Transform boneTransform = bones[boneIndex];
            if (boneTransform == null || kinectManager == null)
                return;

            int iJoint = (int)joint;
            if (iJoint < 0 || !kinectManager.IsJointTracked(userId, iJoint))
                return;

            // Get Kinect joint orientation
            Quaternion jointRotation = kinectManager.GetJointOrientation(userId, iJoint, flip);
            if (jointRotation == Quaternion.identity && !IsLegJoint(joint))
                return;

            // calculate the new orientation
            Quaternion newRotation = Kinect2AvatarRot(jointRotation, boneIndex);

            // Smoothly transition to the new rotation
            if (smoothFactor != 0f)
                boneTransform.rotation = Quaternion.Slerp(boneTransform.rotation, newRotation, smoothFactor * Time.deltaTime);
            else
                boneTransform.rotation = newRotation;
        }

        // Apply the rotations tracked by kinect to fingers (one joint = multiple bones)
        protected void TransformSpecialBoneFingers(ulong userId, int joint, int boneIndex, bool flip)
        {
            //// check for hand grips
            //if (joint == (int)KinectInterop.JointType.HandtipLeft || joint == (int)KinectInterop.JointType.ThumbLeft)
            //{
            //    if (lastLeftHandEvent == InteractionManager.HandEventType.Grip)
            //    {
            //        if (!bLeftFistDone && !kinectManager.IsUserTurnedAround(userId))
            //        {
            //            float angleSign = !mirroredMovement /**(boneIndex == 21 || boneIndex == 22)*/ ? -1f : -1f;
            //            float angleRot = angleSign * 60f;

            //            TransformSpecialBoneFist(boneIndex, angleRot);
            //            bLeftFistDone = (boneIndex >= 29);
            //        }

            //        return;
            //    }
            //    else if (bLeftFistDone && lastLeftHandEvent == InteractionManager.HandEventType.Release)
            //    {
            //        TransformSpecialBoneUnfist(boneIndex);
            //        bLeftFistDone = !(boneIndex >= 29);
            //    }
            //}
            //else if (joint == (int)KinectInterop.JointType.HandtipRight || joint == (int)KinectInterop.JointType.ThumbRight)
            //{
            //    if (lastRightHandEvent == InteractionManager.HandEventType.Grip)
            //    {
            //        if (!bRightFistDone && !kinectManager.IsUserTurnedAround(userId))
            //        {
            //            float angleSign = !mirroredMovement /**(boneIndex == 21 || boneIndex == 22)*/ ? -1f : -1f;
            //            float angleRot = angleSign * 60f;

            //            TransformSpecialBoneFist(boneIndex, angleRot);
            //            bRightFistDone = (boneIndex >= 29);
            //        }

            //        return;
            //    }
            //    else if (bRightFistDone && lastRightHandEvent == InteractionManager.HandEventType.Release)
            //    {
            //        TransformSpecialBoneUnfist(boneIndex);
            //        bRightFistDone = !(boneIndex >= 29);
            //    }
            //}

            bool isJointTracked = kinectManager.IsJointTracked(userId, joint);
            if (!animatorComponent || !isJointTracked)
                return;

            // Get Kinect joint orientation
            Quaternion jointRotation = kinectManager.GetJointOrientation(userId, joint, flip);
            if (jointRotation == Quaternion.identity)
                return;

            // calculate the new orientation
            Quaternion newRotation = Kinect2AvatarRot(jointRotation, boneIndex);

            // get the list of bones
            List<HumanBodyBones> alBones = boneIndex2MultiBoneMap[boneIndex];

            // Smoothly transition to the new rotation
            for (int i = 0; i < alBones.Count; i++)
            {
                Transform boneTransform = animatorComponent.GetBoneTransform(alBones[i]);
                if (!boneTransform)
                    continue;

                if (smoothFactor != 0f)
                    boneTransform.rotation = Quaternion.Slerp(boneTransform.rotation, newRotation, smoothFactor * Time.deltaTime);
                else
                    boneTransform.rotation = newRotation;
            }
        }

        // Apply the rotations needed to transform fingers to fist
        protected void TransformSpecialBoneFist(int boneIndex, float angle)
        {
            if (!animatorComponent)
                return;

            List<HumanBodyBones> alBones = boneIndex2MultiBoneMap[boneIndex];
            for (int i = 0; i < alBones.Count; i++)
            {
                if (i < 1 && (boneIndex == 22 || boneIndex == 24))  // skip the first thumb bone
                    continue;

                HumanBodyBones bone = alBones[i];
                Transform boneTransform = animatorComponent.GetBoneTransform(bone);

                // set the fist rotation
                if (boneTransform && fingerBoneLocalAxes[bone] != Vector3.zero)
                {
                    Quaternion qRotFinger = Quaternion.AngleAxis(angle, fingerBoneLocalAxes[bone]);
                    boneTransform.localRotation = fingerBoneLocalRotations[bone] * qRotFinger;
                }
            }

        }

        // Apply the initial rotations fingers
        protected void TransformSpecialBoneUnfist(int boneIndex)
        {
            if (!animatorComponent)
                return;

            List<HumanBodyBones> alBones = boneIndex2MultiBoneMap[boneIndex];
            for (int i = 0; i < alBones.Count; i++)
            {
                HumanBodyBones bone = alBones[i];
                Transform boneTransform = animatorComponent.GetBoneTransform(bone);

                // set the initial rotation
                if (boneTransform)
                {
                    boneTransform.localRotation = fingerBoneLocalRotations[bone];
                }
            }
        }

        // Moves the avatar - gets the tracked position of the user and applies it to avatar.
        protected void MoveAvatar(ulong UserID)
        {
            if ((moveRate == 0f) || !kinectManager ||
               !kinectManager.IsJointTracked(UserID, (int)KinectInterop.JointType.Pelvis))
            {
                return;
            }

            // get the position of user's spine base
            Vector3 trans = kinectManager.GetUserPosition(UserID);
            if (flipLeftRight)
                trans.x = -trans.x;

            // invert the z-coordinate, if needed
            if (stageOrigin && posRelInvertedZ)
            {
                trans.z = -trans.z;
            }

            if (!offsetCalibrated)
            {
                offsetCalibrated = true;

                offsetPos.x = trans.x;  // !mirroredMovement ? trans.x * moveRate : -trans.x * moveRate;
                offsetPos.y = trans.y;  // trans.y * moveRate;
                offsetPos.z = !mirroredMovement ? -trans.z : trans.z;  // -trans.z * moveRate;
            }

            // transition to the new position
            Vector3 targetPos = bodyRootPosition + Kinect2AvatarPos(trans, verticalMovement);

            if (isRigidBody && !verticalMovement)
            {
                // workaround for obeying the physics (e.g. gravity falling)
                targetPos.y = bodyRoot != null ? bodyRoot.position.y : transform.position.y;
            }

            // added by r618
            if (horizontalOffset != 0f &&
                bones[5] != null && bones[11] != null)
            {
                // { 5, HumanBodyBones.LeftUpperArm},
                // { 11, HumanBodyBones.RightUpperArm},
                //Vector3 dirSpine = bones[5].position - bones[11].position;
                Vector3 dirShoulders = bones[11].position - bones[5].position;
                targetPos += dirShoulders.normalized * horizontalOffset;
            }

            if (verticalMovement && verticalOffset != 0f &&
                bones[0] != null && bones[3] != null)
            {
                Vector3 dirSpine = bones[3].position - bones[0].position;
                targetPos += dirSpine.normalized * verticalOffset;
            }

            if (forwardOffset != 0f &&
                bones[0] != null && bones[3] != null && bones[5] != null && bones[11] != null)
            {
                Vector3 dirSpine = (bones[3].position - bones[0].position).normalized;
                Vector3 dirShoulders = (bones[11].position - bones[5].position).normalized;
                Vector3 dirForward = Vector3.Cross(dirShoulders, dirSpine).normalized;

                targetPos += dirForward * forwardOffset;
            }

            if (groundedFeet)
            {
                // keep the current correction
                float fLastTgtY = targetPos.y;
                targetPos.y += fFootDistance;

                float fNewDistance = GetDistanceToGround();
                float fNewDistanceTime = Time.time;

                //			Debug.Log(string.Format("PosY: {0:F2}, LastY: {1:F2},  TgrY: {2:F2}, NewDist: {3:F2}, Corr: {4:F2}, Time: {5:F2}", bodyRoot != null ? bodyRoot.position.y : transform.position.y,
                //				fLastTgtY, targetPos.y, fNewDistance, fFootDistance, fNewDistanceTime));

                if (Mathf.Abs(fNewDistance) >= 0.01f && Mathf.Abs(fNewDistance - fFootDistanceInitial) >= maxFootDistanceGround)
                {
                    if ((fNewDistanceTime - fFootDistanceTime) >= maxFootDistanceTime)
                    {
                        fFootDistance += (fNewDistance - fFootDistanceInitial);
                        fFootDistanceTime = fNewDistanceTime;

                        targetPos.y = fLastTgtY + fFootDistance;

                        //					Debug.Log(string.Format("   >> change({0:F2})! - Corr: {1:F2}, LastY: {2:F2},  TgrY: {3:F2} at time {4:F2}", 
                        //								(fNewDistance - fFootDistanceInitial), fFootDistance, fLastTgtY, targetPos.y, fFootDistanceTime));
                    }
                }
                else
                {
                    fFootDistanceTime = fNewDistanceTime;
                }
            }

            if (bodyRoot != null)
            {
                bodyRoot.position = smoothFactor != 0f ?
                    Vector3.Lerp(bodyRoot.position, targetPos, smoothFactor * Time.deltaTime) : targetPos;
            }
            else
            {
                transform.position = smoothFactor != 0f ?
                    Vector3.Lerp(transform.position, targetPos, smoothFactor * Time.deltaTime) : targetPos;
            }
        }

        // Set model's arms to be in T-pose
        protected void SetModelArmsInTpose()
        {
            Vector3 vTposeLeftDir = transform.TransformDirection(Vector3.left);
            Vector3 vTposeRightDir = transform.TransformDirection(Vector3.right);

            Transform transLeftUarm = GetBoneTransform(GetBoneIndexByJoint(KinectInterop.JointType.ShoulderLeft, false)); // animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform transLeftLarm = GetBoneTransform(GetBoneIndexByJoint(KinectInterop.JointType.ElbowLeft, false)); // animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            Transform transLeftHand = GetBoneTransform(GetBoneIndexByJoint(KinectInterop.JointType.WristLeft, false)); // animator.GetBoneTransform(HumanBodyBones.LeftHand);

            if (transLeftUarm != null && transLeftLarm != null)
            {
                Vector3 vUarmLeftDir = transLeftLarm.position - transLeftUarm.position;
                float fUarmLeftAngle = Vector3.Angle(vUarmLeftDir, vTposeLeftDir);

                if (Mathf.Abs(fUarmLeftAngle) >= 5f)
                {
                    Quaternion vFixRotation = Quaternion.FromToRotation(vUarmLeftDir, vTposeLeftDir);
                    transLeftUarm.rotation = vFixRotation * transLeftUarm.rotation;
                }

                if (transLeftHand != null)
                {
                    Vector3 vLarmLeftDir = transLeftHand.position - transLeftLarm.position;
                    float fLarmLeftAngle = Vector3.Angle(vLarmLeftDir, vTposeLeftDir);

                    if (Mathf.Abs(fLarmLeftAngle) >= 5f)
                    {
                        Quaternion vFixRotation = Quaternion.FromToRotation(vLarmLeftDir, vTposeLeftDir);
                        transLeftLarm.rotation = vFixRotation * transLeftLarm.rotation;
                    }
                }
            }

            Transform transRightUarm = GetBoneTransform(GetBoneIndexByJoint(KinectInterop.JointType.ShoulderRight, false)); // animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform transRightLarm = GetBoneTransform(GetBoneIndexByJoint(KinectInterop.JointType.ElbowRight, false)); // animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Transform transRightHand = GetBoneTransform(GetBoneIndexByJoint(KinectInterop.JointType.WristRight, false)); // animator.GetBoneTransform(HumanBodyBones.RightHand);

            if (transRightUarm != null && transRightLarm != null)
            {
                Vector3 vUarmRightDir = transRightLarm.position - transRightUarm.position;
                float fUarmRightAngle = Vector3.Angle(vUarmRightDir, vTposeRightDir);

                if (Mathf.Abs(fUarmRightAngle) >= 5f)
                {
                    Quaternion vFixRotation = Quaternion.FromToRotation(vUarmRightDir, vTposeRightDir);
                    transRightUarm.rotation = vFixRotation * transRightUarm.rotation;
                }

                if (transRightHand != null)
                {
                    Vector3 vLarmRightDir = transRightHand.position - transRightLarm.position;
                    float fLarmRightAngle = Vector3.Angle(vLarmRightDir, vTposeRightDir);

                    if (Mathf.Abs(fLarmRightAngle) >= 5f)
                    {
                        Quaternion vFixRotation = Quaternion.FromToRotation(vLarmRightDir, vTposeRightDir);
                        transRightLarm.rotation = vFixRotation * transRightLarm.rotation;
                    }
                }
            }

        }

        // If the bones to be mapped have been declared, map that bone to the model.
        protected virtual void MapBones()
        {
            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                if (!boneIndex2MecanimMap.ContainsKey(boneIndex))
                    continue;

                bones[boneIndex] = animatorComponent ? animatorComponent.GetBoneTransform(boneIndex2MecanimMap[boneIndex]) : null;
            }

            //// map finger bones, too
            //fingerBones = new Transform[fingerIndex2MecanimMap.Count];

            //for (int boneIndex = 0; boneIndex < fingerBones.Length; boneIndex++)
            //{
            //    if (!fingerIndex2MecanimMap.ContainsKey(boneIndex))
            //        continue;

            //    fingerBones[boneIndex] = animatorComponent ? animatorComponent.GetBoneTransform(fingerIndex2MecanimMap[boneIndex]) : null;
            //}
        }

        // Capture the initial rotations of the bones
        protected void GetInitialRotations()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            transform.rotation = Quaternion.identity;

            // save the body root initial position
            if (bodyRoot != null)
            {
                bodyRootPosition = bodyRoot.position;
            }
            else
            {
                bodyRootPosition = transform.position;
            }

            // save the initial bone rotations
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null)
                {
                    initialRotations[i] = bones[i].rotation;
                    localRotations[i] = bones[i].localRotation;
                }
            }

            // get finger bones' local rotations
            foreach (int boneIndex in boneIndex2MultiBoneMap.Keys)
            {
                List<HumanBodyBones> alBones = boneIndex2MultiBoneMap[boneIndex];

                for (int b = 0; b < alBones.Count; b++)
                {
                    HumanBodyBones bone = alBones[b];
                    Transform boneTransform = animatorComponent ? animatorComponent.GetBoneTransform(bone) : null;

                    // get the finger's 1st transform
                    Transform fingerBaseTransform = animatorComponent ? animatorComponent.GetBoneTransform(alBones[b - (b % 3)]) : null;

                    // get the finger's 2nd transform
                    Transform baseChildTransform = fingerBaseTransform && fingerBaseTransform.childCount > 0 ? fingerBaseTransform.GetChild(0) : null;
                    Vector3 vBoneDirChild = baseChildTransform && fingerBaseTransform ? (baseChildTransform.position - fingerBaseTransform.position).normalized : Vector3.zero;
                    Vector3 vOrthoDirChild = Vector3.Cross(vBoneDirChild, Vector3.up).normalized;

                    if (boneTransform)
                    {
                        fingerBoneLocalRotations[bone] = boneTransform.localRotation;

                        if (vBoneDirChild != Vector3.zero)
                        {
                            fingerBoneLocalAxes[bone] = boneTransform.InverseTransformDirection(vOrthoDirChild).normalized;
                        }
                        else
                        {
                            fingerBoneLocalAxes[bone] = Vector3.zero;
                        }
                    }
                }
            }

            // Restore the initial rotation
            transform.rotation = initialRotation;
        }

        // Converts kinect joint rotation to avatar joint rotation, depending on joint initial rotation and offset rotation
        protected Quaternion Kinect2AvatarRot(Quaternion jointRotation, int boneIndex)
        {
            Quaternion newRotation = jointRotation * initialRotations[boneIndex];
            newRotation = initialRotation * newRotation;

            return newRotation;
        }

        // Converts Kinect position to avatar skeleton position, depending on initial position, mirroring and move rate
        protected Vector3 Kinect2AvatarPos(Vector3 jointPosition, bool bMoveVertically)
        {
            float xPos = (jointPosition.x - offsetPos.x) * moveRate;
            float yPos = (jointPosition.y - offsetPos.y) * moveRate;
            float zPos = !mirroredMovement ? (-jointPosition.z - offsetPos.z) * moveRate : (jointPosition.z - offsetPos.z) * moveRate;

            Vector3 newPosition = new Vector3(xPos, bMoveVertically ? yPos : 0f, zPos);

            Quaternion posRotation = mirroredMovement ? Quaternion.Euler(0f, 180f, 0f) * initialRotation : initialRotation;
            newPosition = posRotation * newPosition;

            return newPosition;
        }

        // returns distance from the given transform to the underlying object. The player must be in IgnoreRaycast layer.
        protected virtual float GetTransformDistanceToGround(Transform trans)
        {
            if (!trans)
                return 0f;

            return -trans.position.y;
        }

        // returns the lower distance distance from left or right foot to the ground, or 1000f if no LF/RF transforms are found
        protected virtual float GetDistanceToGround()
        {
            if (leftFoot == null && rightFoot == null)
            {
                leftFoot = GetBoneTransform(GetBoneIndexByJoint(KinectInterop.JointType.FootLeft, false));
                rightFoot = GetBoneTransform(GetBoneIndexByJoint(KinectInterop.JointType.FootRight, false));

                if (leftFoot == null || rightFoot == null)
                {
                    leftFoot = GetBoneTransform(GetBoneIndexByJoint(KinectInterop.JointType.AnkleLeft, false));
                    rightFoot = GetBoneTransform(GetBoneIndexByJoint(KinectInterop.JointType.AnkleRight, false));
                }
            }

            float fDistMin = 1000f;
            float fDistLeft = leftFoot ? GetTransformDistanceToGround(leftFoot) : fDistMin;
            float fDistRight = rightFoot ? GetTransformDistanceToGround(rightFoot) : fDistMin;
            fDistMin = Mathf.Abs(fDistLeft) < Mathf.Abs(fDistRight) ? fDistLeft : fDistRight;

            if (fDistMin == 1000f)
            {
                fDistMin = 0f; // fFootDistanceInitial;
            }

//		    Debug.Log (string.Format ("LFootY: {0:F2}, Dist: {1:F2}, RFootY: {2:F2}, Dist: {3:F2}, Min: {4:F2}", leftFoot ? leftFoot.position.y : 0f, fDistLeft,
//						rightFoot ? rightFoot.position.y : 0f, fDistRight, fDistMin));

            return fDistMin;
        }



        // dictionaries to speed up bone processing
        protected static readonly Dictionary<int, HumanBodyBones> boneIndex2MecanimMap = new Dictionary<int, HumanBodyBones>
        {
            {0, HumanBodyBones.Hips},
            {1, HumanBodyBones.Spine},
            {2, HumanBodyBones.Chest},
		    {3, HumanBodyBones.Neck},
    		{4, HumanBodyBones.Head},

            {5, HumanBodyBones.LeftShoulder},
            {6, HumanBodyBones.LeftUpperArm},
            {7, HumanBodyBones.LeftLowerArm},
            {8, HumanBodyBones.LeftHand},

            {9, HumanBodyBones.RightShoulder},
            {10, HumanBodyBones.RightUpperArm},
            {11, HumanBodyBones.RightLowerArm},
            {12, HumanBodyBones.RightHand},
		
		    {13, HumanBodyBones.LeftUpperLeg},
            {14, HumanBodyBones.LeftLowerLeg},
            {15, HumanBodyBones.LeftFoot},
//    		{16, HumanBodyBones.LeftToes},
		
		    {17, HumanBodyBones.RightUpperLeg},
            {18, HumanBodyBones.RightLowerLeg},
            {19, HumanBodyBones.RightFoot},
//    		{20, HumanBodyBones.RightToes},

		    {21, HumanBodyBones.LeftIndexProximal},
            {22, HumanBodyBones.LeftThumbProximal},
            {23, HumanBodyBones.RightIndexProximal},
            {24, HumanBodyBones.RightThumbProximal},
        };

        protected static readonly Dictionary<int, KinectInterop.JointType> boneIndex2JointMap = new Dictionary<int, KinectInterop.JointType>
        {
            {0, KinectInterop.JointType.Pelvis},
            {1, KinectInterop.JointType.SpineNaval},
            {2, KinectInterop.JointType.SpineChest},
            {3, KinectInterop.JointType.Neck},
            {4, KinectInterop.JointType.Head},

            {5, KinectInterop.JointType.ClavicleLeft},
            {6, KinectInterop.JointType.ShoulderLeft},
            {7, KinectInterop.JointType.ElbowLeft},
            {8, KinectInterop.JointType.WristLeft},

            {9, KinectInterop.JointType.ClavicleRight},
            {10, KinectInterop.JointType.ShoulderRight},
            {11, KinectInterop.JointType.ElbowRight},
            {12, KinectInterop.JointType.WristRight},

            {13, KinectInterop.JointType.HipLeft},
            {14, KinectInterop.JointType.KneeLeft},
            {15, KinectInterop.JointType.AnkleLeft},
            {16, KinectInterop.JointType.FootLeft},

            {17, KinectInterop.JointType.HipRight},
            {18, KinectInterop.JointType.KneeRight},
            {19, KinectInterop.JointType.AnkleRight},
            {20, KinectInterop.JointType.FootRight},
        };

        protected static readonly Dictionary<int, KinectInterop.JointType> boneIndex2MirrorJointMap = new Dictionary<int, KinectInterop.JointType>
        {
            {0, KinectInterop.JointType.Pelvis},
            {1, KinectInterop.JointType.SpineNaval},
            {2, KinectInterop.JointType.SpineChest},
            {3, KinectInterop.JointType.Neck},
            {4, KinectInterop.JointType.Head},

            {5, KinectInterop.JointType.ClavicleRight},
            {6, KinectInterop.JointType.ShoulderRight},
            {7, KinectInterop.JointType.ElbowRight},
            {8, KinectInterop.JointType.WristRight},

            {9, KinectInterop.JointType.ClavicleLeft},
            {10, KinectInterop.JointType.ShoulderLeft},
            {11, KinectInterop.JointType.ElbowLeft},
            {12, KinectInterop.JointType.WristLeft},

            {13, KinectInterop.JointType.HipRight},
            {14, KinectInterop.JointType.KneeRight},
            {15, KinectInterop.JointType.AnkleRight},
            {16, KinectInterop.JointType.FootRight},

            {17, KinectInterop.JointType.HipLeft},
            {18, KinectInterop.JointType.KneeLeft},
            {19, KinectInterop.JointType.AnkleLeft},
            {20, KinectInterop.JointType.FootLeft},
        };

        protected static readonly Dictionary<KinectInterop.JointType, int> jointMap2boneIndex = new Dictionary<KinectInterop.JointType, int>
        {
            {KinectInterop.JointType.Pelvis, 0},
            {KinectInterop.JointType.SpineNaval, 1},
            {KinectInterop.JointType.SpineChest, 2},
            {KinectInterop.JointType.Neck, 3},
            {KinectInterop.JointType.Head, 4},

            {KinectInterop.JointType.ClavicleLeft, 5},
            {KinectInterop.JointType.ShoulderLeft, 6},
            {KinectInterop.JointType.ElbowLeft, 7},
            {KinectInterop.JointType.WristLeft, 8},

            {KinectInterop.JointType.ClavicleRight, 9},
            {KinectInterop.JointType.ShoulderRight, 10},
            {KinectInterop.JointType.ElbowRight, 11},
            {KinectInterop.JointType.WristRight, 12},

            {KinectInterop.JointType.HipLeft, 13},
            {KinectInterop.JointType.KneeLeft, 14},
            {KinectInterop.JointType.AnkleLeft, 15},
            {KinectInterop.JointType.FootLeft, 16},

            {KinectInterop.JointType.HipRight, 17},
            {KinectInterop.JointType.KneeRight, 18},
            {KinectInterop.JointType.AnkleRight, 19},
            {KinectInterop.JointType.FootRight, 20},
        };

        protected static readonly Dictionary<KinectInterop.JointType, int> mirrorJointMap2boneIndex = new Dictionary<KinectInterop.JointType, int>
        {
            {KinectInterop.JointType.Pelvis, 0},
            {KinectInterop.JointType.SpineNaval, 1},
            {KinectInterop.JointType.SpineChest, 2},
            {KinectInterop.JointType.Neck, 3},
            {KinectInterop.JointType.Head, 4},

            {KinectInterop.JointType.ClavicleRight, 5},
            {KinectInterop.JointType.ShoulderRight, 6},
            {KinectInterop.JointType.ElbowRight, 7},
            {KinectInterop.JointType.WristRight, 8},

            {KinectInterop.JointType.ClavicleLeft, 9},
            {KinectInterop.JointType.ShoulderLeft, 10},
            {KinectInterop.JointType.ElbowLeft, 11},
            {KinectInterop.JointType.WristLeft, 12},

            {KinectInterop.JointType.HipRight, 13},
            {KinectInterop.JointType.KneeRight, 14},
            {KinectInterop.JointType.AnkleRight, 15},
            {KinectInterop.JointType.FootRight, 16},

            {KinectInterop.JointType.HipLeft, 17},
            {KinectInterop.JointType.KneeLeft, 18},
            {KinectInterop.JointType.AnkleLeft, 19},
            {KinectInterop.JointType.FootLeft, 20},
        };

        protected static readonly Dictionary<int, KinectInterop.JointType> boneIndex2FingerMap = new Dictionary<int, KinectInterop.JointType>
        {
            {21, KinectInterop.JointType.HandtipLeft},
            {22, KinectInterop.JointType.ThumbLeft},
            {23, KinectInterop.JointType.HandtipRight},
            {24, KinectInterop.JointType.ThumbRight},
        };

        protected static readonly Dictionary<int, KinectInterop.JointType> boneIndex2MirrorFingerMap = new Dictionary<int, KinectInterop.JointType>
        {
            {21, KinectInterop.JointType.HandtipRight},
            {22, KinectInterop.JointType.ThumbRight},
            {23, KinectInterop.JointType.HandtipLeft},
            {24, KinectInterop.JointType.ThumbLeft},
        };

        protected static readonly Dictionary<int, List<HumanBodyBones>> boneIndex2MultiBoneMap = new Dictionary<int, List<HumanBodyBones>>
        {
            {21, new List<HumanBodyBones> {  // left fingers
				    HumanBodyBones.LeftIndexProximal,
                    HumanBodyBones.LeftIndexIntermediate,
                    HumanBodyBones.LeftIndexDistal,
                    HumanBodyBones.LeftMiddleProximal,
                    HumanBodyBones.LeftMiddleIntermediate,
                    HumanBodyBones.LeftMiddleDistal,
                    HumanBodyBones.LeftRingProximal,
                    HumanBodyBones.LeftRingIntermediate,
                    HumanBodyBones.LeftRingDistal,
                    HumanBodyBones.LeftLittleProximal,
                    HumanBodyBones.LeftLittleIntermediate,
                    HumanBodyBones.LeftLittleDistal,
                }},
            {22, new List<HumanBodyBones> {  // left thumb
				    HumanBodyBones.LeftThumbProximal,
                    HumanBodyBones.LeftThumbIntermediate,
                    HumanBodyBones.LeftThumbDistal,
                }},
            {23, new List<HumanBodyBones> {  // right fingers
				    HumanBodyBones.RightIndexProximal,
                    HumanBodyBones.RightIndexIntermediate,
                    HumanBodyBones.RightIndexDistal,
                    HumanBodyBones.RightMiddleProximal,
                    HumanBodyBones.RightMiddleIntermediate,
                    HumanBodyBones.RightMiddleDistal,
                    HumanBodyBones.RightRingProximal,
                    HumanBodyBones.RightRingIntermediate,
                    HumanBodyBones.RightRingDistal,
                    HumanBodyBones.RightLittleProximal,
                    HumanBodyBones.RightLittleIntermediate,
                    HumanBodyBones.RightLittleDistal,
                }},
            {24, new List<HumanBodyBones> {  // right thumb
				    HumanBodyBones.RightThumbProximal,
                    HumanBodyBones.RightThumbIntermediate,
                    HumanBodyBones.RightThumbDistal,
                }},
        };

    }
}
