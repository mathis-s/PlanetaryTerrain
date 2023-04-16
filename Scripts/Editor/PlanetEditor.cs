using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlanetaryTerrain.Foliage;
using PlanetaryTerrain;

namespace PlanetaryTerrain.EditorUtils
{
    [CustomEditor(typeof(Planet))]
    public class PlanetEditor : Editor
    {
        private enum Tab
        {
            General, Generation, Visual, Foliage, Events, Debug
        }

        Texture2D rangePreview;

        Tab tab = Tab.General;
        Planet planet;
        SerializedProperty detailDistances, generateColliders, detailMSDs;
        SerializedProperty scaledSpaceMaterial, planetMaterial;
        SerializedProperty foliageBiomes;
        SerializedProperty eventScSpEntered, eventScSpLeft, eventFinishedGeneration;
        GradientEditor gradientEditor;

        string[] tabNames = { "General", "Generation", "Visual", "Foliage", "Events", "Debug" };
        bool showColorEditor = false;
        bool showExpFoliage = false;


        public static Color32[] defaultSequence
        {
            get
            {
                return new Color32[] {
                                    new Color32(0x1f, 0x77, 0xb4, 0xff),
                                    new Color32(0xff, 0x7f, 0x0e, 0xff),
                                    new Color32(0x2c, 0xa0, 0x2c, 0xff),
                                    new Color32(0xd6, 0x27, 0x28, 0xff),
                                    new Color32(0x94, 0x67, 0xbd, 0xff),
                                    new Color32(0x8c, 0x56, 0x4b, 0xff)
                                    };
            }
        }

        public void OnEnable()
        {
            planet = (Planet)target;

            detailDistances = serializedObject.FindProperty("detailDistances");
            generateColliders = serializedObject.FindProperty("generateColliders");
            detailMSDs = serializedObject.FindProperty("detailMsds");
            scaledSpaceMaterial = serializedObject.FindProperty("scaledSpaceMaterial");
            planetMaterial = serializedObject.FindProperty("planetMaterial");
            foliageBiomes = serializedObject.FindProperty("foliageBiomes");

            eventScSpEntered = serializedObject.FindProperty("enteredScaledSpace");
            eventScSpLeft = serializedObject.FindProperty("leftScaledSpace");
            eventFinishedGeneration = serializedObject.FindProperty("eventFinishedGeneration");
        }

