using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MoodCharacters.EditorTools
{
    public static class MoodSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string AnimFolder = Root + "/Animations";
        private const string CharFolder = Root + "/Characters";
        private const string MusicFolder = Root + "/Audio/Music";
        private const string SfxFolder = Root + "/Audio/SFX";
        private const string MatFolder = Root + "/Materials";
        private const string SceneFolder = Root + "/Scenes";
        private const string ControllerPath = AnimFolder + "/MoodCharacter.controller";
        private const string ScenePath = SceneFolder + "/Main.unity";
        private const string FontFolder = Root + "/Fonts";

        private static TMP_FontAsset displayFont;
        private static TMP_FontAsset monoFont;

                
        [MenuItem("Mood Characters/1. Configure Imported Assets")]
        public static void ConfigureAssets()
        {
            EnsureFolders();
            foreach (string path in ModelPaths(AnimFolder))
            {
                ConfigureModel(path, true);
            }
            foreach (string path in ModelPaths(CharFolder))
            {
                ConfigureModel(path, false);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Mood Characters: import settings applied.");
        }

        [MenuItem("Mood Characters/2. Build Animator Controller")]
        public static AnimatorController BuildController()
        {
            EnsureFolders();
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Mood", AnimatorControllerParameterType.Int);
            controller.AddParameter("IsActive", AnimatorControllerParameterType.Bool);
            controller.AddParameter("React", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState idle = sm.AddState("Idle", new Vector3(300, 0, 0));
            AnimatorState happy = sm.AddState("Happy", new Vector3(600, -120, 0));
            AnimatorState angry = sm.AddState("Angry", new Vector3(650, 0, 0));
            AnimatorState sad = sm.AddState("Sad", new Vector3(600, 120, 0));
            AnimatorState react = sm.AddState("React", new Vector3(300, 220, 0));
            sm.defaultState = idle;

            idle.motion = FindAnimClip("happy");
            happy.motion = FindAnimClip("angry");
            angry.motion = FindAnimClip("idle");
            sad.motion = FindAnimClip("sad");
            react.motion = FindAnimClip("wav");

            AddMoodTransitions(idle, happy, 1);
            AddMoodTransitions(idle, angry, 2);
            AddMoodTransitions(idle, sad, 3);

            AnimatorStateTransition toReact = sm.AddAnyStateTransition(react);
            toReact.hasExitTime = false;
            toReact.duration = 0.15f;
            toReact.canTransitionToSelf = false;
            toReact.AddCondition(AnimatorConditionMode.If, 0, "React");

            AnimatorStateTransition reactBack = react.AddTransition(idle);
            reactBack.hasExitTime = true;
            reactBack.exitTime = 0.9f;
            reactBack.duration = 0.2f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Mood Characters: animator controller built.");
            return controller;
        }

        [MenuItem("Mood Characters/3. Build Scene")]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolders();
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = BuildController();
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera cam = Camera.main;
            cam.transform.position = new Vector3(0f, 1.6f, -3.2f);
            cam.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.1f);
            cam.gameObject.AddComponent<CameraFocus>();

            BuildStage();
            List<MoodCharacter> characters = BuildCharacters(controller);

            GameObject managers = new GameObject("--- Managers ---");
            GameManager gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.transform.SetParent(managers.transform);
            SerializedObject gmSo = new SerializedObject(gm);
            SerializedProperty list = gmSo.FindProperty("characters");
            list.arraySize = characters.Count;
            for (int i = 0; i < characters.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
            }
            gmSo.ApplyModifiedPropertiesWithoutUndo();

            BuildAudioManager(managers.transform);

            InputManager input = new GameObject("InputManager").AddComponent<InputManager>();
            input.transform.SetParent(managers.transform);
            SetRef(input, "raycastCamera", cam);

            BuildUI(gm);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Mood Characters: scene built and saved to " + ScenePath);
        }

        [MenuItem("Mood Characters/Build Everything")]
        public static void BuildEverything()
        {
            ConfigureAssets();
            BuildController();
            BuildScene();
        }

        private static void EnsureFolders()
        {
            string[] folders = { AnimFolder, CharFolder, Root + "/Audio", MusicFolder, SfxFolder, MatFolder, SceneFolder, Root + "/Prefabs" };
            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
                    AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
                }
            }
        }

        private static IEnumerable<string> ModelPaths(string folder)
        {
            return AssetDatabase.FindAssets("t:Model", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct();
        }

        private static void ConfigureModel(string path, bool isAnimation)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            if (isAnimation)
            {
                bool loop = !Path.GetFileName(path).ToLowerInvariant().Contains("wav");
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                foreach (ModelImporterClipAnimation clip in clips)
                {
                    clip.loopTime = loop;
                    clip.loopPose = loop;
                    clip.lockRootRotation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = true;
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionY = true;
                    clip.keepOriginalPositionXZ = true;
                }
                importer.clipAnimations = clips;
            }
            else
            {
                importer.importAnimation = false;
                string textureFolder = CharFolder + "/Textures";
                if (!AssetDatabase.IsValidFolder(textureFolder))
                {
                    AssetDatabase.CreateFolder(CharFolder, "Textures");
                }
                importer.ExtractTextures(textureFolder);
            }

            importer.SaveAndReimport();
        }

        private static AnimationClip FindAnimClip(string keyword)
        {
            foreach (string path in ModelPaths(AnimFolder))
            {
                string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool match = keyword == "idle"
                    ? file.Contains("idle") && !file.Contains("happy") && !file.Contains("sad") && !file.Contains("angry")
                    : file.Contains(keyword);
                if (!match)
                {
                    continue;
                }

                AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                if (clip != null)
                {
                    return clip;
                }
            }
            return null;
        }

        private static void AddMoodTransitions(AnimatorState idle, AnimatorState mood, int value)
        {
            AnimatorStateTransition enter = idle.AddTransition(mood);
            enter.hasExitTime = false;
            enter.duration = 0.2f;
            enter.AddCondition(AnimatorConditionMode.Equals, value, "Mood");
            enter.AddCondition(AnimatorConditionMode.If, 0, "IsActive");

            AnimatorStateTransition exit = mood.AddTransition(idle);
            exit.hasExitTime = false;
            exit.duration = 0.2f;
            exit.AddCondition(AnimatorConditionMode.NotEqual, value, "Mood");
        }

        private static Material MakeMaterial(string name, Color color, bool emissive)
        {
            string path = MatFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", color * 2.5f);
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildStage()
        {
            GameObject stage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stage.name = "Stage";
            stage.transform.position = new Vector3(0f, -0.05f, 0.4f);
            stage.transform.localScale = new Vector3(7.5f, 0.05f, 3.2f);
            stage.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Stage", new Color(0.16f, 0.16f, 0.2f), false);

            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Backdrop";
            back.transform.position = new Vector3(0f, 2.5f, 2.6f);
            back.transform.localScale = new Vector3(14f, 6f, 0.2f);
            back.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Backdrop", new Color(0.1f, 0.1f, 0.14f), false);
            Object.DestroyImmediate(back.GetComponent<Collider>());

            Light key = Object.FindAnyObjectByType<Light>();
            if (key != null)
            {
                key.transform.rotation = Quaternion.Euler(40f, 160f, 0f);
                key.intensity = 1.2f;
            }

            GameObject spot = new GameObject("Rim Light");
            Light rim = spot.AddComponent<Light>();
            rim.type = LightType.Point;
            rim.range = 8f;
            rim.intensity = 3f;
            rim.color = new Color(0.55f, 0.6f, 1f);
            spot.transform.position = new Vector3(0f, 3f, 1.8f);
        }

        private static List<MoodCharacter> BuildCharacters(AnimatorController controller)
        {
            List<GameObject> models = ModelPaths(CharFolder)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(g => g != null)
                .Take(3)
                .ToList();

            Color[] ringColors =
            {
                new Color(1f, 0.55f, 0.2f),
                new Color(0.3f, 0.9f, 0.6f),
                new Color(0.6f, 0.45f, 1f)
            };

            AudioClip neutral = FindClip(SfxFolder, "hmm", "neutral", "calm");
            AudioClip happy = FindClip(SfxFolder, "yay", "laugh", "happy");
            AudioClip angry = FindClip(SfxFolder, "grunt", "angry", "huh");
            AudioClip sad = FindClip(SfxFolder, "sigh", "sad");
            AudioClip react = FindClip(SfxFolder, "hey", "wave", "hello");

            List<MoodCharacter> result = new List<MoodCharacter>();
            GameObject group = new GameObject("--- Characters ---");

            for (int i = 0; i < 3; i++)
            {
                GameObject character;
                string displayName;
                if (i < models.Count)
                {
                    character = (GameObject)PrefabUtility.InstantiatePrefab(models[i]);
                    displayName = Pretty(models[i].name);
                    character.transform.localScale = Vector3.one * ScaleFor(displayName);
                }
                else
                {
                    character = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Object.DestroyImmediate(character.GetComponent<Collider>());
                    character.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                    displayName = "Placeholder " + (char)('A' + i);
                }

                character.name = displayName;
                character.transform.SetParent(group.transform);
                character.transform.position = new Vector3(-2f + 2f * i, 0f, 0f);
                character.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                Animator animator = character.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = character.AddComponent<Animator>();
                }
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                CapsuleCollider col = character.AddComponent<CapsuleCollider>();
                Vector3 scale = character.transform.localScale;
                col.center = new Vector3(0f, 0.9f / Mathf.Max(scale.y, 0.01f), 0f);
                col.height = 1.8f / Mathf.Max(scale.y, 0.01f);
                col.radius = 0.3f / Mathf.Max(scale.x, 0.01f);
                if (i >= models.Count)
                {
                    col.center = Vector3.zero;
                    col.height = 2f;
                    col.radius = 0.5f;
                    character.transform.position += Vector3.up * 0.9f;
                }

                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "SelectionRing";
                Object.DestroyImmediate(ring.GetComponent<Collider>());
                ring.transform.SetParent(character.transform, false);
                ring.transform.position = new Vector3(character.transform.position.x, 0.01f, 0f);
                ring.transform.localScale = DivideScale(new Vector3(0.9f, 0.005f, 0.9f), character.transform.lossyScale);
                ring.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Ring_" + i, ringColors[i], true);

                MoodCharacter mood = character.AddComponent<MoodCharacter>();
                SerializedObject so = new SerializedObject(mood);
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("selectionRing").objectReferenceValue = ring;
                so.FindProperty("neutralClip").objectReferenceValue = neutral;
                so.FindProperty("happyClip").objectReferenceValue = happy;
                so.FindProperty("angryClip").objectReferenceValue = angry;
                so.FindProperty("sadClip").objectReferenceValue = sad;
                so.FindProperty("reactClip").objectReferenceValue = react;
                so.ApplyModifiedPropertiesWithoutUndo();

                result.Add(mood);
            }
            return result;
        }

        private static float ScaleFor(string characterName)
        {
            switch (characterName.ToLowerInvariant())
            {
                case "remy": return 0.5f;
                case "amy": return 1.25f;
                default: return 1f;
            }
        }

        private static Vector3 DivideScale(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x / Mathf.Max(b.x, 0.0001f), a.y / Mathf.Max(b.y, 0.0001f), a.z / Mathf.Max(b.z, 0.0001f));
        }

        private static string Pretty(string raw)
        {
            string cleaned = new string(raw.Where(c => char.IsLetter(c) || c == ' ').ToArray()).Trim();
            if (string.IsNullOrEmpty(cleaned))
            {
                return raw;
            }
            return char.ToUpper(cleaned[0]) + cleaned.Substring(1);
        }

        private static AudioClip FindClip(string folder, params string[] keywords)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return null;
            }

            string[] paths = AssetDatabase.FindAssets("t:AudioClip", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();

            foreach (string keyword in keywords)
            {
                foreach (string path in paths)
                {
                    if (Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Contains(keyword))
                    {
                        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    }
                }
            }
            return null;
        }

        private static void BuildAudioManager(Transform parent)
        {
            GameObject go = new GameObject("AudioManager");
            go.transform.SetParent(parent);
            AudioSource[] sources = new AudioSource[4];
            for (int i = 0; i < 4; i++)
            {
                sources[i] = go.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
            }

            AudioManager audio = go.AddComponent<AudioManager>();
            SerializedObject so = new SerializedObject(audio);
            so.FindProperty("musicSourceA").objectReferenceValue = sources[0];
            so.FindProperty("musicSourceB").objectReferenceValue = sources[1];
            so.FindProperty("characterSource").objectReferenceValue = sources[2];
            so.FindProperty("uiSource").objectReferenceValue = sources[3];
            so.FindProperty("neutralMusic").objectReferenceValue = FindClip(MusicFolder, "neutral", "calm", "lofi");
            so.FindProperty("happyMusic").objectReferenceValue = FindClip(MusicFolder, "happy", "upbeat");
            so.FindProperty("angryMusic").objectReferenceValue = FindClip(MusicFolder, "angry", "tense", "drum");
            so.FindProperty("sadMusic").objectReferenceValue = FindClip(MusicFolder, "sad", "piano");
            so.FindProperty("clickClip").objectReferenceValue = FindClip(SfxFolder, "click");
            so.FindProperty("screenChangeClip").objectReferenceValue = FindClip(SfxFolder, "whoosh", "swoosh", "swipe");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRef(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_DefaultControls.Resources TmpResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
            };
        }

        private static DefaultControls.Resources UguiResources()
        {
            return new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
            };
        }

        private static RectTransform Place(GameObject go, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = go.AddComponent<RectTransform>();
            }
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        private static void Stretch(GameObject go, Transform parent)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = go.AddComponent<RectTransform>();
            }
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TMP_FontAsset LoadFont(string fileName)
        {
            string assetPath = FontFolder + "/" + fileName + " SDF.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            Font source = AssetDatabase.LoadAssetAtPath<Font>(FontFolder + "/" + fileName + ".ttf");
            if (source == null)
            {
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(source);
            asset.name = fileName + " SDF";
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.material.name = fileName + " SDF Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (Texture2D atlas in asset.atlasTextures)
            {
                atlas.name = fileName + " SDF Atlas";
                AssetDatabase.AddObjectToAsset(atlas, asset);
            }
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static TMP_Text Label(Transform parent, string name, string content, float size, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 box, TextAlignmentOptions align, Color color, bool display = false)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Place(go, parent, anchor, pivot, pos, box);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = display ? displayFont : monoFont;
            if (font != null)
            {
                t.font = font;
            }
            t.text = content;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.textWrappingMode = display ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.characterSpacing = display ? -4f : 4f;
            t.lineSpacing = display ? -28f : 0f;
            if (display && displayFont == null)
            {
                t.fontStyle = FontStyles.Bold;
            }
            return t;
        }

        private static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Place(go, parent, anchor, pivot, pos, size);
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void Frame(Transform target, Color color, float thickness)
        {
            Vector2[] mins = { new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
            Vector2[] maxs = { new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            Vector2[] sizes = { new Vector2(0f, thickness), new Vector2(0f, thickness), new Vector2(thickness, 0f), new Vector2(thickness, 0f) };
            for (int i = 0; i < 4; i++)
            {
                GameObject edge = new GameObject("Hairline", typeof(RectTransform));
                RectTransform rt = edge.GetComponent<RectTransform>();
                rt.SetParent(target, false);
                rt.anchorMin = mins[i];
                rt.anchorMax = maxs[i];
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = sizes[i];
                Image img = edge.AddComponent<Image>();
                img.color = color;
                img.raycastTarget = false;
            }
        }

        private static void Crosshairs(Transform parent)
        {
            Vector2[] corners = { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
            foreach (Vector2 corner in corners)
            {
                Vector2 inset = new Vector2(corner.x < 0.5f ? 22f : -22f, corner.y < 0.5f ? 22f : -22f);
                Label(parent, "Crosshair", "+", 30, corner, new Vector2(0.5f, 0.5f), inset, new Vector2(40f, 40f), TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.45f));
            }
        }

        private static Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color bg, Color textColor, float fontSize = 22f)
        {
            GameObject go = TMP_DefaultControls.CreateButton(TmpResources());
            go.name = name;
            Place(go, parent, anchor, new Vector2(0.5f, 0.5f), pos, size);
            Image img = go.GetComponent<Image>();
            img.sprite = null;
            img.type = Image.Type.Simple;
            img.color = bg;
            Frame(go.transform, new Color(1f, 1f, 1f, 0.55f), 2f);
            TMP_Text t = go.GetComponentInChildren<TMP_Text>();
            if (monoFont != null)
            {
                t.font = monoFont;
            }
            t.text = label;
            t.fontSize = fontSize;
            t.color = textColor;
            t.fontStyle = FontStyles.Normal;
            t.characterSpacing = 4f;
            go.AddComponent<ButtonClickSound>();
            return go.GetComponent<Button>();
        }

        private static UIScreen MakeScreen(Transform canvas, string name, int state, bool background)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Stretch(go, canvas);
            go.AddComponent<CanvasGroup>();
            if (background)
            {
                Image bg = go.AddComponent<Image>();
                bg.color = new Color(0.04f, 0.04f, 0.06f, 0.86f);
            }
            UIScreen screen = go.AddComponent<UIScreen>();
            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("stateWhileOpen").enumValueIndex = state;
            so.ApplyModifiedPropertiesWithoutUndo();
            return screen;
        }

        private static Slider MakeSlider(Transform parent, string name, string label, float y)
        {
            Label(parent, name + "Label", label, 22, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-300f, y), new Vector2(200f, 50f), TextAlignmentOptions.Left, Color.white);
            GameObject go = DefaultControls.CreateSlider(UguiResources());
            go.name = name;
            Place(go, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110f, y), new Vector2(380f, 20f));
            foreach (Image img in go.GetComponentsInChildren<Image>(true))
            {
                img.sprite = null;
                img.type = Image.Type.Simple;
            }
            Transform background = go.transform.Find("Background");
            if (background != null)
            {
                background.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
            }
            Transform fill = go.transform.Find("Fill Area/Fill");
            if (fill != null)
            {
                fill.GetComponent<Image>().color = Color.white;
            }
            Transform handle = go.transform.Find("Handle Slide Area/Handle");
            if (handle != null)
            {
                handle.GetComponent<Image>().color = new Color(1f, 0.8f, 0.25f);
            }
            Slider slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        private static void BuildUI(GameManager gm)
        {
            displayFont = LoadFont("InterDisplay-Bold");
            monoFont = LoadFont("JetBrainsMono-Medium");

            GameObject canvasGo = new GameObject("UI", typeof(RectTransform));
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            UIManager ui = canvasGo.AddComponent<UIManager>();

            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif

            Transform root = canvasGo.transform;
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 left = new Vector2(0f, 0.5f);
            Vector2 topLeft = new Vector2(0f, 1f);
            Vector2 topRight = new Vector2(1f, 1f);
            Vector2 bottom = new Vector2(0.5f, 0f);
            Color ink = new Color(0.06f, 0.06f, 0.08f);
            Color glass = new Color(0.05f, 0.05f, 0.07f, 0.6f);
            Color muted = new Color(1f, 1f, 1f, 0.55f);

            UIScreen menu = MakeScreen(root, "Screen_MainMenu", 0, true);
            UIScreen stage = MakeScreen(root, "Screen_Stage", 1, false);
            UIScreen settings = MakeScreen(root, "Screen_Settings", 2, true);

            Crosshairs(menu.transform);
            Label(menu.transform, "Kicker", "[ INTERACTIVE CHARACTER SYSTEM ]  /  2026", 20, left, new Vector2(0f, 0.5f), new Vector2(124f, 360f), new Vector2(900f, 30f), TextAlignmentOptions.Left, muted);
            Label(menu.transform, "Title", "MOOD\nCHARACTERS", 170, left, new Vector2(0f, 0.5f), new Vector2(112f, 190f), new Vector2(1700f, 330f), TextAlignmentOptions.Left, Color.white, true);
            Label(menu.transform, "Subtitle", "PICK A CHARACTER. CHANGE HOW IT FEELS.", 22, left, new Vector2(0f, 0.5f), new Vector2(124f, -10f), new Vector2(900f, 30f), TextAlignmentOptions.Left, muted);
            Button start = MakeButton(menu.transform, "Btn_Start", "[01]  START", left, new Vector2(314f, -110f), new Vector2(380f, 76f), Color.white, ink, 24);
            Button openSettings = MakeButton(menu.transform, "Btn_Settings", "[02]  SETTINGS", left, new Vector2(314f, -200f), new Vector2(380f, 76f), glass, Color.white, 24);
            Button quit = MakeButton(menu.transform, "Btn_Quit", "[03]  QUIT", left, new Vector2(314f, -290f), new Vector2(380f, 76f), glass, Color.white, 24);

            Crosshairs(stage.transform);
            TMP_Text indexText = Label(stage.transform, "IndexText", "[01/03]", 22, topLeft, topLeft, new Vector2(52f, -48f), new Vector2(400f, 30f), TextAlignmentOptions.Left, muted);
            TMP_Text nameText = Label(stage.transform, "CharacterName", "CHARACTER", 150, topLeft, topLeft, new Vector2(40f, -64f), new Vector2(1200f, 160f), TextAlignmentOptions.Left, Color.white, true);
            Panel(stage.transform, "Rule", topLeft, topLeft, new Vector2(52f, -238f), new Vector2(520f, 2f), new Color(1f, 1f, 1f, 0.3f));
            Image badge = Panel(stage.transform, "MoodBadge", topLeft, topLeft, new Vector2(52f, -262f), new Vector2(16f, 16f), Color.grey);
            TMP_Text moodText = Label(stage.transform, "MoodText", "MOOD / NEUTRAL", 26, topLeft, topLeft, new Vector2(82f, -254f), new Vector2(600f, 34f), TextAlignmentOptions.Left, Color.white);
            TMP_Text animText = Label(stage.transform, "AnimStateText", "STATE / IDLE", 20, topLeft, topLeft, new Vector2(52f, -298f), new Vector2(600f, 30f), TextAlignmentOptions.Left, muted);

            GameObject dropdownGo = TMP_DefaultControls.CreateDropdown(TmpResources());
            dropdownGo.name = "CharacterDropdown";
            Place(dropdownGo, stage.transform, topRight, topRight, new Vector2(-52f, -44f), new Vector2(340f, 60f));
            Image dropdownImage = dropdownGo.GetComponent<Image>();
            dropdownImage.sprite = null;
            dropdownImage.color = glass;
            Frame(dropdownGo.transform, new Color(1f, 1f, 1f, 0.55f), 2f);
            TMP_Dropdown dropdown = dropdownGo.GetComponent<TMP_Dropdown>();
            foreach (TMP_Text t in dropdownGo.GetComponentsInChildren<TMP_Text>(true))
            {
                if (monoFont != null)
                {
                    t.font = monoFont;
                }
                t.characterSpacing = 4f;
            }
            dropdown.captionText.fontSize = 22;
            dropdown.captionText.color = Color.white;
            dropdown.itemText.fontSize = 22;

            Button stageSettings = MakeButton(stage.transform, "Btn_Settings", "SETTINGS", topRight, new Vector2(-132f, -150f), new Vector2(160f, 56f), glass, Color.white, 20);
            Button stageMenu = MakeButton(stage.transform, "Btn_Menu", "MENU", topRight, new Vector2(-312f, -150f), new Vector2(160f, 56f), glass, Color.white, 20);

            Label(stage.transform, "Hint", "[1-4] MOOD   [LEFT/RIGHT] SWITCH   [SPACE / CLICK] WAVE   [ESC] BACK", 18, bottom, bottom, new Vector2(0f, 132f), new Vector2(1400f, 30f), TextAlignmentOptions.Center, muted);

            string[] moodNames = { "NEUTRAL", "HAPPY", "ANGRY", "SAD" };
            float[] moodX = { -430f, -240f, -50f, 140f };
            Image[] moodImages = new Image[4];
            TMP_Text[] moodLabels = new TMP_Text[4];
            Button[] moodButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                moodButtons[i] = MakeButton(stage.transform, "Btn_" + moodNames[i], "[" + (i + 1) + "] " + moodNames[i], bottom, new Vector2(moodX[i], 70f), new Vector2(178f, 72f), glass, Color.white, 20);
                moodImages[i] = moodButtons[i].GetComponent<Image>();
                moodLabels[i] = moodButtons[i].GetComponentInChildren<TMP_Text>();
            }
            Button wave = MakeButton(stage.transform, "Btn_React", "[SPACE] WAVE", bottom, new Vector2(356f, 70f), new Vector2(210f, 72f), glass, Color.white, 20);
            Button prev = MakeButton(stage.transform, "Btn_Prev", "<", bottom, new Vector2(-610f, 70f), new Vector2(80f, 72f), glass, Color.white, 28);
            Button next = MakeButton(stage.transform, "Btn_Next", ">", bottom, new Vector2(546f, 70f), new Vector2(80f, 72f), glass, Color.white, 28);

            InteractionHUD hud = stage.gameObject.AddComponent<InteractionHUD>();
            SerializedObject hudSo = new SerializedObject(hud);
            hudSo.FindProperty("characterNameText").objectReferenceValue = nameText;
            hudSo.FindProperty("indexText").objectReferenceValue = indexText;
            hudSo.FindProperty("moodText").objectReferenceValue = moodText;
            hudSo.FindProperty("animationStateText").objectReferenceValue = animText;
            hudSo.FindProperty("moodBadge").objectReferenceValue = badge;
            hudSo.FindProperty("characterDropdown").objectReferenceValue = dropdown;
            hudSo.FindProperty("unselectedButtonColor").colorValue = glass;
            SerializedProperty images = hudSo.FindProperty("moodButtonImages");
            SerializedProperty labels = hudSo.FindProperty("moodButtonLabels");
            images.arraySize = 4;
            labels.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                images.GetArrayElementAtIndex(i).objectReferenceValue = moodImages[i];
                labels.GetArrayElementAtIndex(i).objectReferenceValue = moodLabels[i];
            }
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            Image settingsPanel = Panel(settings.transform, "Panel", center, center, Vector2.zero, new Vector2(760f, 660f), new Color(0.08f, 0.08f, 0.11f, 0.96f));
            Frame(settingsPanel.transform, new Color(1f, 1f, 1f, 0.3f), 2f);
            Transform sp = settingsPanel.transform;
            Label(sp, "Kicker", "[ AUDIO ]", 20, center, center, new Vector2(0f, 290f), new Vector2(600f, 30f), TextAlignmentOptions.Center, muted);
            Label(sp, "Title", "SETTINGS", 84, center, center, new Vector2(0f, 225f), new Vector2(700f, 100f), TextAlignmentOptions.Center, Color.white, true);
            Slider master = MakeSlider(sp, "Slider_Master", "MASTER", 120f);
            Slider music = MakeSlider(sp, "Slider_Music", "MUSIC", 40f);
            Slider sfx = MakeSlider(sp, "Slider_SFX", "EFFECTS", -40f);

            GameObject toggleGo = DefaultControls.CreateToggle(UguiResources());
            toggleGo.name = "Toggle_Mute";
            Place(toggleGo, sp, center, center, new Vector2(-280f, -120f), new Vector2(36f, 36f));
            foreach (Text legacy in toggleGo.GetComponentsInChildren<Text>(true))
            {
                Object.DestroyImmediate(legacy.gameObject);
            }
            RectTransform toggleBg = toggleGo.transform.GetChild(0) as RectTransform;
            if (toggleBg != null)
            {
                toggleBg.anchorMin = Vector2.zero;
                toggleBg.anchorMax = Vector2.one;
                toggleBg.offsetMin = Vector2.zero;
                toggleBg.offsetMax = Vector2.zero;
            }
            foreach (Image img in toggleGo.GetComponentsInChildren<Image>(true))
            {
                img.sprite = null;
                img.type = Image.Type.Simple;
                img.color = img.transform == toggleBg ? new Color(1f, 1f, 1f, 0.15f) : new Color(1f, 0.8f, 0.25f);
            }
            Toggle mute = toggleGo.GetComponent<Toggle>();
            mute.isOn = false;
            Label(sp, "MuteLabel", "MUTE ALL SOUND", 22, center, new Vector2(0f, 0.5f), new Vector2(-240f, -120f), new Vector2(400f, 40f), TextAlignmentOptions.Left, Color.white);
            TMP_Text status = Label(sp, "StatusText", "MASTER 100%", 20, center, center, new Vector2(0f, -190f), new Vector2(600f, 30f), TextAlignmentOptions.Center, muted);
            Button back = MakeButton(sp, "Btn_Back", "[ESC]  BACK", center, new Vector2(0f, -262f), new Vector2(300f, 72f), Color.white, ink, 22);

            SettingsScreen settingsScript = settings.gameObject.AddComponent<SettingsScreen>();
            SerializedObject setSo = new SerializedObject(settingsScript);
            setSo.FindProperty("masterSlider").objectReferenceValue = master;
            setSo.FindProperty("musicSlider").objectReferenceValue = music;
            setSo.FindProperty("sfxSlider").objectReferenceValue = sfx;
            setSo.FindProperty("muteToggle").objectReferenceValue = mute;
            setSo.FindProperty("statusText").objectReferenceValue = status;
            setSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject uiSo = new SerializedObject(ui);
            uiSo.FindProperty("firstScreen").objectReferenceValue = menu;
            SerializedProperty screens = uiSo.FindProperty("screens");
            screens.arraySize = 3;
            screens.GetArrayElementAtIndex(0).objectReferenceValue = menu;
            screens.GetArrayElementAtIndex(1).objectReferenceValue = stage;
            screens.GetArrayElementAtIndex(2).objectReferenceValue = settings;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddObjectPersistentListener<UIScreen>(start.onClick, ui.Open, stage);
            UnityEventTools.AddObjectPersistentListener<UIScreen>(openSettings.onClick, ui.Open, settings);
            UnityEventTools.AddPersistentListener(quit.onClick, ui.QuitApp);
            UnityEventTools.AddObjectPersistentListener<UIScreen>(stageSettings.onClick, ui.Open, settings);
            UnityEventTools.AddPersistentListener(stageMenu.onClick, ui.GoHome);
            UnityEventTools.AddPersistentListener(back.onClick, ui.Back);
            UnityEventTools.AddPersistentListener(wave.onClick, gm.TriggerReaction);
            UnityEventTools.AddPersistentListener(prev.onClick, gm.PreviousCharacter);
            UnityEventTools.AddPersistentListener(next.onClick, gm.NextCharacter);
            for (int i = 0; i < 4; i++)
            {
                UnityEventTools.AddIntPersistentListener(moodButtons[i].onClick, gm.SetMoodIndex, i);
            }
        }
    }
}
