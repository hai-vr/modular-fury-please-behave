// This LICENSE text was appended by Haï~ to the source code of
// - https://github.com/hai-vr/modular-fury-please-behave
// on the basis that this file may be considered reuse of VRCFury
// - https://github.com/VRCFury/VRCFury/blob/main/com.vrcfury.vrcfury/LICENSE.md
// even though most of the essence of this file is code patching.
/**
Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)
https://creativecommons.org/licenses/by-nc/4.0/
https://creativecommons.org/licenses/by-nc/4.0/legalcode

Copyright (c) VRCFury Developers. All unlicensed rights reserved.
 */

using System.Reflection;
using Hai.MFPB;
using HarmonyLib;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VF;
using VF.Builder;
using VF.VrcHooks;


[assembly: ExportsPlugin(typeof(MFPBPlugin))]
namespace Hai.MFPB
{
    public enum MFPBFuryShouldRun
    {
        BeforeModularAvatar,
        AfterModularAvatar
    }
    
    [InitializeOnLoad]
    public static class MFPB
    {
        public static bool EmergencyStop;
        public static MFPBFuryShouldRun VRCFuryShouldRun = MFPBFuryShouldRun.BeforeModularAvatar;
        
        static MFPB()
        {
            var harmony = new Harmony("dev.hai-vr.mfpb");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }
    }
    
    public class MFPBFury
    {
        public static void RunVRCFury(BuildContext ctx)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.Log("(MFPB) Running Play Mode execution path. This will execute PlayModeTrigger.Rescan");
                // Run VRCFury by making the Rescan method think that the scene only contains one avatar
                // by lying when VFGameObject.GetRoots(Scene) is invoked.
                var rescanMethod = typeof(PlayModeTrigger).GetMethod("Rescan", BindingFlags.Static | BindingFlags.NonPublic);
                if (rescanMethod == null)
                {
                    Debug.LogError("(MFPB) Can't execute Play Mode, Rescan method not found!");
                    MFPB.EmergencyStop = true;
                    return;
                }

                try
                {
                    MFPBPlayModeTriggerPatch.isPassThroughMode = true;
                    MFPBVFGameObjectPatch.avatarRoot = ctx.AvatarRootObject;
                    MFPBVFGameObjectPatch.isInterceptionMode = true;
                    rescanMethod.Invoke(null, new[] { (GameObject)null });
                }
                finally
                {
                    MFPBVFGameObjectPatch.isInterceptionMode = false;
                    MFPBPlayModeTriggerPatch.isPassThroughMode = false;
                }
            }
            else
            {
                Debug.Log("(MFPB) Running Edit Mode execution path. This will execute PreuploadHook.OnPreprocessAvatar");
                new PreuploadHook().OnPreprocessAvatar(ctx.AvatarRootObject);
            }
        }
    }

    [HarmonyPatch(typeof(PreuploadHook), nameof(PreuploadHook.OnPreprocessAvatar))]
    public class MFPBPreuploadHookPatch
    {
        static bool Prefix(GameObject _vrcCloneObject, ref bool __result)
        {
            if (MFPB.EmergencyStop) return true;

            Debug.Log("(MFPB) MFPBPreuploadHookPatch.OnPreprocessAvatar intercepted, will skip and return true.");
            __result = true;
            
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayModeTrigger), "Rescan")]
    public class MFPBPlayModeTriggerPatch
    {
        public static bool isPassThroughMode;
        
        static bool Prefix(Scene scene)
        {
            if (MFPB.EmergencyStop) return true;

            var passThrough = isPassThroughMode;
            if (isPassThroughMode)
            {
                Debug.Log("(MFPB) MFPBPlayModeTriggerPatch.Rescan intercepted, will pass through.");
            }
            else
            {
                Debug.Log("(MFPB) MFPBPlayModeTriggerPatch.Rescan intercepted, will skip.");
            }
            return passThrough;
        }
    }
    
    [HarmonyPatch(typeof(VFGameObject), nameof(VFGameObject.GetRoots), typeof(Scene))]
    public class MFPBVFGameObjectPatch
    {
        public static bool isInterceptionMode;

        public static GameObject avatarRoot;
        
        static bool Prefix(Scene scene, ref VFGameObject[] __result)
        {
            if (MFPB.EmergencyStop) return true;

            if (isInterceptionMode)
            {
                Debug.Log($"(MFPB) MFPBVFGameObjectPatch.GetRoots intercepted, will skip and return avatarRoot={avatarRoot}.");
    
                __result = new[]
                {
                    new VFGameObject(avatarRoot)
                };
                return false;
            }

            return true;
        }
    }

    public class MFPBPlugin : Plugin<MFPBPlugin>
    {
        protected override void Configure()
        {
            if (MFPB.EmergencyStop) return;

            InPhase(BuildPhase.Generating)
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("MFPB-BeforeMA", ctx =>
                {
                    if (MFPB.VRCFuryShouldRun == MFPBFuryShouldRun.BeforeModularAvatar)
                    {
                        DoExecuteVRCFury(ctx);
                    }
                });
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run("MFPB-AfterMA", ctx =>
                {
                    if (MFPB.VRCFuryShouldRun == MFPBFuryShouldRun.AfterModularAvatar)
                    {
                        DoExecuteVRCFury(ctx);
                    }
                });
        }

        private static void DoExecuteVRCFury(BuildContext ctx)
        {
            Debug.Log($"(MFPB) Running MFPB. MFPB.VRCFuryShouldRun={MFPB.VRCFuryShouldRun}");
            MFPBFury.RunVRCFury(ctx);
        }
    }
}