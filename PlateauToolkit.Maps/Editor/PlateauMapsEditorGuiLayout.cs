using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PlateauToolkit.Maps.Editor
{
    public readonly struct PlateauMapsImageButtonGUI
    {
        readonly float m_Width;
        readonly float m_Height;

        public PlateauMapsImageButtonGUI(float width, float height)
        {
            m_Width = width;
            m_Height = height;
        }

        public bool Button(string iconTexturePath, Color? buttonColor = null)
        {
            Color defaultColor = GUI.backgroundColor;
            using (PlateauMapsEditorGuiLayout.BackgroundColorScope(buttonColor.GetValueOrDefault(defaultColor)))
            {
                bool button = GUILayout.Button(
                    (Texture2D)AssetDatabase.LoadAssetAtPath(iconTexturePath, typeof(Texture2D)),
                    GUILayout.Width(m_Width),
                    GUILayout.Height(m_Height));

                return button;
            }
        }

        public bool Button(Texture2D texture2D, Color? buttonColor = null)
        {
            Color defaultColor = GUI.backgroundColor;
            using (PlateauMapsEditorGuiLayout.BackgroundColorScope(buttonColor.GetValueOrDefault(defaultColor)))
            {
                bool button = GUILayout.Button(
                    texture2D,
                    GUILayout.Width(m_Width),
                    GUILayout.Height(m_Height));

                return button;
            }
        }
    }
    public class PlateauMapsEditorGuiLayout : MonoBehaviour
    {
        /// <summary>
        /// Show GUIs with a grid layout.
        /// </summary>
        public static void GridLayout(float width, float cellWidth, float cellHeight, IEnumerable<Action> cellGuis)
        {
            Action[] cellGuiArray = cellGuis.ToArray();
            int cellsPerRow = Mathf.Max(1, Mathf.Min((int)(width / cellWidth), cellGuiArray.Length));
            int cellCount = cellGuiArray.Length % cellsPerRow == 0 ? cellGuiArray.Length : cellGuiArray.Length + (cellsPerRow - cellGuiArray.Length % cellsPerRow);

            bool isHorizontalGroupOpen = false;

            GUILayout.BeginVertical();
            {
                for (int i = 0; i < cellCount; i++)
                {
                    if (i % cellsPerRow == 0)
                    {
                        // If a horizontal group is open, end it before starting a new one.
                        if (isHorizontalGroupOpen)
                        {
                            GUILayout.FlexibleSpace(); // Center align
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace(); // Center align
                        isHorizontalGroupOpen = true;
                    }

                    // Begin a GUI group for each cell, to isolate their layouts from each other.
                    GUILayout.BeginVertical(GUILayout.Width(cellWidth), GUILayout.Height(cellHeight));
                    if (i < cellGuiArray.Length)
                    {
                        cellGuiArray[i]();
                    }
                    else
                    {
                        GUILayout.Label("");
                    }
                    GUILayout.EndVertical();
                }

                // If a horizontal group is open at the end of the loop, end it.
                if (isHorizontalGroupOpen)
                {
                    // Add empty cells to fill the row if necessary
                    for (int j = cellGuiArray.Length; j % cellsPerRow != 0; j++)
                    {
                        GUILayout.BeginVertical(GUILayout.Width(cellWidth), GUILayout.Height(cellHeight));
                        GUILayout.EndVertical();
                    }

                    GUILayout.FlexibleSpace(); // Center align
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
        }

        public class CallbackDisposable : IDisposable
        {
            readonly Action m_OnDispose;

            public CallbackDisposable(Action onDispose)
            {
                m_OnDispose = onDispose;
            }

            public void Dispose()
            {
                m_OnDispose.Invoke();
            }
        }

        public static IDisposable BackgroundColorScope(Color color)
        {
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = color;

            return new CallbackDisposable(
                () =>
                {
                    GUI.backgroundColor = defaultColor;
                });
        }

        public static void BorderLine()
        {
            Rect borderRect = EditorGUILayout.GetControlRect(false, 1, PlateauMapsGUIStyles.BorderStyle);
            EditorGUI.DrawRect(borderRect, PlateauMapsGUIStyles.k_LineColor);
        }

        public static void Header(string label)
        {
            EditorGUILayout.Space(8);

            BorderLine();

            using (var scope = new EditorGUILayout.VerticalScope(PlateauMapsGUIStyles.HeaderBoxStyle, GUILayout.Height(24)))
            {
                EditorGUI.DrawRect(scope.rect, PlateauMapsGUIStyles.k_HeaderBackgroundColor);

                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope(PlateauMapsGUIStyles.HeaderContentStyle))
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(label, GUILayout.ExpandWidth(false));
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
            }

            BorderLine();

            EditorGUILayout.Space(8);
        }

        public static EditorGUILayout.VerticalScope FooterScope()
        {
            BorderLine();

            var scope = new EditorGUILayout.VerticalScope(PlateauMapsGUIStyles.FooterBoxStyle, GUILayout.Height(24));
            EditorGUI.DrawRect(scope.rect, PlateauMapsGUIStyles.k_FooterBackgroundColor);

            return scope;
        }

        public static class PlateauMapsGUIStyles
        {
            public static readonly Color k_LineColor = new Color(40 / 255f, 40 / 255f, 40 / 255f, 1);
            public static readonly Color k_HeaderBackgroundColor = new Color(51 / 255f, 51 / 255f, 51 / 255f, 1);
            public static readonly Color k_FooterBackgroundColor = new Color(51 / 255f, 51 / 255f, 51 / 255f, 1);

            public static GUIStyle BorderStyle { get; }
            public static GUIStyle HeaderBoxStyle { get; }
            public static GUIStyle HeaderContentStyle { get; }
            public static GUIStyle FooterBoxStyle { get; }

            static PlateauMapsGUIStyles()
            {
                BorderStyle = new GUIStyle(GUIStyle.none);
                BorderStyle.normal.textColor = Color.white;
                BorderStyle.alignment = TextAnchor.MiddleCenter;
                BorderStyle.margin = new RectOffset(0, 0, 0, 0);
                BorderStyle.padding = new RectOffset(0, 0, 0, 0);

                FooterBoxStyle = new GUIStyle(GUI.skin.box);
                FooterBoxStyle.normal.textColor = Color.white;
                FooterBoxStyle.alignment = TextAnchor.MiddleCenter;
                FooterBoxStyle.margin = new RectOffset(0, 0, 0, 0);
                FooterBoxStyle.padding = new RectOffset(0, 0, 0, 0);

                HeaderBoxStyle = new GUIStyle(FooterBoxStyle);

                HeaderContentStyle = new GUIStyle(GUI.skin.box);
                HeaderContentStyle.normal.textColor = Color.white;
                HeaderContentStyle.alignment = TextAnchor.MiddleCenter;
                HeaderContentStyle.margin = new RectOffset(0, 0, 0, 0);
                HeaderContentStyle.padding = new RectOffset(5, 5, 5, 5);
            }
        }

    }
}
