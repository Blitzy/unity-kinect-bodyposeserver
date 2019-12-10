using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using SessionSocketClient;

[RequireComponent(typeof(Animator))]
[System.Serializable]
public class UnityKinectStageAvatarSessionData : UnitySessionData
{
    [SerializeField]
    [HideInInspector]
    private List<BoneData> _boneDataList;

    [SerializeField]
    [HideInInspector]
    private Vector3 _offsetFromStageOrigin;

    private Animator _animator;
    private Dictionary<HumanBodyBones, Transform> _bones;
    private bool _watchForChanges;
    private Transform _stageOrigin;

    protected override void Init() {
        _animator = GetComponent<Animator>();
        if (!_animator.isHuman) {
            Debug.LogError("[UnityKinectStageAvatarSessionData] Animator must be applied to a humanoid.", gameObject);
        }

        var so = GameObject.FindWithTag("Stage Origin");
        if (so != null) {
            _stageOrigin = so.transform;
        } else {
            Debug.LogError("[UnityKinectStageAvatarSessionData] Could not find transform tagged 'Stage Origin'.");
        }
        
        // Retrieve and cache all the human body bones and initialize bone data.
        var enumValues = Enum.GetValues(typeof(HumanBodyBones));
        _bones = new Dictionary<HumanBodyBones, Transform>(enumValues.Length);
        _boneDataList = new List<BoneData>(enumValues.Length);
        foreach(HumanBodyBones hbb in enumValues) {
            if (hbb == HumanBodyBones.LastBone) {
                continue;
            }

            var bone = _animator.GetBoneTransform(hbb);
            if (bone != null) {
                _bones.Add(hbb, bone);
                _boneDataList.Add(new BoneData(){
                    humanBodyBone = (int)hbb,
                    localRotation = bone.localRotation
                });
            }
        }
    }

    protected override void HookupEventListeners() {
        _watchForChanges = true;
    }

    protected override void UnhookEventListeners() {
        _watchForChanges = false;
    }

    protected override void UpdateDataFromLocal() {
        // Update bone data with current values from bone transforms.
        for (int i = 0; i < _boneDataList.Count; i++) {
            HumanBodyBones hbb = (HumanBodyBones)_boneDataList[i].humanBodyBone;
            Transform bone = _bones[hbb];

            _boneDataList[i].localRotation = bone.localRotation;
        }

        _offsetFromStageOrigin = _CalcOffsetFromStageOrigin();
        Debug.Log("[UnityKinectStageAvatarSessionData] UpdateDataFromLocal offsetFromStageOrigin: " + _offsetFromStageOrigin);
    }

    protected override void UpdateLocalFromData() {
        // Update bone transforms with current values from bone data.
        for (int i = 0; i < _boneDataList.Count; i++) {
            HumanBodyBones hbb = (HumanBodyBones)_boneDataList[i].humanBodyBone;
            Transform bone = _bones[hbb];

            bone.localRotation = _boneDataList[i].localRotation;
        }

        _InheritOffsetFromStageOrigin();
        Debug.Log("[UnityKinectStageAvatarSessionData] UpdateLocalFromData offsetFromStageOrigin: " + _offsetFromStageOrigin);
    }

    private void LateUpdate() {
        if (_watchForChanges && _HasBoneTransformChanged()) {
            // Bone transform has changed, update the bone data and save it.
            UpdateDataFromLocal();
            SaveData();
        }
    }

    private bool _HasBoneTransformChanged() {
        for (int i = 0; i < _boneDataList.Count; i++) {
            HumanBodyBones hbb = (HumanBodyBones)_boneDataList[i].humanBodyBone;
            Transform bone = _bones[hbb];

            if (!bone.localRotation.Equals(_boneDataList[i].localRotation)) {
                // Debug.LogFormat("[UnityHumanoidSessionData] Bone changed {0}. Bone rotation: {1}, BoneData rotation: {2}",
                //     hbb.ToString(), bone.localRotation.ToString(), _boneDataList[i].localRotation.ToString()
                // );
                // Bone local rotation is different from bone data local rotation.
                return true;
            }
        }

        var curOffset = _CalcOffsetFromStageOrigin();
        if (!curOffset.Equals(_offsetFromStageOrigin)) {
            return true;
        }

        return false;
    }

    private Vector3 _CalcOffsetFromStageOrigin() {
        if (_stageOrigin == null) {
            return Vector3.zero;
        }
        
        var originLocalPos = transform.InverseTransformPoint(_stageOrigin.position);
        return originLocalPos - transform.localPosition;
    }

    private void _InheritOffsetFromStageOrigin() {
        if (_stageOrigin == null) {
            return;
        }

        var originLocalPos = transform.InverseTransformPoint(_stageOrigin.position);
        transform.localPosition = originLocalPos + _offsetFromStageOrigin;
    }

    [Serializable]
    public class BoneData {
        /// <summary>
        /// This is the raw human body bone enum value for this bone.
        /// </summary>
        public int humanBodyBone;

        /// <summary>
        /// This is the local rotation for the bone.
        /// </summary>
        public Quaternion localRotation; 
    }
}