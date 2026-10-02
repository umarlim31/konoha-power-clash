using UnityEditor;

namespace Konoha.Editor
{
    // Import rules for owner-supplied hero models under Assets/Konoha/Art/Heroes/<Hero>/.
    // <Hero>.fbx is the skinned model with a humanoid rig; <Hero>@<Clip>.fbx files carry
    // one animation each. Files elsewhere in the project are not affected.
    internal sealed class HeroModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!HeroVisualCatalog.IsHeroModelAsset(assetPath) || !(assetImporter is ModelImporter importer))
                return;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.animationType = ModelImporterAnimationType.Human;
            // Each file builds its own humanoid avatar; humanoid clips retarget between them.
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            // Materials (URP via material description) and embedded textures stay inside the FBX.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importAnimation = HeroVisualCatalog.TryParseClipPath(assetPath, out _, out _);
        }

        private void OnPreprocessAnimation()
        {
            if (!HeroVisualCatalog.TryParseClipPath(assetPath, out _, out string clip) ||
                !(assetImporter is ModelImporter importer))
                return;
            bool loop = HeroVisualCatalog.IsLoopingClip(clip);
            var clips = importer.defaultClipAnimations;
            foreach (var animation in clips)
            {
                if (clips.Length == 1) animation.name = clip;
                animation.loopTime = loop;
                // Gameplay moves the character; animations stay in place (root motion off).
                animation.lockRootRotation = true;
                animation.lockRootHeightY = true;
                animation.lockRootPositionXZ = true;
                animation.keepOriginalOrientation = true;
                animation.keepOriginalPositionY = true;
                animation.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }
    }
}
