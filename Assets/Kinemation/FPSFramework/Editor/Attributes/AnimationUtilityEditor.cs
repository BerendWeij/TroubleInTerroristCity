// Designed by Kinemation, 2023

using UnityEditor;
using UnityEngine;

namespace Kinemation.FPSFramework.Editor.Attributes
{
    using UnityEngine;
    using UnityEditor;

    public class FPSAnimatorUtilityEditorWindow : EditorWindow
    {
        private const string ExtractBoneName = "ik_hand_gun";
        private const string AdditiveBonePath = "rootBone/WeaponBoneAdditive";

        private AnimationClip animationClip;
        private AnimationClip refClip;
        private Vector3 rotationOffset;
        
        private AvatarMask maskToModify;
        private Transform boneToAdd;

        private int tab = 0;

        [MenuItem("Window/FPS Animator Utility")]
        public static void ShowWindow()
        {
            GetWindow<FPSAnimatorUtilityEditorWindow>("FPS Animator Utility");
        }

        private void OnGUI()
        {
            tab = GUILayout.Toolbar(tab, new string[] {"IK Additive", "Avatar Mask Fixer"});

            switch (tab)
            {
                case 0:
                    IKAdditiveTab();
                    break;
                case 1:
                    AvatarMaskFixerTab();
                    break;
            }
        }

        private void IKAdditiveTab()
        {
            EditorGUILayout.HelpBox("This tool extracts curves from `"
                                    + ExtractBoneName
                                    + "`, and creates curve additive animation."
                                    + "\nUseful for procedural animations, like idle, walk or sprint.",
                MessageType.Info);
        
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            animationClip =
                EditorGUILayout.ObjectField("Animation Clip", animationClip, typeof(AnimationClip), true)
                    as AnimationClip;
            refClip =
                EditorGUILayout.ObjectField("Ref Clip", refClip, typeof(AnimationClip), true) 
                    as AnimationClip;

            rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rotationOffset);
            
            if (GUILayout.Button("Extract"))
            {
                if (animationClip != null)
                {
                    ExtractAndSetAnimationData();
                }
            }
        }

