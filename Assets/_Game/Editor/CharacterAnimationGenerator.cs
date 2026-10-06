#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Game.Gameplay.Player;
using Game.Presentation.Animation;

namespace Game.Editor
{
    public static class CharacterAnimationGenerator
    {
        private const string AnimFolder = "Assets/_Game/Presentation/Animation";

        [MenuItem("Game/Generate Character Animations & Controller", priority = 51)]
        public static void GenerateAll()
        {
            if (!AssetDatabase.IsValidFolder(AnimFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Game/Presentation", "Animation");
            }

            // 1. Load Sliced Sprites
            var idleSprites = LoadSprites("Assets/assets/SpriteSheet/character_idle_breath_strip.png");
            var runSprites = LoadSprites("Assets/assets/SpriteSheet/character_run_strip.png");
            var jumpSprites = LoadSprites("Assets/assets/SpriteSheet/character_jump_strip.png");
            var zerogSprites = LoadSprites("Assets/assets/SpriteSheet/character_zerog_strip.png");

            if (idleSprites == null || idleSprites.Length == 0 ||
                runSprites == null || runSprites.Length == 0 ||
                jumpSprites == null || jumpSprites.Length == 0 ||
                zerogSprites == null || zerogSprites.Length == 0)
            {
                Debug.LogError("[CharacterAnimationGenerator] Missing sliced character sprites. Run 'Game/Slice Character Sprite Sheets' first.");
                return;
            }

            // 2. Create Animation Clips
            var idleClip = CreateSpriteClip($"{AnimFolder}/Player_Idle.anim", idleSprites, 6f, loop: true);
            var runClip = CreateSpriteClip($"{AnimFolder}/Player_Run.anim", runSprites, 10f, loop: true);
            
            // Jump takeoff & apex (frames 0, 1, 2)
            var jumpFrames = new Sprite[] { jumpSprites[0], jumpSprites[1], jumpSprites[2] };
            var jumpClip = CreateSpriteClip($"{AnimFolder}/Player_Jump.anim", jumpFrames, 8f, loop: false);

            // Fall & land (frames 3, 4)
            var fallFrames = new Sprite[] { jumpSprites[3], jumpSprites[4] };
            var fallClip = CreateSpriteClip($"{AnimFolder}/Player_Fall.anim", fallFrames, 8f, loop: true);

            var zerogClip = CreateSpriteClip($"{AnimFolder}/Player_ZeroG.anim", zerogSprites, 6f, loop: true);

            // Climb clip (alternating run frames)
            var climbFrames = new Sprite[] { runSprites[1], runSprites[3] };
            var climbClip = CreateSpriteClip($"{AnimFolder}/Player_Climb.anim", climbFrames, 6f, loop: true);

            // 3. Create Animator Controller
            string controllerPath = $"{AnimFolder}/PlayerAnimatorController.controller";
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // Add Parameters
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsClimbing", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsZeroG", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Add States
            var idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            var runState = rootStateMachine.AddState("Run");
            runState.motion = runClip;

            var jumpState = rootStateMachine.AddState("Jump");
            jumpState.motion = jumpClip;

            var fallState = rootStateMachine.AddState("Fall");
            fallState.motion = fallClip;

            var climbState = rootStateMachine.AddState("Climb");
            climbState.motion = climbClip;

            var zerogState = rootStateMachine.AddState("ZeroG");
            zerogState.motion = zerogClip;

            rootStateMachine.defaultState = idleState;

            EditorUtility.SetDirty(idleState);
            EditorUtility.SetDirty(runState);
            EditorUtility.SetDirty(jumpState);
            EditorUtility.SetDirty(fallState);
            EditorUtility.SetDirty(climbState);
            EditorUtility.SetDirty(zerogState);
            EditorUtility.SetDirty(controller);

            // Transitions: Idle <-> Run
            var idleToRun = idleState.AddTransition(runState);
            idleToRun.hasExitTime = false;
            idleToRun.duration = 0.05f;
            idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var runToIdle = runState.AddTransition(idleState);
            runToIdle.hasExitTime = false;
            runToIdle.duration = 0.05f;
            runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // Transitions: AnyState -> Jump
            var anyToJump = rootStateMachine.AddAnyStateTransition(jumpState);
            anyToJump.hasExitTime = false;
            anyToJump.duration = 0.05f;
            anyToJump.canTransitionToSelf = false;
            anyToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            anyToJump.AddCondition(AnimatorConditionMode.Greater, 0.2f, "VerticalVelocity");
            anyToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsClimbing");
            anyToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsZeroG");

            // Transitions: AnyState -> Fall
            var anyToFall = rootStateMachine.AddAnyStateTransition(fallState);
            anyToFall.hasExitTime = false;
            anyToFall.duration = 0.05f;
            anyToFall.canTransitionToSelf = false;
            anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
            anyToFall.AddCondition(AnimatorConditionMode.Less, -0.2f, "VerticalVelocity");
            anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsClimbing");
            anyToFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsZeroG");

            // Transitions: Jump/Fall -> Idle
            var jumpToIdle = jumpState.AddTransition(idleState);
            jumpToIdle.hasExitTime = false;
            jumpToIdle.duration = 0.05f;
            jumpToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            jumpToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            var fallToIdle = fallState.AddTransition(idleState);
            fallToIdle.hasExitTime = false;
            fallToIdle.duration = 0.05f;
            fallToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            fallToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // Transitions: Jump/Fall -> Run
            var jumpToRun = jumpState.AddTransition(runState);
            jumpToRun.hasExitTime = false;
            jumpToRun.duration = 0.05f;
            jumpToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            jumpToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var fallToRun = fallState.AddTransition(runState);
            fallToRun.hasExitTime = false;
            fallToRun.duration = 0.05f;
            fallToRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
            fallToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            // Transitions: AnyState -> Climb
            var anyToClimb = rootStateMachine.AddAnyStateTransition(climbState);
            anyToClimb.hasExitTime = false;
            anyToClimb.duration = 0.05f;
            anyToClimb.canTransitionToSelf = false;
            anyToClimb.AddCondition(AnimatorConditionMode.If, 0f, "IsClimbing");

            var climbToIdle = climbState.AddTransition(idleState);
            climbToIdle.hasExitTime = false;
            climbToIdle.duration = 0.05f;
            climbToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsClimbing");

            // Transitions: AnyState -> ZeroG
            var anyToZeroG = rootStateMachine.AddAnyStateTransition(zerogState);
            anyToZeroG.hasExitTime = false;
            anyToZeroG.duration = 0.05f;
            anyToZeroG.canTransitionToSelf = false;
            anyToZeroG.AddCondition(AnimatorConditionMode.If, 0f, "IsZeroG");

            var zeroGToIdle = zerogState.AddTransition(idleState);
            zeroGToIdle.hasExitTime = false;
            zeroGToIdle.duration = 0.05f;
            zeroGToIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsZeroG");
            zeroGToIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");

            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterAnimationGenerator] Created all AnimationClips and PlayerAnimatorController successfully.");

            // 4. Setup Player in Scene
            ConfigureScenePlayer(controller, idleSprites[0]);
        }