        public override void OnInspectorGUI()
        {

            EditorGUILayout.Space();
            tab = (Tab)GUILayout.Toolbar((int)tab, tabNames, EditorStyles.toolbarButton);
            EditorGUILayout.Space();

            switch (tab)
            {
                case Tab.General:

                    planet.radius = EditorGUILayout.FloatField("Radius", planet.radius);
                    EditorGUILayout.PropertyField(detailDistances, true);
                    planet.calculateMsds = EditorGUILayout.Toggle(new GUIContent("Calculate MSDs", "The MSD is the bumpiness of the quad. When calculated, bumpyness thresholds can be set for splitting quads."), planet.calculateMsds);
                    if (planet.calculateMsds)
                        EditorGUILayout.PropertyField(detailMSDs, true);
                    EditorGUILayout.PropertyField(generateColliders, true);
                    GUILayout.Space(5f);
                    planet.lodModeBehindCam = (LODModeBehindCam)EditorGUILayout.EnumPopup(new GUIContent("LOD Mode behind Camera", "How are quads behind the camera handled?"), planet.lodModeBehindCam);
                    if (planet.lodModeBehindCam == LODModeBehindCam.NotComputed)
                        planet.behindCameraExtraRange = EditorGUILayout.FloatField(new GUIContent("LOD Extra Range", "Extra Range for quads behind the Camera. Increase for large planets."), planet.behindCameraExtraRange);
                    GUILayout.Space(5f);
                    planet.recomputeQuadDistancesThreshold = EditorGUILayout.FloatField(new GUIContent("Recompute Quad Threshold", "Threshold for recomputing all quad distances. Increase for better performance while moving with many quads."), planet.recomputeQuadDistancesThreshold);
                    planet.updateAllQuads = EditorGUILayout.Toggle(new GUIContent("Update all Quads simultaneously", "Update all Quads in one frame or over multiple frames? Only turn on when player is very fast and planet has few quads."), planet.updateAllQuads);
                    if (!planet.updateAllQuads)
                        planet.maxQuadsToUpdate = EditorGUILayout.IntField(new GUIContent("Max Quads to update per frame", "Max Quads to update in one frame. Lower value means process of updating all Quads takes longer, fewer spikes of lower framerates. If it takes too long, the next update tries to start while the last one is still running, warning and suggestion to increase maxQuadsToUpdate will be logged."), planet.maxQuadsToUpdate);
                    planet.floatingOrigin = (FloatingOrigin)EditorGUILayout.ObjectField("Floating Origin (if used)", planet.floatingOrigin, typeof(FloatingOrigin), true);
                    planet.hideQuads = EditorGUILayout.Toggle("Hide Quads in Hierarchy", planet.hideQuads);
                    int qs = planet.quadSize;
                    planet.quadSize = EditorGUILayout.IntSlider(new GUIContent("Quad Size", "Vertices per quad side. Total number of vertices per Quad is this squared."), planet.quadSize, 5, 253);
                    if (planet.quadSize % 2 == 0 || ((planet.quadSize - 1) / 2) % 2 != 0)
                        planet.quadSize = qs;


                    break;

                case Tab.Generation:
                    planet.serializedInherited.heightProviderType = (HeightProviderType)EditorGUILayout.EnumPopup("Generation Mode", planet.serializedInherited.heightProviderType);
                    GUILayout.Space(5f);

                    switch (planet.serializedInherited.heightProviderType)
                    {
                        case HeightProviderType.Heightmap:
                            planet.serializedInherited.heightmapHeightProvider.heightmapTextAsset = (TextAsset)EditorGUILayout.ObjectField("Heightmap", planet.serializedInherited.heightmapHeightProvider.heightmapTextAsset, typeof(TextAsset), false);
                            planet.serializedInherited.heightmapHeightProvider.useBicubicInterpolation = EditorGUILayout.Toggle("Use Bicubic Interpolation", planet.serializedInherited.heightmapHeightProvider.useBicubicInterpolation);
                            break;

                        case HeightProviderType.Noise:
                            planet.serializedInherited.noiseHeightProvider.noiseSerialized = (TextAsset)EditorGUILayout.ObjectField("Noise", planet.serializedInherited.noiseHeightProvider.noiseSerialized, typeof(TextAsset), false);
                            break;

                        case HeightProviderType.Hybrid:
                            planet.serializedInherited.hybridHeightProvider.heightmapTextAsset = (TextAsset)EditorGUILayout.ObjectField("Heightmap", planet.serializedInherited.hybridHeightProvider.heightmapTextAsset, typeof(TextAsset), false);
                            planet.serializedInherited.hybridHeightProvider.useBicubicInterpolation = EditorGUILayout.Toggle("Use Bicubic Interpolation", planet.serializedInherited.hybridHeightProvider.useBicubicInterpolation);
                            GUILayout.Space(5f);
                            planet.serializedInherited.hybridHeightProvider.noiseSerialized = (TextAsset)EditorGUILayout.ObjectField("Noise", planet.serializedInherited.hybridHeightProvider.noiseSerialized, typeof(TextAsset), false);
                            planet.serializedInherited.hybridHeightProvider.hybridModeNoiseDiv = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Noise Divisor", "Increase for noise to be less pronounced."), planet.serializedInherited.hybridHeightProvider.hybridModeNoiseDiv), float.Epsilon, float.MaxValue);
                            break;
                        case HeightProviderType.Const:
                            planet.serializedInherited.constHeightProvider.constant = EditorGUILayout.FloatField("Constant Height", planet.serializedInherited.constHeightProvider.constant);
                            break;

                        case HeightProviderType.ComputeShader:
                            planet.computeShader = (ComputeShader)EditorGUILayout.ObjectField("Compute Shader", planet.computeShader, typeof(ComputeShader), false);
                            break;

                        case HeightProviderType.StreamingHeightmap:


                            EditorGUILayout.HelpBox("A streaming heightmap needs a high resolution heightmap and a low-res copy (Base Heightmap). The copy is assigned in the editor as usual, but the high-res heightmap isn't. You need to set the path of the high-res heightmap. This path might not be vaild any more after building!", MessageType.None);

                            planet.serializedInherited.streamingHeightmapHeightProvider.baseHeightmapTextAsset = (TextAsset)EditorGUILayout.ObjectField("Base Heightmap", planet.serializedInherited.streamingHeightmapHeightProvider.baseHeightmapTextAsset, typeof(TextAsset), false);
                            planet.serializedInherited.streamingHeightmapHeightProvider.heightmapPath = EditorGUILayout.TextField("Path", planet.serializedInherited.streamingHeightmapHeightProvider.heightmapPath);

                            if (GUILayout.Button("Select"))
                                planet.serializedInherited.streamingHeightmapHeightProvider.heightmapPath = EditorUtility.OpenFilePanel("Heightmap", Application.dataPath, "bytes");

                            planet.serializedInherited.streamingHeightmapHeightProvider.useBicubicInterpolation = EditorGUILayout.Toggle("Use Bicubic Interpolation", planet.serializedInherited.streamingHeightmapHeightProvider.useBicubicInterpolation);
                            planet.serializedInherited.streamingHeightmapHeightProvider.loadSize = EditorGUILayout.Vector2Field("Loaded Area Size", planet.serializedInherited.streamingHeightmapHeightProvider.loadSize);
                            planet.serializedInherited.streamingHeightmapHeightProvider.reloadThreshold = EditorGUILayout.FloatField("Reload Threshold", planet.serializedInherited.streamingHeightmapHeightProvider.reloadThreshold);
                            break;

                        case HeightProviderType.DetailHeightmaps:

                            planet.serializedInherited.detailHeightmapHeightProvider.baseHeightmapTextAsset = (TextAsset)EditorGUILayout.ObjectField("Base Heightmap", planet.serializedInherited.detailHeightmapHeightProvider.baseHeightmapTextAsset, typeof(TextAsset), false);
                            planet.serializedInherited.detailHeightmapHeightProvider.useBicubicInterpolation = EditorGUILayout.Toggle("Use Bicubic Interpolation", planet.serializedInherited.detailHeightmapHeightProvider.useBicubicInterpolation);

                            var dh = planet.serializedInherited.detailHeightmapHeightProvider.detailHeightmaps;

                            if(dh == null)
                                dh = new DetailHeightmapHeightProvider.DetailHeightmap[0];

                            for (int i = 0; i < dh.Length; i++)
                            {
                                GUILayout.Space(20f);
                                
                                dh[i].heightmapTextAsset = (TextAsset)EditorGUILayout.ObjectField("Detail Heightmap", dh[i].heightmapTextAsset, typeof(TextAsset), false);
                                dh[i].lowerLeftInBaseHeightmap = EditorGUILayout.Vector2IntField("Lower Left in Base Heightmap (pixels)", dh[i].lowerLeftInBaseHeightmap);
                                dh[i].sizeInBaseHeightmap = EditorGUILayout.Vector2IntField("Size in Base Heightmap (pixels)", dh[i].sizeInBaseHeightmap);
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Delete"))
                                {
                                    var dhNew = new DetailHeightmapHeightProvider.DetailHeightmap[dh.Length - 1];

                                    for (int j = 0; j < dhNew.Length; j++)
                                        dhNew[j] = dh[j >= i ? j + 1 : j];

                                    dh = dhNew;
                                }
                                
                            }
                            
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("Add"))
                            {
                                var dhNew = new DetailHeightmapHeightProvider.DetailHeightmap[dh.Length + 1];
                                dh.CopyTo(dhNew, 0);
                                dh = dhNew;
                            }

                            planet.serializedInherited.detailHeightmapHeightProvider.detailHeightmaps = dh;
                            EditorGUILayout.EndHorizontal();

                            break;
                    }

                    GUILayout.Space(10f);
                    planet.heightScale = EditorGUILayout.FloatField("Height Scale", planet.heightScale);
                    planet.quadsSplittingSimultaneously = EditorGUILayout.IntField(new GUIContent("Quads Splitting Simultaneously", "Number of quads that can split at the same time. Higher means shorter loading time but more CPU usage."), planet.quadsSplittingSimultaneously);
                    GUILayout.Space(10f);
                    planet.useScaledSpace = EditorGUILayout.Toggle("Use Scaled Space", planet.useScaledSpace);
                    if (planet.useScaledSpace)
                    {
                        //planet.createScaledSpaceCopy = EditorGUILayout.Toggle("Create Scaled Space Copy", planet.createScaledSpaceCopy);
                        planet.scaledSpaceFactor = EditorGUILayout.FloatField("Scaled Space Factor", planet.scaledSpaceFactor);
                        if (GUILayout.Button("Create Scaled Space Copy"))
                        {
                            planet.Initialize();
                            planet.CreateScaledSpaceCopy();
                            planet.Reset();
                        }

                    }
                    break;

                case Tab.Visual:
                    EditorGUILayout.PropertyField(planetMaterial);
                    planet.uvType = (UVType)EditorGUILayout.EnumPopup("UV Type", (System.Enum)planet.uvType);
                    if (planet.uvType == UVType.Cube)
                        planet.uvScale = EditorGUILayout.FloatField("UV Scale", planet.uvScale);
                    if (planet.useScaledSpace)
                    {
                        planet.scaledSpaceDistance = EditorGUILayout.FloatField(new GUIContent("Scaled Space Distance", "Distance at which the planet disappears and the Scaled Space copy of the planet is shown if enabled."), planet.scaledSpaceDistance);
                        if (planet.useScaledSpace)
                            EditorGUILayout.PropertyField(scaledSpaceMaterial);
                    }
                    planet.visSphereRadiusMod = EditorGUILayout.FloatField("Visibilty Sphere Radius Mod", planet.visSphereRadiusMod);
                    GUILayout.Space(5f);

                    GUILayout.Space(5f);
                    GUILayout.Label("Textures", EditorStyles.boldLabel);
                    GUILayout.Space(5f);

                    planet.slopeTextureType = (SlopeTextureType)EditorGUILayout.EnumPopup("Slope Texture Type", planet.slopeTextureType);
                    if (planet.slopeTextureType == SlopeTextureType.Threshold)
                    {
                        planet.slopeAngle = EditorGUILayout.Slider("Slope Angle", planet.slopeAngle, 0f, 90f);
                        planet.slopeTexture = EditorGUILayout.IntField(new GUIContent("Slope Texture", "Texture ID (0-5) used for slope."), planet.slopeTexture);
                    }
                    else if (planet.slopeTextureType == SlopeTextureType.Fade)
                    {
                        planet.slopeAngle = EditorGUILayout.Slider("Slope Angle", planet.slopeAngle, 0f, 90f);
                        planet.slopeFadeInAngle = EditorGUILayout.Slider("Fade-in Angle", planet.slopeFadeInAngle, 0f, 90f);
                        planet.slopeTexture = EditorGUILayout.IntField(new GUIContent("Slope Texture", "Texture ID (0-5) used for slope."), planet.slopeTexture);
                    }

                    GUILayout.Space(10f);
                    planet.serializedInherited.textureProviderType = (TextureProviderType)EditorGUILayout.EnumPopup("Texture Selection Type", planet.serializedInherited.textureProviderType);

                    if (planet.serializedInherited.textureProviderType == TextureProviderType.Gradient)
                        DrawTexProviderGradient();

                    else if (planet.serializedInherited.textureProviderType == TextureProviderType.Range)
                        DrawTexProviderRange();

                    else if (planet.serializedInherited.textureProviderType == TextureProviderType.Splatmap)
                        DrawTexProviderSplatmap();

                    break;


                case Tab.Foliage:
                    planet.generateDetails = EditorGUILayout.Toggle("Generate Details", planet.generateDetails);

                    if (planet.generateDetails)
                    {
                        planet.foliageBiomes = (Biome)EditorGUILayout.EnumFlagsField(new GUIContent("Foliage Biomes"), planet.foliageBiomes);

                        GUILayout.Space(5f);
                        planet.generateGrass = EditorGUILayout.Toggle("Generate Grass", planet.generateGrass);

                        if (planet.generateGrass)
                        {
                            planet.grassPerQuad = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Grass per Quad", "How much grass on each quad?"), planet.grassPerQuad));
                            planet.grassMaterial = (Material)EditorGUILayout.ObjectField("Grass Material", planet.grassMaterial, typeof(Material), false);
                        }
                        GUILayout.Space(5f);

                        planet.grassLevel = EditorGUILayout.IntField(new GUIContent("Detail Level", "Level at and after which details are generated"), planet.grassLevel);
                        planet.detailDistance = EditorGUILayout.FloatField(new GUIContent("Detail Distance", "Distance at which grass and meshes are generated"), planet.detailDistance);
                        planet.detailObjectsGeneratingSimultaneously = EditorGUILayout.IntField(new GUIContent("Details generating simultaneously", "How many quads can generate details at the same time."), planet.detailObjectsGeneratingSimultaneously);
                        GUILayout.Space(5f);

                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Add Mesh"))
                            planet.detailMeshes.Add(new DetailMesh());
                        if (GUILayout.Button("Add Prefab"))
                            planet.detailPrefabs.Add(new DetailPrefab());
                        EditorGUILayout.EndHorizontal();

                        for (int i = 0; i < planet.detailMeshes.Count; i++)
                        {
                            var dM = planet.detailMeshes[i];
                            if (dM.isGrass)
                                continue;

                            GUILayout.Label("Detail Mesh:", EditorStyles.boldLabel);
                            dM.number = EditorGUILayout.IntField(new GUIContent("Number", "Number of instances per quad"), dM.number);
                            dM.meshOffsetUp = EditorGUILayout.FloatField("Offset Up", dM.meshOffsetUp);
                            dM.meshScale = EditorGUILayout.Vector3Field("Scale", dM.meshScale);
                            dM.mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", dM.mesh, typeof(Mesh), false);
                            dM.material = (Material)EditorGUILayout.ObjectField("Material", dM.material, typeof(Material), false);
                            dM.useGPUInstancing = EditorGUILayout.Toggle("Use GPU Instancing", dM.useGPUInstancing);

                            planet.detailMeshes[i] = dM;

                            if (GUILayout.Button("Remove"))
                                planet.detailMeshes.RemoveAt(i);

                            GUILayout.Space(10f);
                        }

                        for (int i = 0; i < planet.detailPrefabs.Count; i++)
                        {
                            var dP = (DetailPrefab)planet.detailPrefabs[i];
                            GUILayout.Label("Detail Prefab:", EditorStyles.boldLabel);
                            dP.number = EditorGUILayout.IntField(new GUIContent("Number", "Number of instances per quad"), dP.number);
                            dP.meshOffsetUp = EditorGUILayout.FloatField("Offset Up", dP.meshOffsetUp);
                            dP.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", dP.prefab, typeof(GameObject), true);

                            if (GUILayout.Button("Remove"))
                                planet.detailPrefabs.RemoveAt(i);

                            GUILayout.Space(10f);
                        }

                        showExpFoliage = EditorGUILayout.Foldout(showExpFoliage, "Experimental");

                        if (showExpFoliage)
                        {
                            planet.expGrass = EditorGUILayout.Toggle("Use exp. backend for Grass", planet.expGrass);
                            planet.expMeshes = EditorGUILayout.Toggle("Use exp. backend for Meshes", planet.expMeshes);
                            planet.expPrefabs = EditorGUILayout.Toggle("Use exp. backend for Prefabs", planet.expPrefabs);

                            if (planet.expGrass || planet.expMeshes || planet.expPrefabs)
                                EditorGUILayout.HelpBox("Attach the Foliage Experimental script to your camera or player and assign this planet to use experimental Foliage.", MessageType.Info);
                        }

                    }
                    break;

                case Tab.Events:

                    EditorGUILayout.PropertyField(eventFinishedGeneration, new GUIContent("Finished generating Quads", "Called every time the planet has finished generating quads, not just once."));
                    GUILayout.Space(5f);
                    EditorGUILayout.PropertyField(eventScSpEntered, new GUIContent("Player entered Scaled Space"));
                    GUILayout.Space(5f);
                    EditorGUILayout.PropertyField(eventScSpLeft, new GUIContent("Player left Scaled Space"));

                    break;
                case Tab.Debug:
                    DrawDefaultInspector();
                    break;
            }
            serializedObject.ApplyModifiedProperties();

        }