        private void AvatarMaskFixerTab()
        {
            EditorGUILayout.HelpBox("This tool adds a Transform to the Avatar Mask. " 
                                    + "Useful if you need to include a non-skeletal object in your mask.", 
                MessageType.Info);
            
            boneToAdd =
                EditorGUILayout.ObjectField("Bone To Add", boneToAdd, typeof(Transform), true)
                    as Transform;

            maskToModify =
                EditorGUILayout.ObjectField("Upper Body Mask", maskToModify, typeof(AvatarMask), true) 
                    as AvatarMask;

            if (boneToAdd == null)
            {
                EditorGUILayout.HelpBox("Select the Bone transform", MessageType.Warning);
                return;
            }
            
            if (maskToModify == null)
            {
                EditorGUILayout.HelpBox("Select the Avatar Mask", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Add Bone"))
            {
                for (int i = maskToModify.transformCount - 1; i >= 0; i--)
                {
                    if (maskToModify.GetTransformPath(i).EndsWith(boneToAdd.name))
                    {
                        return;
                    }
                }

                maskToModify.AddTransformPath(boneToAdd, false);
                string path = maskToModify.GetTransformPath(maskToModify.transformCount - 1);
                int slashIndex = path.IndexOf("/");
                if (slashIndex >= 0)
                {
                    path = path.Substring(slashIndex + 1);
                }

                maskToModify.SetTransformPath(maskToModify.transformCount - 1, path);
            }
        }

        private Vector3 GetVectorValue(AnimationClip clip, EditorCurveBinding[] bindings, float time)
        {
            float tX = AnimationUtility.GetEditorCurve(clip, bindings[0]).Evaluate(time);
            float tY = AnimationUtility.GetEditorCurve(clip, bindings[1]).Evaluate(time);
            float tZ = AnimationUtility.GetEditorCurve(clip, bindings[2]).Evaluate(time);

            return new Vector3(tX, tY, tZ);
        }

        private Quaternion GetQuatValue(AnimationClip clip, EditorCurveBinding[] bindings, float time)
        {
            float tX = AnimationUtility.GetEditorCurve(clip, bindings[0]).Evaluate(time);
            float tY = AnimationUtility.GetEditorCurve(clip, bindings[1]).Evaluate(time);
            float tZ = AnimationUtility.GetEditorCurve(clip, bindings[2]).Evaluate(time);
            float tW = AnimationUtility.GetEditorCurve(clip, bindings[3]).Evaluate(time);

            return new Quaternion(tX, tY, tZ, tW);
        }

        private void ExtractAndSetAnimationData()
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(animationClip);

            EditorCurveBinding[] tBindings = new EditorCurveBinding[3];
            EditorCurveBinding[] rBindings = new EditorCurveBinding[4];

            foreach (var binding in bindings)
            {
                if (!binding.path.ToLower().EndsWith(ExtractBoneName))
                {
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localposition.x"))
                {
                    tBindings[0] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localposition.y"))
                {
                    tBindings[1] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localposition.z"))
                {
                    tBindings[2] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localrotation.x"))
                {
                    rBindings[0] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localrotation.y"))
                {
                    rBindings[1] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localrotation.z"))
                {
                    rBindings[2] = binding;
                    continue;
                }

                if (binding.propertyName.ToLower().Contains("m_localrotation.w"))
                {
                    rBindings[3] = binding;
                }
            }

            Vector3 refTranslation = GetVectorValue(refClip, tBindings, 0f);
            Quaternion refRotation = GetQuatValue(refClip, rBindings, 0f) * Quaternion.Euler(rotationOffset);

            AnimationCurve tX = new AnimationCurve();
            AnimationCurve tY = new AnimationCurve();
            AnimationCurve tZ = new AnimationCurve();

            AnimationCurve rX = new AnimationCurve();
            AnimationCurve rY = new AnimationCurve();
            AnimationCurve rZ = new AnimationCurve();
            AnimationCurve rW = new AnimationCurve();

            float playLength = animationClip.length;
            float frameRate = 1f / animationClip.frameRate;
            float playBack = 0f;

            while (playBack < playLength)
            {
                Vector3 translation = GetVectorValue(animationClip, tBindings, playBack);
                Quaternion rotation = GetQuatValue(animationClip, rBindings, playBack) * Quaternion.Euler(rotationOffset);

                Vector3 deltaT = translation - refTranslation;
                Quaternion deltaR = Quaternion.Inverse(refRotation) * rotation;

                tX.AddKey(playBack, deltaT.x);
                tY.AddKey(playBack, deltaT.y);
                tZ.AddKey(playBack, deltaT.z);

                rX.AddKey(playBack, deltaR.x);
                rY.AddKey(playBack, deltaR.y);
                rZ.AddKey(playBack, deltaR.z);
                rW.AddKey(playBack, deltaR.w);

                playBack += frameRate;
            }

            animationClip.SetCurve(AdditiveBonePath, typeof(Transform), tBindings[0].propertyName, tX);
            animationClip.SetCurve(AdditiveBonePath, typeof(Transform), tBindings[1].propertyName, tY);
            animationClip.SetCurve(AdditiveBonePath, typeof(Transform), tBindings[2].propertyName, tZ);

            animationClip.SetCurve(AdditiveBonePath, typeof(Transform), rBindings[0].propertyName, rX);
            animationClip.SetCurve(AdditiveBonePath, typeof(Transform), rBindings[1].propertyName, rY);
            animationClip.SetCurve(AdditiveBonePath, typeof(Transform), rBindings[2].propertyName, rZ);
            animationClip.SetCurve(AdditiveBonePath, typeof(Transform), rBindings[3].propertyName, rW);
        }
    }


    public class TransformRetarget : EditorWindow
    {
        AnimationClip sourceClip;
        AnimationClip targetClip;

        Transform sourceBone;
        Transform sourceRoot;

        Transform targetBone;
        Transform targetRoot;

        [MenuItem("Window/Transform Retarget")]
        public static void ShowWindow()
        {
            GetWindow<TransformRetarget>("Transform Retarget");
        }

        private void OnGUI()
        {
            sourceClip = (AnimationClip) EditorGUILayout.ObjectField("Source Animation Clip", sourceClip,
                typeof(AnimationClip), false);
            targetClip = (AnimationClip) EditorGUILayout.ObjectField("Target Animation Clip", targetClip,
                typeof(AnimationClip), false);

            sourceRoot = (Transform) EditorGUILayout.ObjectField("Source Root Transform", sourceRoot, typeof(Transform),
                true);
            sourceBone = (Transform) EditorGUILayout.ObjectField("Source Transform", sourceBone, typeof(Transform),
                true);

            targetRoot = (Transform) EditorGUILayout.ObjectField("Target Root Transform", targetRoot, typeof(Transform),
                true);
            targetBone = (Transform) EditorGUILayout.ObjectField("Target Transform", targetBone, typeof(Transform),
                true);

            if (GUILayout.Button("Retarget Animation"))
            {
                if (sourceClip == null || targetClip == null)
                {
                    Debug.LogError("Transform Retarget: failed. Source or Target clip is null");
                    return;
                }

                if (sourceRoot == null || sourceBone == null)
                {
                    Debug.LogError("Transform Retarget: failed. Source is null");
                    return;
                }

                if (targetRoot == null || targetBone == null)
                {
                    Debug.LogError("Transform Retarget: failed. Target is null");
                    return;
                }

                RetargetAnimation();
            }
        }

        private void RetargetAnimation()
        {
            // Get all curve bindings from the source clip
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(sourceClip);
            foreach (var binding in curveBindings)
            {
                // If this curve belongs to the source transform
                if (binding.path.Equals(AnimationUtility.CalculateTransformPath(sourceBone, sourceRoot)))
                {
                    // Create a new binding that points to the target transform instead
                    EditorCurveBinding newBinding = new EditorCurveBinding()
                    {
                        path = AnimationUtility.CalculateTransformPath(targetBone, targetRoot),
                        type = binding.type,
                        propertyName = binding.propertyName
                    };

                    // Copy the curve from the source clip to the target clip
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                    AnimationUtility.SetEditorCurve(targetClip, newBinding, curve);
                }
            }
        }
    }
}