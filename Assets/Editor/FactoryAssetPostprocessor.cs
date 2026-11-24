using System.IO;
using UnityEditor;
using UnityEngine;

namespace LethalFactory.EditorTools
{
    /// <summary>
    /// Automatically configures Texture maps (Normal/MaskMap) and Blender FBX imports
    /// (Rig, Looping Animation Clips, Lightmap UVs) for the Abandoned Factory assets.
    /// </summary>
    public class FactoryAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/Textures/"))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 1024;
            importer.wrapMode = TextureWrapMode.Repeat;

            if (assetPath.EndsWith("_Normal.png"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
            }
            else if (assetPath.EndsWith("_MaskMap.png"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
            }
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Art/Models/"))
                return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1.0f;
            importer.useFileScale = true;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

            if (assetPath.StartsWith("Assets/Art/Models/Characters/"))
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.importBlendShapes = false;
                importer.resampleCurves = true;
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.generateSecondaryUV = true;
            }
        }

        private void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith("Assets/Art/Models/Characters/"))
                return;

            var importer = (ModelImporter)assetImporter;
            var defaultClips = importer.defaultClipAnimations;
            if (defaultClips == null || defaultClips.Length == 0)
                return;

            var configured = new ModelImporterClipAnimation[defaultClips.Length];
            for (int i = 0; i < defaultClips.Length; i++)
            {
                var clip = defaultClips[i];
                // Strip Blender's "ArmatureName|" prefix for clean clip names in Unity
                string cleanName = clip.name;
                int pipeIdx = cleanName.LastIndexOf('|');
                if (pipeIdx >= 0 && pipeIdx < cleanName.Length - 1)
                {
                    cleanName = cleanName.Substring(pipeIdx + 1);
                }

                clip.name = cleanName;
                bool shouldLoop = !cleanName.ToLowerInvariant().Contains("howl");
                clip.loopTime = shouldLoop;
                clip.loopPose = shouldLoop;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                configured[i] = clip;
            }

            importer.clipAnimations = configured;
        }
    }
}