        void DrawTexProviderSplatmap()
        {
            GUILayout.Space(10f);
            var sp = planet.serializedInherited.textureProviderSplatmap;
            sp.dataType = (TextureProviderSplatmap.DataType)EditorGUILayout.EnumPopup("Data Source", sp.dataType);

            sp.useBicubicInterpolation = EditorGUILayout.Toggle("Use Bicubic Interpolation", sp.useBicubicInterpolation);

            if (sp.dataType == TextureProviderSplatmap.DataType.Texture2d)
            {
                if (sp == null || sp.dataType != TextureProviderSplatmap.DataType.Texture2d)
                    sp = new TextureProviderSplatmap(TextureProviderSplatmap.DataType.Texture2d, false);

                sp.textAssets = null;
                EditorGUILayout.HelpBox("If only one Splatmap is assigned, its channels (r, g, b) correspond to the first three texture channels of a Planet Surface/Fade Shader. If both are assigned, Splatmap A's rgb-channels map to textures 0 to 2; Splatmap B's rgb-channels map to textures 3 to 5.", MessageType.Info);
                sp.textureA = (Texture2D)EditorGUILayout.ObjectField("Splatmap A", sp.textureA, typeof(Texture2D), false);
                sp.textureB = (Texture2D)EditorGUILayout.ObjectField("Splatmap B", sp.textureB, typeof(Texture2D), false);

                sp.heightmapColors = new int[] { 0, 1, 2, 3, 4, 5 };
            }
            else if (sp.dataType == TextureProviderSplatmap.DataType.TextAssets)
            {
                GUILayout.Space(10f);

                if (sp == null || sp.dataType != TextureProviderSplatmap.DataType.TextAssets)
                    sp = new TextureProviderSplatmap(TextureProviderSplatmap.DataType.TextAssets, false);

                if (sp.textAssets == null)
                    sp.textAssets = new TextAsset[1];

                if (sp.heightmapColors == null)
                    sp.heightmapColors = new int[1];

                float temp = EditorGUIUtility.labelWidth;

                for (int i = 0; i < sp.textAssets.Length; i++)
                {
                    EditorGUIUtility.labelWidth = 75f;
                    EditorGUILayout.BeginHorizontal();
                    sp.textAssets[i] = (TextAsset)EditorGUILayout.ObjectField("Splatmap", sp.textAssets[i], typeof(TextAsset), false);
                    GUILayout.Space(25f);
                    sp.heightmapColors[i] = EditorGUILayout.IntField("Channel", sp.heightmapColors[i]);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("X"))
                    {
                        TextAsset[] newTextAssets = new TextAsset[sp.textAssets.Length - 1];
                        int[] newColors = new int[sp.textAssets.Length - 1];

                        for (int j = 0; j < newTextAssets.Length; j++)
                        {
                            newColors[j] = sp.heightmapColors[j >= i ? j + 1 : j];
                            newTextAssets[j] = sp.textAssets[j >= i ? j + 1 : j];
                        }

                        sp.textAssets = newTextAssets;
                        sp.heightmapColors = newColors;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUIUtility.labelWidth = temp;
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add") && sp.textAssets.Length < 6)
                {
                    TextAsset[] newTextAssets = new TextAsset[sp.textAssets.Length + 1];
                    int[] newColors = new int[sp.heightmapColors.Length + 1];

                    sp.textAssets.CopyTo(newTextAssets, 0);
                    sp.heightmapColors.CopyTo(newColors, 0);

                    sp.textAssets = newTextAssets;
                    sp.heightmapColors = newColors;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        void DrawTexProviderGradient()
        {
            if (planet.serializedInherited.textureProviderGradient == null)
            {
                planet.serializedInherited.textureProviderGradient = new TextureProviderGradient();
            }

            if (gradientEditor == null)
            {
                gradientEditor = new GradientEditor();
                gradientEditor.gradient = planet.serializedInherited.textureProviderGradient;
                gradientEditor.colorSequence = planet.textureColorSequence;
                gradientEditor.Init();
            }

            var tg = planet.serializedInherited.textureProviderGradient;

            GUILayout.Label("Gradient", EditorStyles.label);
            gradientEditor.OnGUI(EditorGUILayout.GetControlRect());
            GUILayout.Space(70f);
            float temp = EditorGUIUtility.labelWidth;
            showColorEditor = EditorGUILayout.Foldout(showColorEditor, "Color Editor (only affects above preview, not planet)");
            EditorGUIUtility.labelWidth = 1;
            if (showColorEditor)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (i % 2 == 0) EditorGUILayout.BeginHorizontal();
                    planet.textureColorSequence[i] = EditorGUILayout.ColorField(i.ToString(), planet.textureColorSequence[i]);
                    if (i % 2 == 1) EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("Reset"))
                    planet.textureColorSequence = defaultSequence;

                gradientEditor.colorSequence = planet.textureColorSequence;
            }

            EditorGUIUtility.labelWidth = temp;
        }

        void DrawTexProviderRange()
        {
            EditorGUI.BeginChangeCheck();
            GUILayout.Space(10f);

            if (planet.serializedInherited.textureProviderRange == null)
            {
                planet.serializedInherited.textureProviderRange = new TextureProviderRange();
            }

            var tr = planet.serializedInherited.textureProviderRange;
            float temp = EditorGUIUtility.labelWidth;

            Rect position = EditorGUILayout.GetControlRect();
            position.height = 18;
            position.x += 20f;
            position.width -= 20f;

            Rect textureIdField = position;
            textureIdField.width = 50f;

            Rect vector2Field = position;
            vector2Field.x += textureIdField.width + 15f;
            vector2Field.width = 280f;

            Rect button = position;
            button.x += position.width - 25f;
            button.width = 25f;

            float space = 10f;
            for (int i = 0; i < tr.ranges.Length; i++)
            {
                EditorGUIUtility.labelWidth = 20f;
                tr.textures[i] = Mathf.Clamp(EditorGUI.IntField(textureIdField, "ID", tr.textures[i]), 0, 5);
                EditorGUIUtility.labelWidth = 45f;
                tr.ranges[i] = EditorGUI.Vector2Field(vector2Field, "Range", tr.ranges[i]);

                if (GUI.Button(button, "X"))
                {
                    Vector2[] newRanges = new Vector2[tr.ranges.Length - 1];
                    int[] newTextures = new int[tr.textures.Length - 1];

                    for (int j = 0; j < newRanges.Length; j++)
                    {
                        newRanges[j] = tr.ranges[j >= i ? j + 1 : j];
                        newTextures[j] = tr.textures[j >= i ? j + 1 : j];
                    }

                    tr.ranges = newRanges;
                    tr.textures = newTextures;
                }

                textureIdField.y += 20f;
                vector2Field.y += 20f;
                button.y += 20f;
                space += 20f;
            }

            button.x = position.x;
            button.x += position.width - 40;
            button.width = 40;

            if (GUI.Button(button, "Add"))
            {
                Vector2[] newRanges = new Vector2[tr.ranges.Length + 1];
                int[] newTextures = new int[tr.textures.Length + 1];

                tr.ranges.CopyTo(newRanges, 0);
                tr.textures.CopyTo(newTextures, 0);

                tr.ranges = newRanges;
                tr.textures = newTextures;
            }

            EditorGUIUtility.labelWidth = temp;
            GUILayout.Space(space);

            if (rangePreview == null || EditorGUI.EndChangeCheck())
            {
                rangePreview = tr.GetSampleTexture(planet.textureColorSequence);
            }

            position = EditorGUILayout.GetControlRect();
            position.height = 32f;
            GUI.DrawTexture(position, rangePreview, ScaleMode.StretchToFill, false);
            GUILayout.Space(37f);

            temp = EditorGUIUtility.labelWidth;
            showColorEditor = EditorGUILayout.Foldout(showColorEditor, "Color Editor (only affects above preview, not planet)");
            EditorGUIUtility.labelWidth = 1;
            if (showColorEditor)
            {
                EditorGUI.BeginChangeCheck();
                for (int i = 0; i < 6; i++)
                {
                    if (i % 2 == 0) EditorGUILayout.BeginHorizontal();
                    planet.textureColorSequence[i] = EditorGUILayout.ColorField(i.ToString(), planet.textureColorSequence[i]);
                    if (i % 2 == 1) EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("Reset"))
                    planet.textureColorSequence = defaultSequence;

                if (EditorGUI.EndChangeCheck())
                    rangePreview = tr.GetSampleTexture(planet.textureColorSequence);
            }

            EditorGUIUtility.labelWidth = temp;

        }

        void Heightmap()
        {
            planet.serializedInherited.heightmapHeightProvider.heightmapTextAsset = (TextAsset)EditorGUILayout.ObjectField("Heightmap", planet.serializedInherited.heightmapHeightProvider.heightmapTextAsset, typeof(TextAsset), false);
            planet.serializedInherited.heightmapHeightProvider.useBicubicInterpolation = EditorGUILayout.Toggle("Use Bicubic interpolation", planet.serializedInherited.heightmapHeightProvider.useBicubicInterpolation);
        }
        [MenuItem("GameObject/3D Object/Planet")]
        public static void CreatePlanet()
        {
            var go = new GameObject();
            go.AddComponent<Planet>().planetMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            go.name = "Planet";
        }
    }
}