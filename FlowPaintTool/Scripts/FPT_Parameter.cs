#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    using TextData = FPT_Language.FPT_ParameterText;

    public class FPT_Parameter : MonoBehaviour
    {
        private GameObject _rangeVisualization = null;

        private bool _preInputKeyTab = false;
        private bool _preInputKeyZ = false;
        private bool _preInputKeyPlus = false;
        private bool _preInputKeyMinus = false;
        private bool _preInputKeyLeftBracket = false;
        private bool _preInputKeyRightBracket = false;

        private bool _focus = false;

        private void Start()
        {
            _rangeVisualization = Instantiate(FPT_Assets.GetStaticInstance().GetRangeVisualizationPrefab());
            _rangeVisualization.transform.SetParent(transform, false);

            Camera camera = Camera.main;
            camera.nearClipPlane = Math.Min(camera.nearClipPlane, 0.01f);
            camera.gameObject.AddComponent<FPT_Camera>();
        }

        private void Update()
        {
            FPT_EditorData editorData = FPT_EditorData.GetStaticInstance();

            bool inputKeyTab = Input.GetKey(KeyCode.Tab);
            bool inputKeyZ = Input.GetKey(KeyCode.Z);
            bool inputKeyPlus = Input.GetKey(KeyCode.KeypadPlus);
            bool inputKeyMinus = Input.GetKey(KeyCode.KeypadMinus);
            bool inputKeyLeftBracket = Input.GetKey(KeyCode.LeftBracket);
            bool inputKeyRightBracket = Input.GetKey(KeyCode.RightBracket);



            float scrollDelta = Input.mouseScrollDelta.y;

            if (Input.GetKey(KeyCode.R))
            {
                editorData.ChangeBrushSize(scrollDelta);
            }

            if (Input.GetKey(KeyCode.F))
            {
                editorData.ChangeBrushStrength(scrollDelta);
            }

            if (!_preInputKeyTab && inputKeyTab)
            {
                editorData.ChangeEnableMaskMode();
            }

            if (!_preInputKeyZ && inputKeyZ)
            {
                editorData.ChangeEnableMaterialView();
            }



            FPT_Main fptMain = FPT_Main.GetActiveInstance();

            if (fptMain != null)
            {
                if (editorData.GetEnableMaskMode())
                {
                    if (!_preInputKeyPlus && inputKeyPlus)
                    {
                        fptMain.LinkedUnmask();
                    }

                    if (!_preInputKeyMinus && inputKeyMinus)
                    {
                        fptMain.LinkedMask();
                    }
                }
                else
                {
                    if (!_preInputKeyLeftBracket && inputKeyLeftBracket)
                    {
                        fptMain.RenderTextureUndo();
                    }

                    if (!_preInputKeyRightBracket && inputKeyRightBracket)
                    {
                        fptMain.RenderTextureRedo();
                    }
                }
            }



            _preInputKeyTab = inputKeyTab;
            _preInputKeyZ = inputKeyZ;
            _preInputKeyPlus = inputKeyPlus;
            _preInputKeyMinus = inputKeyMinus;
            _preInputKeyLeftBracket = inputKeyLeftBracket;
            _preInputKeyRightBracket = inputKeyRightBracket;
        }

        private void FixedUpdate()
        {
            FPT_Main fptMain = FPT_Main.GetActiveInstance();

            if (fptMain != null)
            {
                bool hit = fptMain.PaintToolRaycast(out RaycastHit raycastHit);
                _rangeVisualization.SetActive(hit);

                float scale = FPT_EditorData.GetStaticInstance().GetBrushSize() * 2f;
                Transform temp0 = _rangeVisualization.transform;
                temp0.position = raycastHit.point;
                temp0.rotation = Camera.main.transform.rotation;
                temp0.localScale = new Vector3(scale, scale, scale);
            }
            else
            {
                _rangeVisualization.SetActive(false);
            }
        }



        private void OnGUI()
        {
            if (_focus)
            {
                FPT_EditorData editorData = FPT_EditorData.GetStaticInstance();

                bool flag0 = FPT_Main.GetActiveInstance() != null;

                string[] rTextArray1 = new string[]
                {
                    TextData.Forward,
                    TextData.Backward,
                    TextData.Right,
                    TextData.Left,
                    TextData.Up,
                    TextData.Down,
                    TextData.SpeedUp
                };

                string[] lTextArray1 = new string[]
                {
                    "W",
                    "S",
                    "D",
                    "A",
                    "E",
                    "Q",
                    "Left shift"
                };

                string[] rTextArray2 = Enumerable.Repeat(string.Empty, 11).ToArray();
                string[] lTextArray2 = Enumerable.Repeat(string.Empty, 11).ToArray();

                if (flag0)
                {
                    if (!editorData.GetEnableMaskMode())
                    {
                        rTextArray2[0] = TextData.Paint;

                        rTextArray2[2] = TextData.CameraRotation;
                    }
                    else
                    {
                        rTextArray2[0] = TextData.Unmask;
                        rTextArray2[1] = TextData.Mask;
                        rTextArray2[2] = TextData.CameraRotation;
                    }

                    rTextArray2[3] = TextData.BrushSize;
                    rTextArray2[4] = TextData.BrushStrength;
                    rTextArray2[5] = TextData.MaskMode;

                    if (!editorData.GetEnableMaskMode())
                    {
                        rTextArray2[6] = TextData.PreviewMode;
                        rTextArray2[7] = TextData.Undo;
                        rTextArray2[8] = TextData.Redo;
                    }
                    else
                    {
                        rTextArray2[9] = TextData.LinkedUnmask;
                        rTextArray2[10] = TextData.LinkedMask;
                    }

                    if (!editorData.GetEnableMaskMode())
                    {
                        lTextArray2[0] = "Mouse left";

                        lTextArray2[2] = "Mouse middle";
                    }
                    else
                    {
                        lTextArray2[0] = "Mouse left";
                        lTextArray2[1] = "Mouse right";
                        lTextArray2[2] = "Mouse middle";
                    }

                    lTextArray2[3] = "R + Mouse scroll";
                    lTextArray2[4] = "F + Mouse scroll";
                    lTextArray2[5] = "Tab";

                    if (!editorData.GetEnableMaskMode())
                    {
                        lTextArray2[6] = "Z";
                        lTextArray2[7] = "[";
                        lTextArray2[8] = "]";
                    }
                    else
                    {
                        lTextArray2[9] = "KeypadPlus";
                        lTextArray2[10] = "KeypadMinus";
                    }
                }



                GUILayout.BeginArea(new Rect(10, 10, 260, Screen.height - 20));
                {
                    GUILayout.BeginVertical(FPT_GUIStyle.GetWindow());
                    {
                        if (editorData.GetOperationInstructions())
                        {
                            if (GUILayout.Button("Hide operation instructions"))
                            {
                                editorData.ChangeOperationInstructions();
                            }

                            GUILayout.Space(10);

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.BeginVertical();
                                {
                                    GUIStyle rightLabel = new GUIStyle(GUI.skin.label);
                                    rightLabel.alignment = TextAnchor.MiddleRight;

                                    foreach (string text in rTextArray1)
                                    {
                                        GUILayout.Label(text, rightLabel);
                                    }

                                    foreach (string text in rTextArray2)
                                    {
                                        GUILayout.Label(text, rightLabel);
                                    }
                                }
                                GUILayout.EndVertical();

                                GUILayout.Space(10);

                                GUILayout.BeginVertical();
                                {
                                    GUIStyle leftLabel = new GUIStyle(GUI.skin.label);
                                    leftLabel.alignment = TextAnchor.MiddleLeft;

                                    foreach (string text in lTextArray1)
                                    {
                                        GUILayout.Label(text, leftLabel);
                                    }

                                    foreach (string text in lTextArray2)
                                    {
                                        GUILayout.Label(text, leftLabel);
                                    }
                                }
                                GUILayout.EndVertical();
                            }
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            if (GUILayout.Button("View operation instructions"))
                            {
                                editorData.ChangeOperationInstructions();
                            }
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndArea();
            }
            else
            {
                GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
                {
                    GUILayout.Label("Paused", GUI.skin.box);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Paused", GUI.skin.box);
                }
                GUILayout.EndArea();
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            _focus = focus;
        }



        [CustomEditor(typeof(FPT_Parameter))]
        public class FlowPaintTool_Control_InspectorUI : Editor
        {
            public override void OnInspectorGUI()
            {
            }
        }
    }
}

#endif