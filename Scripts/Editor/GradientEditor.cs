//Based on Unity C# reference source

using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor;
using PlanetaryTerrain;

namespace PlanetaryTerrain.EditorUtils
{
    public class GradientEditor
    {
        class Styles
        {
            public GUIStyle upSwatch = "Grad Up Swatch";
            public GUIStyle upSwatchOverlay = "Grad Up Swatch Overlay";
            public GUIStyle downSwatch = "Grad Down Swatch";
            public GUIStyle downSwatchOverlay = "Grad Down Swatch Overlay";
            public GUIContent locationText = EditorGUIUtility.TrTextContent("Location");
        }

        public class Swatch
        {
            public float height;
            public int id;

            public Swatch(float height, int id)
            {
                this.height = height;
                this.id = id;
            }
        }

        static Styles s_Styles;
        const int maxNumKeys = 32;
        List<Swatch> swatches;
        [System.NonSerialized]
        Swatch selectedSwatch;
        public TextureProviderGradient gradient;
        public Color32[] colorSequence;

        public void Init()
        {
            BuildArray();

            if (swatches.Count > 0)
                selectedSwatch = swatches[0];
        }

        void BuildArray()
        {
            swatches = new List<Swatch>();
            for (int i = 0; i < gradient.heights.Length; i++)
            {
                swatches.Add(new Swatch(gradient.heights[i], gradient.ids[i]));
            }
        }

        int SwatchSort(Swatch lhs, Swatch rhs)
        {
            if (lhs.height == rhs.height && lhs == selectedSwatch)
                return -1;
            if (lhs.height == rhs.height && rhs == selectedSwatch)
                return 1;

            return lhs.height.CompareTo(rhs.height);
        }

        void AssignBack()
        {
            swatches.Sort((a, b) => SwatchSort(a, b));

            int len = swatches.Count;

            float[] heights = new float[len];
            int[] ids = new int[len];

            for (int i = 0; i < len; i++)
            {
                heights[i] = swatches[i].height;
                ids[i] = swatches[i].id;
            }

            gradient.heights = heights;
            gradient.ids = ids;
        }

        public void DrawGradient(Rect position, TextureProviderGradient gradient)
        {
            Texture2D gradientTexture = gradient.GetSampleTexture(colorSequence);
            GUI.DrawTexture(position, gradientTexture, ScaleMode.StretchToFill, false);
        }

        public void OnGUI(Rect position)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            float swatchHeight = 16f;
            float editSectionHeight = 26f;
            float gradientTextureHeight = 32f;

            position.height = swatchHeight;
            ShowSwatchArray(position, swatches);
            position.y += swatchHeight;

            if (Event.current.type == EventType.Repaint)
            {
                position.height = gradientTextureHeight;
                DrawGradient(position, gradient);
            }

            if (selectedSwatch != null)
            {
                position.y += gradientTextureHeight + 5f;
                position.height = editSectionHeight;

                float alphaOrColorTextWidth = 72;

                Rect rect = position;
                rect.height = 18;

                Rect idTextRect = rect;
                idTextRect.x += 17;
                idTextRect.width = 128;

                Rect heightTextRect = rect;
                heightTextRect.x += rect.width - 150;
                heightTextRect.width = 128;

                float temp = EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = alphaOrColorTextWidth;

                EditorGUI.BeginChangeCheck();

                selectedSwatch.id = Mathf.Clamp(EditorGUI.IntField(idTextRect, "Texture ID", selectedSwatch.id), 0, 5);
                if (EditorGUI.EndChangeCheck())
                {
                    AssignBack();
                    HandleUtility.Repaint();
                }

                EditorGUI.BeginChangeCheck();
                float newLocation = Mathf.Clamp01(EditorGUI.FloatField(heightTextRect, s_Styles.locationText, selectedSwatch.height));
                if (EditorGUI.EndChangeCheck())
                {
                    selectedSwatch.height = Mathf.Clamp(newLocation, 0f, 1f);
                    AssignBack();
                }

                EditorGUIUtility.labelWidth = temp;
            }
        }

        void DrawSwatch(Rect totalPos, Swatch s, bool upwards = false)
        {
            Color temp = GUI.backgroundColor;
            Rect r = CalcSwatchRect(totalPos, s);

            GUI.backgroundColor = gradient.EvaluateColor(s.height, colorSequence);

            GUIStyle back = upwards ? s_Styles.upSwatch : s_Styles.downSwatch;
            GUIStyle overlay = upwards ? s_Styles.upSwatchOverlay : s_Styles.downSwatchOverlay;
            back.Draw(r, false, false, selectedSwatch == s, false);
            GUI.backgroundColor = temp;
            overlay.Draw(r, false, false, selectedSwatch == s, false);
        }

