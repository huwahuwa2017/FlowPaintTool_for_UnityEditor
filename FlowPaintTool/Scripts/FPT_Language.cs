#if UNITY_EDITOR

using UnityEditor;

namespace FlowPaintTool
{
    public static class FPT_Language
    {
        private static FPT_LanguageTypeEnum _language = FPT_LanguageTypeEnum.None;

        public static class FPT_EditorWindowText
        {
            public static string CheckGitHub;
            public static string PleaseSelectOnlyOneGameObject;
            public static string ThePaintToolIsReady;

            public static void ChangeLanguage(FPT_LanguageTypeEnum languageType)
            {
                if (languageType == FPT_LanguageTypeEnum.Japanese)
                {
                    CheckGitHub = "GitHubを確認する";
                    PleaseSelectOnlyOneGameObject = "ゲームオブジェクトを一つ選択してください";
                    ThePaintToolIsReady = "ペイントツールの準備完了";
                }
                else if (languageType == FPT_LanguageTypeEnum.English)
                {
                    CheckGitHub = "Check GitHub";
                    PleaseSelectOnlyOneGameObject = "Please select only one GameObject";
                    ThePaintToolIsReady = "The paint tool is ready";
                }
            }
        }

        public static class FPT_ShaderProcessText
        {
            public static string RenderTextureForPreview;
            public static string Undo;
            public static string Redo;
            public static string OutputPNGFile;
            public static string OutputPath;

            public static void ChangeLanguage(FPT_LanguageTypeEnum languageType)
            {
                if (languageType == FPT_LanguageTypeEnum.Japanese)
                {
                    RenderTextureForPreview = "プレビュー用レンダーテクスチャ";
                    Undo = "Undo";
                    Redo = "Redo";
                    OutputPNGFile = "PNGファイルを出力";
                    OutputPath = "出力先 : ";
                }
                else if (languageType == FPT_LanguageTypeEnum.English)
                {
                    RenderTextureForPreview = "RenderTexture for preview";
                    Undo = "Undo";
                    Redo = "Redo";
                    OutputPNGFile = "Output PNG file";
                    OutputPath = "Output path : ";
                }
            }
        }

        public static class FPT_MeshProcessText
        {
            // MeshProcessGUI Start
            public static string LinkedMask;
            public static string LinkedUnmask;
            public static string MaskAll;
            public static string UnmaskAll;
            public static string InvertAll;
            public static string SubMeshIndex;
            public static string Mask;
            public static string Unmask;
            public static string Invert;
            // MeshProcessGUI End

            public static void ChangeLanguage(FPT_LanguageTypeEnum languageType)
            {
                if (languageType == FPT_LanguageTypeEnum.Japanese)
                {
                    LinkedMask = "接続 マスク";
                    LinkedUnmask = "接続 アンマスク";
                    MaskAll = "すべてマスク";
                    UnmaskAll = "すべてアンマスク";
                    InvertAll = "すべて反転";
                    SubMeshIndex = "サブメッシュ インデックス : ";
                    Mask = "マスク";
                    Unmask = "アンマスク";
                    Invert = "反転";
                }
                else if (languageType == FPT_LanguageTypeEnum.English)
                {
                    LinkedMask = "Linked mask";
                    LinkedUnmask = "Linked unmask";
                    MaskAll = "Mask all";
                    UnmaskAll = "Unmask all";
                    InvertAll = "Invert all";
                    SubMeshIndex = "Sub Mesh index : ";
                    Mask = "Mask";
                    Unmask = "Unmask";
                    Invert = "Invert";
                }
            }
        }

        public static class FPT_MainDataText
        {
            // ErrorCheckGUI Start
            public static string UsingSRGBTexturesInFlowPaintModeWillNot;
            public static string SelectGameObjectThatUsesMeshRendererOrSkinnedMeshRenderer;
            public static string MeshNotFound;
            public static string PleaseAllowReadWriteForTheMesh;
            public static string UVCoordinateDoesNotExistInUVchannel;
            // ErrorCheckGUI End

            // EditorWindowGUI Start
            public static string PaintMode;
            public static string WidthOfTextureCreated;
            public static string HeightOfTextureCreated;
            public static string TypeOfStartingTexture;
            public static string StartingTexture;
            public static string OpenFilePanel;
            public static string FilePath;
            public static string SRGBColorTexture;
            public static string AdvancedSettings;
            public static string TargetUVChannel;
            public static string BleedRange;
            public static string UVEpsilon;
            public static string MaxUndoCount;
            public static string StartThePaintTool;
            // EditorWindowGUI End