        public static void ConfigureScenePlayer(RuntimeAnimatorController animatorController, Sprite defaultSprite)
        {
            var playerGO = GameObject.Find("Player") ?? GameObject.Find("Controller====/Player");
            if (playerGO == null)
            {
                Debug.LogWarning("[CharacterAnimationGenerator] Player GameObject not found in active scene.");
                return;
            }

            Undo.RecordObject(playerGO, "Configure Character Animation and Scale");

            // SpriteRenderer
            var sr = playerGO.GetComponent<SpriteRenderer>();
            if (sr == null) sr = playerGO.AddComponent<SpriteRenderer>();
            sr.sprite = defaultSprite;
            sr.sortingOrder = 8;
            sr.color = Color.white;

            // Animator
            var animator = playerGO.GetComponent<Animator>();
            if (animator == null) animator = playerGO.AddComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;

            // PlayerVisualAnimator
            var visualAnim = playerGO.GetComponent<PlayerVisualAnimator>();
            if (visualAnim == null) visualAnim = playerGO.AddComponent<PlayerVisualAnimator>();

            // CapsuleCollider2D: Perfectly sized for ~2.76m character at PPU 100
            var col = playerGO.GetComponent<CapsuleCollider2D>();
            if (col == null) col = playerGO.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.85f, 2.45f);
            col.offset = new Vector2(0f, 1.25f); // Bottom touches ground at y = 0

            // Smooth physics interpolation
            var rb = playerGO.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            // Ensure scale is 1.0 (PPU 100 gives golden 2.76m character)
            playerGO.transform.localScale = Vector3.one;

            // Configure AutonomousPlayerController locomotion mode for easy testing
            var controllerComp = playerGO.GetComponent<AutonomousPlayerController>();
            if (controllerComp != null)
            {
                controllerComp.SetLocomotionMode(LocomotionMode.Manual);
            }

            EditorUtility.SetDirty(playerGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(playerGO.scene);
            Debug.Log("[CharacterAnimationGenerator] Player configured with Animator, PlayerVisualAnimator, grounded collider, and Manual locomotion mode.");
        }

        private static Sprite[] LoadSprites(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();
        }

        private static AnimationClip CreateSpriteClip(string path, Sprite[] sprites, float frameRate, bool loop)
        {
            string clipName = Path.GetFileNameWithoutExtension(path);
            var clip = new AnimationClip { name = clipName, frameRate = frameRate };

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            var keyframes = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / frameRate,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                existing.name = clipName;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
    }
}
#endif
