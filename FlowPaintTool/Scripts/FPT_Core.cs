#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FlowPaintTool
{
    using TextData = FPT_Language.FPT_ParameterText;

    public class FPT_Core : MonoBehaviour
    {
        private static FPT_Core _instance = null;

        public static FPT_Core GetSingleton()
        {
            if(_instance == null)
            {
                GameObject go0 = new GameObject("PaintToolControl");
                _instance = go0.AddComponent<FPT_Core>();
                _instance.ManualStart();
            }

            return _instance;
        }

        private static Camera _camera = null;

        public static Camera GetCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                GameObject cameraObject = new GameObject("Camera");
                _camera = cameraObject.AddComponent<Camera>();
            }

            return _camera;
        }



        private GameObject _rangeVisualizar = null;

        private bool _preInputKeyTab = false;
        private bool _preInputKeyZ = false;
        private bool _preInputKeyPlus = false;
        private bool _preInputKeyMinus = false;
        private bool _preInputKeyLeftBracket = false;
        private bool _preInputKeyRightBracket = false;

        private bool[] _focus = new bool[3];

        private List<FPT_Main> _ftpMainList = new List<FPT_Main>();

        private FPT_EditorData _fptEditorData = null;

        public bool GetPrePreFocus() => _focus[2];

        public void GenerateFPT_Main(FPT_MainData fpt_MainData)
        {
            GameObject go1 = new GameObject("PaintTool");
            go1.transform.SetParent(Selection.activeTransform, false);

            FPT_Main fptMain = go1.AddComponent<FPT_Main>();
            fptMain.ManualStart(fpt_MainData);
            _ftpMainList.Add(fptMain);
        }

        public void ManualStart()
        {
            _fptEditorData = FPT_EditorData.GetSingleton();

            _rangeVisualizar = Instantiate(FPT_Assets.GetSingleton().GetRangeVisualizationPrefab());
            _rangeVisualizar.transform.SetParent(transform, false);

            Camera camera = GetCamera();
            camera.nearClipPlane = Math.Min(camera.nearClipPlane, 0.01f);
            camera.gameObject.AddComponent<FPT_Camera>().ManualStart();
        }

        private void Update()
        {
            bool inputKeyTab = Input.GetKey(KeyCode.Tab);
            bool inputKeyZ = Input.GetKey(KeyCode.Z);
            bool inputKeyPlus = Input.GetKey(KeyCode.KeypadPlus);
            bool inputKeyMinus = Input.GetKey(KeyCode.KeypadMinus);
            bool inputKeyLeftBracket = Input.GetKey(KeyCode.LeftBracket);
            bool inputKeyRightBracket = Input.GetKey(KeyCode.RightBracket);



            float scrollDelta = Input.mouseScrollDelta.y;

            if (Input.GetKey(KeyCode.R))
            {
                _fptEditorData.ChangeBrushSize(scrollDelta);
            }

            if (Input.GetKey(KeyCode.F))
            {
                _fptEditorData.ChangeBrushStrength(scrollDelta);
            }

            if (!_preInputKeyTab && inputKeyTab)
            {
                _fptEditorData.ChangeMaskMode();
            }

            if (!_preInputKeyZ && inputKeyZ)
            {
                _fptEditorData.ChangePreviewMode();
            }



            FPT_Main fptMain = FPT_Main.GetActiveInstance();

            if (fptMain != null)
            {
                if (_fptEditorData.GetEnableMaskMode())
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
            if (FPT_Main.GetActiveInstance() == null)
            {
                _rangeVisualizar.SetActive(false);
            }

            _focus[2] = _focus[1];
            _focus[1] = _focus[0];
        }



        private void OnGUI()
        {
            if (_focus[0])
            {
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
                    if (!_fptEditorData.GetEnableMaskMode())
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

                    if (!_fptEditorData.GetEnableMaskMode())
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

                    if (!_fptEditorData.GetEnableMaskMode())
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

                    if (!_fptEditorData.GetEnableMaskMode())
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
                        if (_fptEditorData.GetOperationInstructions())
                        {
                            if (GUILayout.Button("Hide operation instructions"))
                            {
                                _fptEditorData.ChangeOperationInstructions();
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
                                _fptEditorData.ChangeOperationInstructions();
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
            _focus[0] = focus;
        }

        public void MoveRangeVisualizar(bool hit, Vector3 point)
        {
            _rangeVisualizar.SetActive(hit);

            if (hit)
            {
                float scale = _fptEditorData.GetBrushSize() * 2f;
                Transform temp0 = _rangeVisualizar.transform;
                temp0.position = point;
                temp0.rotation = GetCamera().transform.rotation;
                temp0.localScale = new Vector3(scale, scale, scale);
            }
        }



        [CustomEditor(typeof(FPT_Core))]
        public class FlowPaintTool_Control_InspectorUI : Editor
        {
            public override void OnInspectorGUI()
            {
            }
        }
    }
}

#endif