            public static void ChangeLanguage(FPT_LanguageTypeEnum languageType)
            {
                if (languageType == FPT_LanguageTypeEnum.Japanese)
                {
                    UsingSRGBTexturesInFlowPaintModeWillNot = "sRGBテクスチャをフローペイントモードで使用できません\nsRGBをオフにしてください";
                    SelectGameObjectThatUsesMeshRendererOrSkinnedMeshRenderer = "MeshRendererかSkinnedMeshRendererを使用しているゲームオブジェクトを選択してください";
                    MeshNotFound = "Meshが見つかりません\nMeshを正しく割り当てているか確認してください";
                    PleaseAllowReadWriteForTheMesh = "MeshのRead/Writeを有効にしてください";
                    UVCoordinateDoesNotExistInUVchannel = "このUVchannelは空です : ";

                    PaintMode = "ペイントモード";
                    WidthOfTextureCreated = "作成するテクスチャの幅";
                    HeightOfTextureCreated = "作成するテクスチャの高さ";
                    TypeOfStartingTexture = "開始時のテクスチャの種類";
                    StartingTexture = "開始時のテクスチャ";
                    OpenFilePanel = "ファイル選択画面を開く";
                    FilePath = "ファイルパス";
                    SRGBColorTexture = "sRGB (カラーテクスチャ)";
                    AdvancedSettings = "高度な設定";
                    TargetUVChannel = "使用する UVチャンネル";
                    BleedRange = "にじみの範囲";
                    UVEpsilon = "UV epsilon";
                    MaxUndoCount = "最大Undo回数";
                    StartThePaintTool = "ペイントツールを開始";
                }
                else if (languageType == FPT_LanguageTypeEnum.English)
                {
                    UsingSRGBTexturesInFlowPaintModeWillNot = "Using sRGB textures in FlowPaintMode will not give accurate results\nPlease turn off sRGB";
                    SelectGameObjectThatUsesMeshRendererOrSkinnedMeshRenderer = "Select GameObject that uses MeshRenderer or SkinnedMeshRenderer";
                    MeshNotFound = "Mesh not found\nMake sure you are assigning Mesh correctly";
                    PleaseAllowReadWriteForTheMesh = "Please allow Read/Write for the mesh";
                    UVCoordinateDoesNotExistInUVchannel = "UV coordinate does not exist in UVchannel ";

                    PaintMode = "Paint mode";
                    WidthOfTextureCreated = "Width of texture created";
                    HeightOfTextureCreated = "Height of texture created";
                    TypeOfStartingTexture = "Type of starting texture";
                    StartingTexture = "Starting texture";
                    OpenFilePanel = "Open file panel";
                    FilePath = "File path";
                    SRGBColorTexture = "sRGB (Color Texture)";
                    AdvancedSettings = "Advanced settings";
                    TargetUVChannel = "Target UV channel";
                    BleedRange = "Bleed range";
                    UVEpsilon = "UV epsilon";
                    MaxUndoCount = "Max undo count";
                    StartThePaintTool = "Start the paint tool";
                }
            }
        }

        public static class FPT_EditorDataText
        {
            // CommonGUI Start
            public static string MaskMode;
            public static string PreviewMode;
            public static string CameraSettings;
            public static string RotateSpeed;
            public static string MoveSpeed;
            public static string Inertia;
            public static string BrushSettings;
            public static string Size;
            public static string Strength;
            public static string Shape;
            // CommonGUI End

            // FlowPaintGUI Start
            public static string FlowPaintSettings;
            public static string HeightLimit;
            public static string MinimumHeight;
            public static string MaximumHeight;
            public static string FixedDirection;
            public static string FixedDirectionVector;
            public static string DisplayNormalLength;
            public static string DisplayNormalAmount;
            // FlowPaintGUI End

            // ColorPaintGUI Start
            public static string ColorPaintSettings;
            public static string Color;
            public static string SelectColorToEdit;
            public static string R;
            public static string G;
            public static string B;
            public static string A;
            // ColorPaintGUI End

