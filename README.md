# FlowPaintTool_for_UnityEditor

### これは何？
Unityのエディター拡張で、「方向」や「流れ」を表現するためのテクスチャ(Flow map)を生成したり色を塗ったりできます  
この動画ではFlowPaintTool_for_UnityEditorを使って、lilToonのファー用のノーマルマップと長さマスクを生成しています  

https://github.com/huwahuwa2017/FlowPaintTool_for_UnityEditor/assets/42481055/ff16965c-98d0-47e9-a2ee-ea9b8d898097  

### 準備
[Releases](https://github.com/huwahuwa2017/FlowPaintTool_for_UnityEditor/releases)からunitypackageをダウンロードしてプロジェクトにインポートしてください

対象となる3Dモデルのインポート設定のRead/Write有効にして、Hierarcyへ配置してください

### 使い方
1. UnityEditorの画面上部にある「FlowPaintTool」をクリックして「Open」をクリック  
![image](/Readme/JP/0.png)  
1. UnityEditorのPlayModeを開始  
1. Hierarcyへ配置した3Dモデルを表示している（MeshRendererかSkinnedMeshRendererを使用している）ゲームオブジェクトを選択  
1. FlowPaintTool window下部にある「ペイントツールを開始」ボタンを押す  
![image](/Readme/JP/1.png)  
これにより、3Dモデルを表示しているゲームオブジェクトの子オブジェクトとしてPaintToolゲームオブジェクトが生成されます  
HierarcyでPaintToolゲームオブジェクトを選択している間、ペイントができるようになります  
1. PaintToolゲームオブジェクトのInspector画面下部にある「PNGファイルを出力」ボタンを押すことにより、PNGファイルとして保存できます。  
![image](/Readme/JP/2.png)  

### 詳細説明
#### メインメニューの説明  
![image](/Readme/JP/0.png)  
* Open  
FlowPaintTool Windowを開きます  
* Japanese English  
FlowPaintTool内のGUIの言語を変更します  
* Reset Parameter  
FlowPaintTool内部の設定値をリセットします  
なにか問題が発生した時に使えるかもしれません  
#### FlowPaintTool Windowの説明  
![image](/Readme/JP/S0.png)  
* ペイントモード  
「Flow Paint Mode」と「Color Paint Mode」を選択できます  
詳しくは[Flow Paint Modeの説明](#flow-paint-modeの説明)と
[Color Paint Modeの説明](#color-paint-modeの説明)を確認してください
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
![image](/Readme/JP/C0.png)  
* プレビュー用レンダーテクスチャ  
プレビュー用レンダーテクスチャの名前と場所を確認できます  
このプレビュー用レンダーテクスチャを目的のマテリアルにセットすることにより、ペイントツールを実行しながら結果を確認できます  
(かなり説明不足　例を追加したい)  
* マスクモード  
マスクモードの有効・無効を切り替えます  
Tabキーで切り替えることができます  
詳しくは[Mask Modeの説明](#mask-modeの説明)を確認してください  
* プレビューモード  
プレビューモードの有効・無効を切り替えます  
Zキーで切り替えることができます  
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
![image](/Readme/JP/FP0.png)  
ノーマルマップと似た仕組みで「方向」や「流れ」を表現するためのテクスチャ(Flow map)を生成するモードです  
以下の設定が追加されます  
* 高さの制限  
Flow mapに書き込むZ成分の大きさを範囲内に制限します  
これはフロー ペイントで生成した「方向」が3Dオブジェクト内部に沈み込む状態を抑制したい時に役に立ちます  
逆にオブジェクト内部に沈み込む「方向」を書き込んだFlow mapを利用したい時、注意するべき点があります  
詳しくは(準備中)  
* 方向の固定  
Flow mapへ書き込む「方向」をワールド座標系で指定します  
* 表示する法線の長さ  
表示する法線の長さです 単位はメートルです  
* 表示する法線の量  
表示する法線の量です  
#### Color Paint Modeの説明  
![image](/Readme/JP/CP0.png)  
色を塗るモードです  
以下の設定が追加されます  
* 色  
塗りたい色を設定できます  
* 編集する色の選択  
赤・緑・青・透明度の値を編集するかしないかを設定できます  
例えばR、G、BをオフにしてAをオンにした場合、透明度だけを編集することになります  
#### Mask Modeの説明  
![image](/Readme/JP/M0.png)  
影響を与えるか与えないかをポリゴン単位で指定するモードです  
以下の設定が追加されます  
* 接続マスク 接続アンマスク  
頂点座標・ポリゴンの表と裏・UV座標などを考慮して、接続されていると判定したポリゴンをすべてマスク・アンマスクします  
* すべてマスク すべてアンマスク すべて反転  
すべてのポリゴンをマスク・アンマスク・反転します  
#### 注意点  
Flow Paint Modeでオブジェクト内部に沈み込む「方向」を書き込んだFlow mapをインポートする場合、画像インポート設定のTexture TypeをDefaultにする必要があるかもしれません  
画像インポート設定のTexture TypeがNormal Mapの場合、dxt5nm形式によってZ成分の符号が常に+になることを考慮する必要があります  
  
C#を編集したい場合は、Play modeを実行していない状態で書き換えてください  
Unity editorのPlay modeが実行中かつPaint toolが実行中にC#を書き換えると、実行中のPaint tool内のデータが破損します  


