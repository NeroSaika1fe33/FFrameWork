using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    // ライブのノーツ・判定・ゲージ・タップ演出を表示するビュー。判定や得点計算自体は行わない。
    public partial class LiveFrontView : LiveViewBase
    {
        [Header("Render & Camera Dependencies")]
        // 背景描画に使う Renderer
        [SerializeField] private Renderer backgroundRenderer;

        // クリアゲージ 0 時の暗転マスク
        [SerializeField] private SpriteRenderer deadMask;

        // ライブ前面表示用の Camera
        [SerializeField] private Camera frontViewCamera;

        [Header("Explicit Inspector Assignments (Performance Optimizations)")]
        // 自動探索を避けたい場合に明示指定する判定レーン
        [SerializeField] private Transform explicitMainJudgeLane;

        // 自動探索を避けたい場合に明示指定するサウンドプレイヤー
        [SerializeField] private MonoBehaviour explicitLiveSoundPlayer;

        // ライブ全体の制御元
        private BaseLiveController baseController;

        // 背景用に複製した Material
        private Material backgroundMaterial;

        // フェード用に保持する SpriteRenderer ごとの初期アルファ
        private Dictionary<SpriteRenderer, float> spriteRendererAlphaDict;

        // この判定でハプティック再生済みか
        private bool isPlayedHaptic;

        // エフェクト Camera の基準 FOV をキャッシュ済みか

        // 前回エフェクト行列を更新した画面幅
        private int lastTapEffectMatrixScreenWidth = -1;

        // 前回エフェクト行列を更新した画面高さ
        private int lastTapEffectMatrixScreenHeight = -1;

        // 遅延してエフェクト行列を更新する残りフレーム数
        private int pendingTapEffectMatrixRefreshFrames;

        // ノーツ表示とライン表示の管理クラス
        private NotesViewManager notesViewManager;

        // タップエフェクト表示
        private TapEffectView tapEffect;

        // ロングタップエフェクト表示
        private LongTapEffectView longTapEffectView;

        // 背景 Renderer の SortingOrder
        private const int BackgroundSortingOrder = -200;

        // 画面サイズ変更後にタップエフェクト行列を再更新するフレーム数
        private const int TapEffectMatrixDeferredRefreshFrames = 2;

        // ライブ前面表示に必要な UI、エフェクト、ノーツ表示を初期化する
        // baseController: ライブ全体を管理するコントローラー
        public override void Setup(BaseLiveController baseController)
        {
            base.Setup(baseController);
            ResolveMissingReferences();
            ResolveDefaultAssets();
            this.baseController = baseController;
            clearGaugeView?.Setup(OnPause, GetLiveMaxClearGauge(), GetLiveWarningClearGauge(), GetLiveClearGaugeThreshold());
            if (baseController != null)
            {
                LiveBootDataBase bootData = baseController.BootData;
                scoreView?.Setup(bootData != null ? bootData.MusicData : null);
                comboView?.Setup(baseController.Settings == null || baseController.Settings.UseAllPerfectEffect);
                baseController.BaseCamera = frontViewCamera;
            }

            countdownView?.Setup();
            screenEffectView?.Setup(baseController);
            SetupBackground();

            if (fade != null) fade.enabled = false;
            if (autoLabel != null) autoLabel.SetActive(false);

            laneView?.Setup(baseController != null ? baseController.Settings : null);

			notesViewManager = new NotesViewManager();
			notesViewManager.Setup(liveRoot);
			notesViewManager.SetAssets(
				notePrefabs, longNoteLineTexture, fixedLongNoteLineTexture, sliderLongLineTexture, volumeLongDownLineTexture, volumeLongUpLineTexture,
				guideLineTexture, longNoteLineMaterial, fixedLongNoteLineMaterial, guideLineMaterial, sliderLongLineMaterial, volumeLongDownLineMaterial, volumeLongUpLineMaterial);

            CacheSpriteRendererAlphas();
            UpdateSpriteAlpha(0f);
        }

        // ノーツ種別ごとの必要数に合わせてノーツ表示プールを作成する
        // notePoolCount: ノーツ種別ごとのプール数
        public override void CreateNotePool(Dictionary<NoteCategory, int> notePoolCount)
        {
            notesViewManager?.CreateNotePool(baseController, notePoolCount);
        }

        // 初期スコア、クリアゲージ、コンボを前面 UI へ反映する
        // score: 初期表示するスコア情報
        public override void SetupScore(LiveScore score)
        {
            scoreView?.SetupScore(score);
            clearGaugeView?.Execute(score.clearGauge);
            comboView?.Excute(score);
        }

        // スコアゲージ計算器をスコア表示へ渡す
        // scoreGaugeCalculator: 新しいゲージ計算器
        public override void ChangeScoreCalculator(ScoreGaugeCalculator scoreGaugeCalculator)
        {
            scoreView?.ChangeScoreCalculator(scoreGaugeCalculator);
        }

        // ライブ読み込み時にタップエフェクトと初期透明度を準備する
        public override void OnLoad()
        {
            SetupTapEffect();
            UpdateSpriteAlpha(0f);
        }

        // ライブ解放時のビュー解放入口
        public override void OnUnload()
        {
        }

        // 楽曲開始時の前面 UI 演出入口
        // time: 楽曲開始時刻
        public override void MusicStart(float time)
        {
        }

        // リズムゲーム開始時に画面依存レイアウトを更新して表示を開始する
        public override void RhythmGameStart()
        {
            RefreshScreenDependentLayout();
        }

        // 画面サイズ変更時に Camera とエフェクト行列を更新する
        public override void OnScreenSizeChanged()
        {
            RefreshScreenDependentLayout();
            QueueTapEffectMatrixRefresh();
        }

        // リトライ時にノーツ、エフェクト、UI 表示状態を初期化する
        public override void Retry()
        {
            notesViewManager?.Clear();
            longTapEffectView?.Clear();
            tapEffect?.Clear();
            comboView?.Clear();
            clearGaugeView?.Clear();

            if (autoLabel != null) autoLabel.SetActive(false);
            UpdateSpriteAlpha(0f);
        }

        // ライブ終了時に前面 UI をフェードアウトする
        public override void Finish()
        {
            FadeOut();
        }


        // 背景 Renderer の Material を複製してライブ用に設定する
        private void SetupBackground()
        {
            if (backgroundRenderer == null) return;

            backgroundRenderer.sortingOrder = BackgroundSortingOrder;

            Material source = backgroundRenderer.material;
            if (source == null) return;

            backgroundMaterial = new Material(source);
            if (backgroundMaterial.HasProperty("_ZWrite"))
            {
                backgroundMaterial.SetFloat("_ZWrite", 0f);
            }
            backgroundRenderer.material = backgroundMaterial;

            if (baseController != null)
            {
                backgroundMaterial.mainTexture = baseController.BackgroundTexture;
            }
            backgroundMaterial.color = Color.black;
        }

        // タップエフェクトとロングタップエフェクトを初期化する
        private void SetupTapEffect()
        {
            if (effectRoot == null) return;

            tapEffect = effectRoot.GetComponent<TapEffectView>() ?? effectRoot.gameObject.AddComponent<TapEffectView>();
            ILiveSoundPlayer soundPlayer = ResolveLiveSoundPlayer();
            tapEffect.Setup(baseController, tapEffectPrefabs, tapSingleEffectPrefab, tapFlickEffectPrefab, tapLoopEffectPrefab, soundPlayer, ResolveMainJudgeLane());

            Transform longTapEffectRoot = ResolveLongTapEffectRoot();
            Transform longTapEffectTransform = longTapEffectRoot != null ? longTapEffectRoot.Find("LongTapEffect") : null;
            if (longTapEffectTransform != null)
            {
                longTapEffectView = longTapEffectTransform.GetComponent<LongTapEffectView>() ?? longTapEffectTransform.gameObject.AddComponent<LongTapEffectView>();
            }
            else
            {
                GameObject longTapEffectObject = new GameObject("LongTapEffect");
                longTapEffectView = longTapEffectObject.AddComponent<LongTapEffectView>();
                longTapEffectView.transform.SetParent(longTapEffectRoot != null ? longTapEffectRoot : effectRoot, false);
            }

            longTapEffectView.Setup(longHoldAuraPrefab, longHoldGenPrefab, longTapEffectPoolCount, baseController, soundPlayer);
            effectRoot.position = transform.position;
        }

        // ロングタップエフェクトを配置するルート Transform を解決する
        private Transform ResolveLongTapEffectRoot()
        {
            if (liveRoot != null)
            {
                return liveRoot;
            }

            Transform judgeLane = ResolveMainJudgeLane();
            if (judgeLane != null && judgeLane.parent != null)
            {
                return judgeLane.parent;
            }

            Transform foundLiveRoot = transform.Find("LiveRoot");
            if (foundLiveRoot != null)
            {
                return foundLiveRoot;
            }

            GameObject liveRootObject = GameObject.Find("LiveRoot");
            return liveRootObject != null ? liveRootObject.transform : null;
        }

        // タップエフェクト用 Camera の行列をグローバル Shader パラメータへ設定する
        private void RefreshTapEffectCameraMatrices()
        {
            if (effectCamera == null)
            {
                CacheTapEffectMatrixScreenSize();
                return;
            }

            if (effectRoot != null) effectRoot.position = transform.position;

            effectCamera.rect = frontViewCamera != null ? frontViewCamera.rect : new Rect(0f, 0f, 1f, 1f);
            effectCamera.aspect = GetTapEffectCameraAspect();
            effectCamera.ResetProjectionMatrix();

            Shader.SetGlobalMatrix(ShaderPropertyID.TAP_EFFECT_VIEW_MATRIX_ID, effectCamera.worldToCameraMatrix);
            Shader.SetGlobalMatrix(ShaderPropertyID.TAP_EFFECT_PROJECTION_MATRIX_ID, GL.GetGPUProjectionMatrix(effectCamera.projectionMatrix, false));
            effectCamera.enabled = false;
            CacheTapEffectMatrixScreenSize();
        }

        // タップエフェクト Camera に使う画面アスペクト比を取得する
        private float GetTapEffectCameraAspect()
        {
            Rect rect = frontViewCamera != null ? frontViewCamera.rect : new Rect(0f, 0f, 1f, 1f);
            float width = Screen.width * Mathf.Max(rect.width, 0.0001f);
            float height = Screen.height * Mathf.Max(rect.height, 0.0001f);
            return height > 0f ? width / height : 16f / 9f;
        }

        // 数フレーム後までタップエフェクト行列の再更新を予約する
        private void QueueTapEffectMatrixRefresh() => pendingTapEffectMatrixRefreshFrames = TapEffectMatrixDeferredRefreshFrames;

        // 画面サイズ変更または予約がある場合にタップエフェクト行列を更新する
        private void RefreshTapEffectCameraMatricesIfNeeded()
        {
            if (lastTapEffectMatrixScreenWidth == Screen.width &&
                lastTapEffectMatrixScreenHeight == Screen.height &&
                pendingTapEffectMatrixRefreshFrames <= 0) return;

            RefreshTapEffectCameraMatrices();
            if (pendingTapEffectMatrixRefreshFrames > 0) pendingTapEffectMatrixRefreshFrames--;
        }

        // 現在の画面サイズをタップエフェクト行列更新済みサイズとして記録する
        private void CacheTapEffectMatrixScreenSize()
        {
            lastTapEffectMatrixScreenWidth = Screen.width;
            lastTapEffectMatrixScreenHeight = Screen.height;
        }

        // liveRoot 配下の SpriteRenderer 初期アルファをキャッシュする
        private void CacheSpriteRendererAlphas()
        {
            spriteRendererAlphaDict = new Dictionary<SpriteRenderer, float>();
            if (liveRoot == null) return;

            foreach (var renderer in liveRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer != null && !spriteRendererAlphaDict.ContainsKey(renderer))
                {
                    spriteRendererAlphaDict.Add(renderer, renderer.color.a);
                }
            }
        }

        // キャッシュした初期アルファに倍率を掛けて表示透明度を更新する
        // alpha: 0 から 1 のアルファ倍率
        private void UpdateSpriteAlpha(float alpha)
        {
            if (spriteRendererAlphaDict == null) return;

            foreach (var pair in spriteRendererAlphaDict)
            {
                if (pair.Key == null) continue;
                Color color = pair.Key.color;
                color.a = pair.Value * alpha;
                pair.Key.color = color;
            }
        }

        // 前面 UI を非表示にし、フェード Sprite を表示する
        private void FadeOut()
        {
            if (autoLabel != null) autoLabel.SetActive(false);
            UpdateSpriteAlpha(0f);
            if (fade != null)
            {
                fade.enabled = true;
                Color color = fade.color;
                color.a = 1f;
                fade.color = color;
            }
        }

        // ライブ設定から最大クリアゲージを取得する
        private int GetLiveMaxClearGauge()
        {
            LiveBundleBuildData data = baseController != null && baseController.BootData != null ? baseController.BootData.BundleBuildData : null;
            return data != null ? data.MaxClearGauge : LiveConfig.DefaultMaxClearGauge;
        }

        // ポーズボタン押下時にカウントダウン停止とライブ停止を行う
        private void OnPause()
        {
            countdownView?.StopCountdown();
            baseController?.Pause();
        }

        // 再開時の前面 UI 処理入口
        // time: 再開した楽曲時間
        public override void Resume(float time) { }

        // カウントダウン表示を開始する
        public override void Countdown() => countdownView?.StartCountdown();

        // 画面サイズに依存する Camera、背景、エフェクト行列を更新する
        private void RefreshScreenDependentLayout()
        {
            ResolveMissingReferences();
            UpdateBackgroundSize();
            RefreshTapEffectCameraMatrices();
        }

        // Inspector 未設定の参照を名前や子階層から解決する
        private void ResolveMissingReferences()
        {
            if (effectCamera == null)
            {
                effectCamera = FindComponentByName<Camera>("EffectCamera");
            }
            if (frontViewCamera == null)
            {
                frontViewCamera =
                    FindComponentByName<Camera>("FrontViewCamera") ??
                    FindComponentByName<Camera>("Front View Camera") ??
                    FindComponentByName<Camera>("Main Camera") ??
                    FindFirstCameraExcept(effectCamera);
            }
            if (liveRoot == null)
            {
                liveRoot = FindChildByName(transform, "LiveRoot");
            }
            if (effectRoot == null)
            {
                effectRoot = FindChildByName(transform, "EffectRoot");
            }
            if (mainJudgeLane == null)
            {
                mainJudgeLane = FindChildByName(liveRoot != null ? liveRoot : transform, "MainJudgeLane");
            }
            if (fade == null)
            {
                fade = FindComponentByName<SpriteRenderer>("Fade");
            }
            if (deadMask == null)
            {
                deadMask = FindComponentByName<SpriteRenderer>("DeadMask");
            }
            if (backgroundRenderer == null)
            {
                backgroundRenderer = FindComponentByName<Renderer>("Background");
            }
            if (laneView == null)
            {
                laneView = GetComponentInChildren<LaneView>(true);
            }
            if (judgmentView == null)
            {
                judgmentView = GetComponentInChildren<JudgmentView>(true);
            }
            if (judgmentDescriptionView == null)
            {
                judgmentDescriptionView = GetComponentInChildren<JudgmentDescriptionView>(true);
            }
            if (countdownView == null)
            {
                countdownView = GetComponentInChildren<CountdownView>(true);
            }
            if (screenEffectView == null)
            {
                screenEffectView = GetComponentInChildren<ScreenEffectView>(true);
            }
            if (liveSoundPlayer == null)
            {
                liveSoundPlayer = FindLiveSoundPlayer();
            }
        }

        // 未指定のエフェクト Prefab を明示設定だけで補完する
        private void ResolveDefaultAssets()
        {
            if (tapFlickEffectPrefab == null)
            {
                tapFlickEffectPrefab = tapSingleEffectPrefab;
            }
            if (tapLoopEffectPrefab == null)
            {
                tapLoopEffectPrefab = tapSingleEffectPrefab;
            }
            if (longHoldAuraPrefab == null)
            {
                Debug.LogError("[LiveFrontView] Missing longHoldAuraPrefab. Assign it in the Inspector.");
            }
            if (longHoldGenPrefab == null)
            {
                Debug.LogError("[LiveFrontView] Missing longHoldGenPrefab. Assign it in the Inspector.");
            }
        }

        // 指定名の子 Transform から Component を取得する
        // objectName: 探索する GameObject 名
        private T FindComponentByName<T>(string objectName) where T : Component
        {
            Transform target = FindChildByName(transform, objectName);
            return target != null ? target.GetComponent<T>() : null;
        }

        // 子階層を再帰的に探索して指定名の Transform を取得する
        // root: 探索開始 Transform
        // childName: 探索する Transform 名
        private static Transform FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
            {
                return null;
            }
            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildByName(root.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        // 指定 Camera 以外で最初に見つかった Camera を取得する
        // excludedCamera: 除外する Camera
        private Camera FindFirstCameraExcept(Camera excludedCamera)
        {
            Camera[] cameras = GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i] != excludedCamera)
                {
                    return cameras[i];
                }
            }

            return cameras.Length > 0 ? cameras[0] : null;
        }

        // 子階層から ILiveSoundPlayer を実装する MonoBehaviour を探す
        private MonoBehaviour FindLiveSoundPlayer()
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ILiveSoundPlayer)
                {
                    return behaviours[i];
                }
            }

            return null;
        }

        // Inspector 編集時に参照を自動解決する
        private void OnValidate()
        {
            ResolveMissingReferences();
        }

        // 正投影カメラの表示範囲に背景と暗転マスクを合わせる。透視投影時は従来の基準高さを使う。
        private void UpdateBackgroundSize()
        {
            float height = frontViewCamera != null && frontViewCamera.orthographic
                ? frontViewCamera.orthographicSize * 2f
                : 10f;
            float aspect = GetTapEffectCameraAspect();

            if (backgroundRenderer != null) backgroundRenderer.transform.localScale = new Vector3(height * aspect, height, 1f);
            if (deadMask != null) deadMask.size = new Vector2(height * aspect, height);
        }

        // 毎フレームのノーツ表示とロングタップエフェクトを更新する
        // time: 現在の楽曲時間
        // currentFrameInfo: 現在フレームの譜面時間情報
        public override void OnUpdate(float time, MusicScoreInfo currentFrameInfo)
        {
            isPlayedHaptic = false;
            notesViewManager?.OnUpdate(time);
            longTapEffectView?.Excute(time, currentFrameInfo.seVolume);
        }

        // ノーツを表示プールから生成する
        // note: 表示するノーツ
        public override void SpawnNote(INote note)
        {
            notesViewManager?.SpawnNote(note);
            if (note is LongNote) longTapEffectView?.Add(note);
        }

        // ノーツを表示プールへ戻す
        // note: 戻すノーツ
        public override void UnspawnNote(INote note)
        {
            notesViewManager?.UnspawnNote(note);
            if (note is LongNote) longTapEffectView?.Remove(note);
        }

        // ノーツ判定時にエフェクトと判定表示を再生する
        // note: 判定済みノーツ
        public override void JudgmentNote(INote note)
        {
            if (note == null) return;
            Effect(note);
            judgmentView?.Excute(note.JudgeInfo);
            judgmentDescriptionView?.Excute(note.JudgeInfo);
        }

        // 未取得のレーン入力をタップエフェクトへ通知する
        // lane: 入力されたレーン番号
        // input: 入力情報
        public override void Unpicked(int lane, ref LiveInput input) => tapEffect?.Unpicked(lane, ref input);

        // 明示指定または自動探索でメイン判定レーンを取得する
        private Transform ResolveMainJudgeLane()
        {
            if (explicitMainJudgeLane != null) return explicitMainJudgeLane;
            if (mainJudgeLane != null) return mainJudgeLane;

            mainJudgeLane = liveRoot != null ? liveRoot.Find("MainJudgeLane") : transform.Find("MainJudgeLane");
            return mainJudgeLane;
        }

        // 明示指定または子階層から ILiveSoundPlayer を取得する
        private ILiveSoundPlayer ResolveLiveSoundPlayer()
        {
            if (explicitLiveSoundPlayer is ILiveSoundPlayer explicitPlayer) return explicitPlayer;
            if (liveSoundPlayer is ILiveSoundPlayer assignedSoundPlayer) return assignedSoundPlayer;

            return baseController != null ? baseController.GetComponentInChildren<ILiveSoundPlayer>(true) : null;
        }

        // スコア表示を更新する
        // score: 更新後のスコア情報
        // addScore: 今回加算されたスコア
        public override void UpdateScore(ref LiveScore score, int addScore) => scoreView?.UpdateScore(ref score, addScore);

        // コンボ表示を更新する
        // score: 更新後のスコア情報
        public override void UpdateCombo(LiveScore score) => comboView?.Excute(score);

        public override void UpdateClearGauge(int clearGauge)
        {
            clearGaugeView?.Execute(clearGauge);
        }

        // ライブ設定から瀕死ラインを取得する
        private int GetLiveWarningClearGauge()
        {
            LiveBundleBuildData data = baseController != null && baseController.BootData != null ? baseController.BootData.BundleBuildData : null;
            return data != null ? data.WarningClearGauge : Mathf.FloorToInt(GetLiveMaxClearGauge() * 0.2f);
        }

        // ライブ設定からクリアラインを取得する
        private int GetLiveClearGaugeThreshold()
        {
            LiveBundleBuildData data = baseController != null && baseController.BootData != null ? baseController.BootData.BundleBuildData : null;
            return data != null ? data.ClearGaugeThreshold : Mathf.FloorToInt(GetLiveMaxClearGauge() * 0.7f);
        }

        private void OnDestroy()
        {
            if (backgroundMaterial != null)
            {
                Destroy(backgroundMaterial);
                backgroundMaterial = null;
            }
        }

        // ノーツ判定に応じたタップエフェクトと画面エフェクトを再生する
        // note: 判定済みノーツ
        private void Effect(INote note)
        {
            tapEffect?.Excute(note, CheckPlayedHaptic);
            screenEffectView?.Excute(note);
        }

        // 同一判定内でハプティックが再生済みかを返し、以降を再生済みにする
        private bool CheckPlayedHaptic()
        {
            bool result = isPlayedHaptic;
            isPlayedHaptic = true;
            return result;
        }

        // エフェクト行列と死亡マスクの追従表示を更新する
        private void LateUpdate()
        {
            RefreshTapEffectCameraMatricesIfNeeded();
            if (deadMask == null) return;

            Color color = deadMask.color;
            float targetAlpha = 0f;
            color.a = Mathf.Lerp(color.a, targetAlpha, 0.1f);
            deadMask.color = color;
        }
    }
}