            public static void ChangeLanguage(FPT_LanguageTypeEnum languageType)
            {
                if (languageType == FPT_LanguageTypeEnum.Japanese)
                {
                    MaskMode = "マスク モード";
                    PreviewMode = "プレビュー モード";
                    CameraSettings = "カメラ設定";
                    RotateSpeed = "回転速度";
                    MoveSpeed = "移動速度";
                    Inertia = "慣性";
                    BrushSettings = "ブラシ設定";
                    Size = "大きさ";
                    Strength = "強さ";
                    Shape = "形状";

                    FlowPaintSettings = "フローペイント設定";
                    HeightLimit = "高さの制限";
                    MinimumHeight = "高さの下限";
                    MaximumHeight = "高さの上限";
                    FixedDirection = "方向の固定";
                    FixedDirectionVector = "方向ベクトル";
                    DisplayNormalLength = "表示する法線の長さ";
                    DisplayNormalAmount = "表示する法線の量";

                    ColorPaintSettings = "カラーペイント設定";
                    Color = "色";
                    SelectColorToEdit = "編集する色の選択";
                    R = "R";
                    G = "G";
                    B = "B";
                    A = "A";
                }
                else if (languageType == FPT_LanguageTypeEnum.English)
                {
                    MaskMode = "Mask mode";
                    PreviewMode = "Preview mode";
                    CameraSettings = "Camera settings";
                    RotateSpeed = "Rotate speed";
                    MoveSpeed = "Move speed";
                    Inertia = "Inertia";
                    BrushSettings = "Brush settings";
                    Size = "Size";
                    Strength = "Strength";
                    Shape = "Shape";

                    FlowPaintSettings = "Flow paint settings";
                    HeightLimit = "Height limit";
                    MinimumHeight = "Minimum height";
                    MaximumHeight = "Maximum height";
                    FixedDirection = "Fixed direction";
                    FixedDirectionVector = "Direction vector";
                    DisplayNormalLength = "Display normal length";
                    DisplayNormalAmount = "Display normal amount";

                    ColorPaintSettings = "Color paint settings";
                    Color = "Color";
                    SelectColorToEdit = "Select color to edit";
                    R = "R";
                    G = "G";
                    B = "B";
                    A = "A";
                }
            }
        }

        public static class FPT_ParameterText
        {
            public static string Forward;
            public static string Backward;
            public static string Right;
            public static string Left;
            public static string Up;
            public static string Down;
            public static string SpeedUp;

            public static string Paint;
            public static string CameraRotation;
            public static string Unmask;
            public static string Mask;
            public static string BrushSize;
            public static string BrushStrength;
            public static string MaskMode;
            public static string PreviewMode;
            public static string Undo;
            public static string Redo;
            public static string LinkedUnmask;
            public static string LinkedMask;

            public static void ChangeLanguage(FPT_LanguageTypeEnum languageType)
            {
                if (languageType == FPT_LanguageTypeEnum.Japanese)
                {
                    Forward = "前";
                    Backward = "後";
                    Right = "右";
                    Left = "左";
                    Up = "上";
                    Down = "下";
                    SpeedUp = "高速移動";

                    Paint = "ペイント";
                    CameraRotation = "カメラ回転";
                    Unmask = "アンマスク";
                    Mask = "マスク";
                    BrushSize = "ブラシの大きさ";
                    BrushStrength = "ブラシの強さ";
                    MaskMode = "マスク モード";
                    PreviewMode = "プレビュー モード";
                    Undo = "Undo";
                    Redo = "Redo";
                    LinkedUnmask = "接続 アンマスク";
                    LinkedMask = "接続 マスク";
                }
                else if (languageType == FPT_LanguageTypeEnum.English)
                {
                    Forward = "Forward";
                    Backward = "Backward";
                    Right = "Right";
                    Left = "Left";
                    Up = "Up";
                    Down = "Down";
                    SpeedUp = "Speed up";

                    Paint = "Paint";
                    CameraRotation = "Camera rotation";
                    Unmask = "Unmask";
                    Mask = "Mask";
                    BrushSize = "Brush size";
                    BrushStrength = "Brush strength";
                    MaskMode = "Mask mode";
                    PreviewMode = "Preview mode";
                    Undo = "Undo";
                    Redo = "Redo";
                    LinkedUnmask = "Linked unmask";
                    LinkedMask = "Linked mask";
                }
            }
        }

        public static void ChangeLanguage(FPT_LanguageTypeEnum languageType)
        {
            if (languageType == _language) return;

            _language = languageType;

            FPT_EditorWindowText.ChangeLanguage(_language);
            FPT_ShaderProcessText.ChangeLanguage(_language);
            FPT_MeshProcessText.ChangeLanguage(_language);
            FPT_MainDataText.ChangeLanguage(_language);
            FPT_EditorDataText.ChangeLanguage(_language);
            FPT_ParameterText.ChangeLanguage(_language);

            Menu.SetChecked(FPT_EditorWindow.MenuPathJapanese, _language == FPT_LanguageTypeEnum.Japanese);
            Menu.SetChecked(FPT_EditorWindow.MenuPathEnglish, _language == FPT_LanguageTypeEnum.English);

            FPT_EditorWindow.RepaintInspectorWindow();
        }
    }
}

#endif