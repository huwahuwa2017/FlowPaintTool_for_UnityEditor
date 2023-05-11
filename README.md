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
![image](/Images/JP/0.png)  
1. UnityEditorのPlayModeを開始  
1. Hierarcyへ配置した3Dモデルを表示している（MeshRendererかSkinnedMeshRendererを使用している）ゲームオブジェクトを選択  
1. FlowPaintTool window下部にある「ペイントツールを開始」ボタンを押す  
![image](/Images/JP/1.png)  
これにより、3Dモデルを表示しているゲームオブジェクトの子オブジェクトとしてPaintToolゲームオブジェクトが生成されます  
HierarcyでPaintToolゲームオブジェクトを選択している間、ペイントができるようになります  
1. PaintToolゲームオブジェクトのInspector画面下部にある「PNGファイルを出力」ボタンを押すことにより、PNGファイルとして保存できます。  
![image](/Images/JP/2.png)  

### 詳細説明
#### FlowPaintTool Windowの説明  
![image](/Images/JP/S0.png)  
* ペイントモード  
「Flow Paint Mode」と「Color Paint Mode」を選択できます  
詳しくは[Flow Paint Modeの説明](https://github.com/huwahuwa2017/FlowPaintTool_for_UnityEditor/tree/main#flow-paint-mode%E3%81%AE%E8%AA%AC%E6%98%8E)と
[Color Paint Modeの説明](https://github.com/huwahuwa2017/FlowPaintTool_for_UnityEditor/tree/main#color-paint-mode%E3%81%AE%E8%AA%AC%E6%98%8E)を確認してください
* 開始時のテクスチャの種類  
ペイント開始時にテクスチャを読み込みたい時に使用します  
「Assets」と「FilePath」を選択できます  
「Assets」の場合、UnityProject内にインポートしたテクスチャを選択できるようになります  
「FilePath」の場合、PCに保存している画像(PNG JPG)を選択できるようになります  
* 作成するテクスチャの幅・高さ  
PNGファイルを出力するときの解像度を設定してください  
開始時のテクスチャの解像度とは違う解像度でも問題ありません  
* 使用するUVチャンネル  
ペイント中に使用するUVチャンネルを設定できます  
複数のUV座標を持っている3Dモデルで利用できます  
* にじみの範囲  
UVアイランドの境界をにじませる範囲を設定できます  
* UV epsilon  
UV座標上のポリゴンの重複検出アルゴリズムの許容誤差
Blenderのミラーモディファイアなどによって、UV座標上で重複しているポリゴンが含まれている3Dモデルで、「接続 アンマスク」「接続 マスク」機能を使用するときに影響します  
* 最大Undo回数  
増やしすぎに注意  
#### 「Flow Paint Mode」と「Color Paint Mode」で共通な設定の説明
![image](/Images/JP/C0.png)  
* プレビュー用レンダーテクスチャ  
プレビュー用レンダーテクスチャの名前と場所を確認できます  
このプレビュー用レンダーテクスチャを目的のマテリアルにセットすることにより、ペイントツールを実行しながら結果を確認できます  
(かなり説明不足　例を追加したい)  
* マスクモード  
マスクモードの有効・無効を切り替えます  
詳しくは[Mask Modeの説明](https://github.com/huwahuwa2017/FlowPaintTool_for_UnityEditor/tree/main#mask-mode%E3%81%AE%E8%AA%AC%E6%98%8E)を確認してください  
* プレビューモード
プレビューモードの有効・無効を切り替えます  
目的のマテリアルの現在の状態を確認するときに使います  
(かなり説明不足　例を追加したい)  
* カメラ設定  
カメラの回転速度・移動速度・慣性を設定できます  
* ブラシ設定  
ブラシの大きさ・強さ・形状を設定できます  
ブラシの大きさは R キー長押し + マウスホイール回転 でも調整できます  
ブラシの強さは F キー長押し + マウスホイール回転 でも調整できます  
ブラシの形状は色々ありますが、Smooth(補間曲線)かConstant(一定)で大体何とかなります
* Undo Redo  
状態を戻したりやり直したりできます
\[ キーでUndo、\] キーでRedoを実行できます

* PNGファイルを出力
PNGファイルを出力します  
ペイントツールを実行中のUnityプロジェクトのAssetsフォルダ内に出力した場合、PNGファイルを出力してからインポートします  
もし使用中のテクスチャをこの方法で上書きした場合エラーが出るかもしれません  
#### Flow Paint Modeの説明  
![image](/Images/JP/FP0.png)  
ノーマルマップと似た仕組みで「流れ」を表現するためのテクスチャ(Flow map)を生成するモードです  
以下の設定が追加されます  
(準備中)  
#### Color Paint Modeの説明  
![image](/Images/JP/CP0.png)  
色を塗るモードです  
以下の設定が追加されます  
(準備中)  
#### Mask Modeの説明  
![image](/Images/JP/M0.png)  
影響を与えるか与えないかをポリゴン単位で指定するモードです  
以下の設定が追加されます  
(準備中)  
