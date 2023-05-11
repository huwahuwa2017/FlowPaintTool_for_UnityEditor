# FlowPaintTool_for_UnityEditor

### 準備
Unity 2019.4.31f1 (3D Built-in Render Pipeline)  
あるいは  
VCC 2.1.1 (Avatar Project)  
でプロジェクトを用意して起動してください

[Releases](https://github.com/huwahuwa2017/FlowPaintTool_for_UnityEditor/releases)からunitypackageをダウンロードしてプロジェクトにインポートしてください

対象となる3Dモデルのインポート設定のRead/Write有効にして、Hierarcyへ配置してください

### 使い方
1. UnityEditorの画面上部にある「FlowPaintTool」をクリックして「Open」をクリック  
![image](/Images/JP0.png)  
1. UnityEditorのPlayModeを開始  
1. Hierarcyへ配置した3Dモデルを表示している（MeshRendererかSkinnedMeshRendererを使用している）ゲームオブジェクトを選択  
1. FlowPaintTool window下部にある「ペイントツールを開始」ボタンを押す  
![image](/Images/JP1.png)  
これにより、3Dモデルを表示しているゲームオブジェクトの子オブジェクトとしてPaintToolゲームオブジェクトが生成されます  
HierarcyでPaintToolゲームオブジェクトを選択している間、ペイントができるようになります  
1. PaintToolゲームオブジェクトのInspector画面下部にある「PNGファイルを出力」ボタンを押すことにより、PNGファイルとして保存できます。  
![image](/Images/JP2.png) 

### 詳細説明
* FlowPaintTool Windowの説明  
![image](/Images/JP3.png)  
ペイントモード : 「Flow Paint Mode」と「Color Paint Mode」を選択できます  
作成するテクスチャの幅・高さ : PNGファイルを出力するときの解像度を設定してください  
開始時のテクスチャの解像度より大きくても小さくても問題ありません  
開始時のテクスチャの種類 : 「Assets」と「FilePath」を選択できます  
「Assets」を選択した場合、UnityProject内にインポートした画像を開始時に読み込みます