        Rect CalcSwatchRect(Rect totalRect, Swatch s)
        {
            float time = s.height;
            return new Rect(totalRect.x + Mathf.Round(totalRect.width * time) - 5, totalRect.y, 10, totalRect.height);
        }

        void RemoveDuplicateOverlappingSwatches()
        {
            bool didRemoveAny = false;
            for (int i = 1; i < swatches.Count; i++)
            {
                if (Mathf.Approximately(swatches[i - 1].height, swatches[i].height))
                {
                    swatches.RemoveAt(i);
                    i--;
                    didRemoveAny = true;
                }
            }

            if (didRemoveAny)
                AssignBack();
        }

        void ShowSwatchArray(Rect position, List<Swatch> swatches)
        {
            int id = GUIUtility.GetControlID(652347689, FocusType.Passive);
            Event evt = Event.current;

            float mouseSwatchTime = Mathf.Clamp01((Event.current.mousePosition.x - position.x) / position.width);
            Vector2 fixedStepMousePosition = new Vector3(position.x + mouseSwatchTime * position.width, Event.current.mousePosition.y);

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                    {
                        bool hasSelection = false;
                        foreach (Swatch s in swatches)
                        {
                            if (selectedSwatch == s)
                            {
                                hasSelection = true;
                                continue;
                            }
                            DrawSwatch(position, s);
                        }

                        if (hasSelection && selectedSwatch != null)
                            DrawSwatch(position, selectedSwatch);
                        break;
                    }
                case EventType.MouseDown:
                    {
                        Rect clickRect = position;

                        clickRect.xMin -= 10;
                        clickRect.xMax += 10;
                        if (clickRect.Contains(evt.mousePosition))
                        {
                            GUIUtility.hotControl = id;
                            evt.Use();

                            bool found = false;
                            foreach (Swatch s in swatches)
                            {
                                if (CalcSwatchRect(position, s).Contains(fixedStepMousePosition))
                                {
                                    found = true;
                                    selectedSwatch = s;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                if (swatches.Count < maxNumKeys)
                                {
                                    selectedSwatch = new Swatch(mouseSwatchTime, 0);
                                    swatches.Add(selectedSwatch);
                                    AssignBack();
                                }
                                else
                                {
                                    Debug.LogWarning("Max " + maxNumKeys + " keys are allowed in a texture gradient.");
                                }
                            }
                        }
                        break;
                    }
                case EventType.MouseDrag:

                    if (GUIUtility.hotControl == id && selectedSwatch != null)
                    {
                        evt.Use();

                        if ((evt.mousePosition.y + 5 < position.y || evt.mousePosition.y - 5 > position.yMax))
                        {
                            if (swatches.Count > 1)
                            {
                                swatches.Remove(selectedSwatch);
                                AssignBack();
                                break;
                            }
                        }
                        else if (!swatches.Contains(selectedSwatch))
                            swatches.Add(selectedSwatch);

                        selectedSwatch.height = mouseSwatchTime;
                        AssignBack();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        if (!swatches.Contains(selectedSwatch))
                            selectedSwatch = null;

                        RemoveDuplicateOverlappingSwatches();
                    }
                    break;

                case EventType.KeyDown:
                    if (evt.keyCode == KeyCode.Delete)
                    {
                        if (selectedSwatch != null)
                        {
                            if (swatches.Count > 1)
                            {
                                swatches.Remove(selectedSwatch);
                                AssignBack();
                                HandleUtility.Repaint();
                            }
                        }
                        evt.Use();
                    }
                    else if (evt.keyCode == KeyCode.LeftArrow)
                    {
                        if (selectedSwatch != null)
                        {
                            int index = swatches.IndexOf(selectedSwatch);

                            if (index > 0 && 0 <= --index)
                            {
                                selectedSwatch = swatches[index];
                            }
                        }
                        evt.Use();
                    }
                    else if (evt.keyCode == KeyCode.RightArrow)
                    {
                        if (selectedSwatch != null)
                        {
                            int index = swatches.IndexOf(selectedSwatch);

                            if (index >= 0 && swatches.Count > ++index)
                            {
                                selectedSwatch = swatches[index];
                            }
                        }
                        evt.Use();
                    }
                    break;

                case EventType.ValidateCommand:
                    if (evt.commandName == "Delete")
                        Event.current.Use();
                    break;

                case EventType.ExecuteCommand:
                    if (evt.commandName == "Delete")
                    {
                        if (swatches.Count > 1)
                        {
                            swatches.Remove(selectedSwatch);
                            AssignBack();
                            HandleUtility.Repaint();
                        }
                    }
                    break;
            }

        }

    }
}
