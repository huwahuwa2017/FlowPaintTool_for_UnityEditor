#if UNITY_EDITOR

using UnityEditor;

namespace FlowPaintTool
{
    public static class FPT_Language
    {
        private static FPT_LanguageTypeEnum _language = FPT_LanguageTypeEnum.None;

        public static class FPT_EditorWindowText
        {
            public static string CheckReleases;
            public static string CheckManual;
            public static string PleaseSelectOnlyOneGameObject;
            public static string ThePaintToolIsReady;

            public static void ChangeLanguage(FPT_LanguageTypeEnum languageType)
            {
                if (languageType == FPT_LanguageTypeEnum.Japanese)
                {
                    CheckReleases = "GitHubのReleasesを確認する";
                    CheckManual = "説明書を確認する";
                    PleaseSelectOnlyOneGameObject = "ゲーム オブジェクトを一つ選択してください";
                    ThePaintToolIsReady = "ペイント ツールの準備完了";
                }
                else if (languageType == FPT_LanguageTypeEnum.English)
                {
                    CheckReleases = "Check Releases on GitHub";
                    CheckManual = "Check manual";
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

            public static void ChangeLanguage(FPT_LanguageTypeEnum languageType)
            {
                if (languageType == FPT_LanguageTypeEnum.Japanese)
                {
                    RenderTextureForPreview = "プレビュー用レンダー テクスチャ";
                    Undo = "Undo";
                    Redo = "Redo";
                    OutputPNGFile = "PNGファイルを出力";
                }
                else if (languageType == FPT_LanguageTypeEnum.English)
                {
                    RenderTextureForPreview = "RenderTexture for preview";
                    Undo = "Undo";
                    Redo = "Redo";
                    OutputPNGFile = "Output PNG file";
                }
            }
        }

        public static class FPT_MaskText
        {
            // MeshProcessGUI Start
            public static string MaskLinked;
            public static string UnmaskLinked;
            public static string MaskAll;
            public static string UnmaskAll;
            public static string InvertMasked;
            public static string Mask;
            public static string Unmask;
            public static string Invert;
            // MeshProcessGUI End

            public static void ChangeLanguage(FPT_LanguageTypeEnum languageType)
            {
                if (languageType == FPT_LanguageTypeEnum.Japanese)
                {
                    MaskLinked = "接続マスク";
                    UnmaskLinked = "接続アンマスク";
                    MaskAll = "すべてマスク";
                    UnmaskAll = "すべてアンマスク";
                    InvertMasked = "マスクを反転";
                    Mask = "マスク";
                    Unmask = "アンマスク";
                    Invert = "反転";
                }
                else if (languageType == FPT_LanguageTypeEnum.English)
                {
                    MaskLinked = "Mask linked";
                    UnmaskLinked = "Unmask linked";
                    MaskAll = "Mask all";
                    UnmaskAll = "Unmask all";
                    InvertMasked = "Invert masked";
                    Mask = "Mask";
                    Unmask = "Unmask";
                    Invert = "Invert";
                }
            }
        }

        public static class FPT_MainDataText
        {
            // ErrorCheckGUI Start
            public static string IfYouWantToStartThePaintToolWithoutAStartingTexture;
            public static string UsingSRGBTexturesInFlowPaintModeWillNot;
            public static string SelectGameObjectThatUsesMeshRendererOrSkinnedMeshRenderer;
            public static string MeshNotFound;
            public static string PleaseAllowReadWriteForTheMesh;
            public static string ThisSubmeshDoesNotExist;
            public static string UVCoordinateDoesNotExistInUVchannel;
            public static string UnityDoesNotSupportImportingImagesIn;
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
            public static string TargetSubmeshIndex;
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
                    IfYouWantToStartThePaintToolWithoutAStartingTexture = "開始時のテクスチャを使用せずにペイントツールを開始したい場合は、 開始時のテクスチャの種類をFilePathに変更してください";
                    UsingSRGBTexturesInFlowPaintModeWillNot = "sRGBテクスチャをフロー ペイント モードで使用できません\nsRGBをオフにしてください";
                    SelectGameObjectThatUsesMeshRendererOrSkinnedMeshRenderer = "MeshRendererかSkinnedMeshRendererを使用しているゲーム オブジェクトを選択してください";
                    MeshNotFound = "Meshが見つかりません\nMeshを正しく割り当てているか確認してください";
                    PleaseAllowReadWriteForTheMesh = "MeshのRead/Writeを有効にしてください";
                    ThisSubmeshDoesNotExist = "このsubmeshは存在しません : ";
                    UVCoordinateDoesNotExistInUVchannel = "このUVチャンネルは空です : ";
                    UnityDoesNotSupportImportingImagesIn = "Unityは幅あるいは高さが8192を超えるPNG形式の画像のインポートをサポートしていません";

                    PaintMode = "ペイント モード";
                    WidthOfTextureCreated = "作成するテクスチャの幅";
                    HeightOfTextureCreated = "作成するテクスチャの高さ";
                    TypeOfStartingTexture = "開始時のテクスチャの種類";
                    StartingTexture = "開始時のテクスチャ";
                    OpenFilePanel = "ファイル選択画面を開く";
                    FilePath = "ファイル パス";
                    SRGBColorTexture = "sRGB (カラー テクスチャ)";
                    AdvancedSettings = "高度な設定";
                    TargetSubmeshIndex = "使用するsubmesh";
                    TargetUVChannel = "使用するUVチャンネル";
                    BleedRange = "にじみの範囲";
                    UVEpsilon = "UV epsilon";
                    MaxUndoCount = "最大Undo回数";
                    StartThePaintTool = "ペイント ツールを開始";
                }
                else if (languageType == FPT_LanguageTypeEnum.English)
                {
                    IfYouWantToStartThePaintToolWithoutAStartingTexture = "If you want to start the paint tool without a starting texture, change the starting texture type to FilePath";
                    UsingSRGBTexturesInFlowPaintModeWillNot = "Using sRGB textures in FlowPaintMode will not give accurate results\nPlease turn off sRGB";
                    SelectGameObjectThatUsesMeshRendererOrSkinnedMeshRenderer = "Select GameObject that uses MeshRenderer or SkinnedMeshRenderer";
                    MeshNotFound = "Mesh not found\nMake sure you are assigning Mesh correctly";
                    PleaseAllowReadWriteForTheMesh = "Please allow Read/Write for the mesh";
                    ThisSubmeshDoesNotExist = "This submesh does not exist : ";
                    UVCoordinateDoesNotExistInUVchannel = "UV coordinate does not exist in UVchannel : ";
                    UnityDoesNotSupportImportingImagesIn = "Unity does not support importing images in PNG format with a width or height greater than 8192";

                    PaintMode = "Paint mode";
                    WidthOfTextureCreated = "Width of texture created";
                    HeightOfTextureCreated = "Height of texture created";
                    TypeOfStartingTexture = "Type of starting texture";
                    StartingTexture = "Starting texture";
                    OpenFilePanel = "Open file panel";
                    FilePath = "File path";
                    SRGBColorTexture = "sRGB (Color Texture)";
                    AdvancedSettings = "Advanced settings";
                    TargetSubmeshIndex = "Target submesh index";
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
            public static string InputGazeVector;
            public static string Flip;
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

                    FlowPaintSettings = "フロー ペイント設定";
                    HeightLimit = "高さの制限";
                    MinimumHeight = "高さの下限";
                    MaximumHeight = "高さの上限";
                    FixedDirection = "方向の固定";
                    FixedDirectionVector = "方向ベクトル";
                    InputGazeVector = "視線ベクトルを入力";
                    Flip = "反転";
                    DisplayNormalLength = "表示する法線の長さ";
                    DisplayNormalAmount = "表示する法線の量";

                    ColorPaintSettings = "カラー ペイント設定";
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
                    InputGazeVector = "Input gaze vector";
                    Flip = "Flip";
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
                    LinkedUnmask = "接続アンマスク";
                    LinkedMask = "接続マスク";
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
            FPT_MaskText.ChangeLanguage(_language);
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