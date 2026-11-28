using UnityEngine;
using UnityEngine.InputSystem;

namespace FFrameWork.Live
{
    // デバイスの操作を ArrowLogic に渡し、判定枠の位置・幅を各ビューへ反映する。
    public class ArrowController : MonoBehaviour
    {
        // 矢印本体の表示を更新するビュー
        [SerializeField] private SliderArrowView arrowView;

        // 現在位置を示すポインタービュー
        [SerializeField] private ArrowPointerView pointerView;

        // 判定枠をデバッグ表示するビュー
        [SerializeField] private ArrowFrameDebugView frameDebugView;

        [Header("Pointer Settings")]
        // 矢印が移動できる最小ワールド X 座標
        [SerializeField] private float minWorldX = -270.0f;

        // 矢印が移動できる最大ワールド X 座標
        [SerializeField] private float maxWorldX = 270.0f;

        // 端（|X| >= 180〜270）における基本移動感度
        [SerializeField] private float sensitivity = 3f;

        // 中央領域での移動感度の補正設定
        [Header("Center Sensitivity Settings")]
        // 感度補正を適用する中央領域の境界（X 座標の絶対値）
        [SerializeField] private float centerZoneThreshold = 180.0f; 

        // 中心（X = 0）での感度倍率。0.5 なら基準感度の 50% になる。
        [SerializeField] private float centerSensitivityMultiplier = 0.8f; 

        // 初期化時のワールド X 座標
        [SerializeField] private float startWorldX = 0f;

        [Header("Sprite Size Settings")]
        // 初期化時の矢印表示サイズ
        [SerializeField] private float startSpriteSize = 39f;

        // 矢印操作用の入力管理
        private InputManager inputManager;
        // 入力管理をこの Controller が生成したか
        private bool ownsInputManager;

        // 矢印位置とサイズの状態計算
        private ArrowLogic arrowLogic;

        // カーソル位置リセット直後の入力差分を無視するか
        private bool ignoreNextPositionDelta;

        // 矢印ロジックが利用可能か
        public bool HasLogic => arrowLogic != null && arrowLogic.IsSetup;

        // ライブ全体で共有する入力管理を受け取る
        public void SetInputManager(InputManager inputManager)
        {
            if (this.inputManager == inputManager)
            {
                return;
            }

            DisposeOwnedInputManager();
            this.inputManager = inputManager;
            ownsInputManager = false;
            ignoreNextPositionDelta = true;
        }

        // 初期化時に入力、ロジック、ビューを準備してカーソル状態を設定する
        private void Start()
        {
            CenterMouseCursor();
            SetupArrowLogic();
            SetupArrowViews();
            SetupInputManager();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;

            ResetPositionToCenter();
        }

        // 毎フレーム入力差分を読み取り、矢印状態と表示を更新する
        private void Update()
        {
            if (arrowLogic == null || !arrowLogic.IsSetup)
            {
                SetupArrowLogic();
            }

            float deltaX = ReadArrowPositionDelta();
            arrowLogic.OnUpdate(deltaX);
            ApplyArrowViews();
        }

        // 矢印ロジックが未初期化なら Inspector 設定で初期化する
        private void SetupArrowLogic()
        {
            if (arrowLogic != null && arrowLogic.IsSetup)
            {
                return;
            }

            arrowLogic = new ArrowLogic();
            arrowLogic.Setup(minWorldX, maxWorldX, startWorldX, startSpriteSize);
        }

        // 矢印関連ビューを初期化する
        private void SetupArrowViews()
        {
            pointerView?.Setup();
            arrowView?.Setup();
            frameDebugView?.Setup();
        }

        // ライブ中入力を受け取る InputManager を作成する
        private void SetupInputManager()
        {
            if (inputManager != null)
            {
                return;
            }

            inputManager = new InputManager();
            ownsInputManager = true;
            inputManager.EnableInLiveInput();
        }

        // 中央領域では感度を抑え、境界へ近づくほど通常感度へ戻す。範囲は centerZoneThreshold で指定する。
        private float ReadArrowPositionDelta()
        {
            if (ignoreNextPositionDelta)
            {
                ignoreNextPositionDelta = false;
                return 0f;
            }

            if (inputManager == null)
            {
                return 0f;
            }

            float rawDelta = inputManager.ArrowPositionDelta;
            float currentSensitivity = sensitivity;

            if (arrowLogic != null && arrowLogic.IsSetup)
            {
                float absX = Mathf.Abs(arrowLogic.CurrentX);

                // 中央領域内では、中心からの距離に応じて感度を補正する。
                if (absX < centerZoneThreshold)
                {
                    // 中心を 0、中央領域の境界を 1 とする補間係数を求める。
                    float t = Mathf.InverseLerp(0f, centerZoneThreshold, absX);

                    // 中心の感度倍率から 1.0 まで線形補間する。
                    float multiplier = Mathf.Lerp(centerSensitivityMultiplier, 1.0f, t);

                    currentSensitivity *= multiplier;
                }
                // 中央領域の外側では基準の sensitivity をそのまま使う。
            }

            return rawDelta * currentSensitivity;
        }

        // 現在の矢印ロジック状態を各ビューへ反映する
        private void ApplyArrowViews()
        {
            if (arrowLogic == null || !arrowLogic.IsSetup)
            {
                return;
            }

            float currentX = arrowLogic.CurrentX;
            float currentSize = arrowLogic.CurrentSize;

            pointerView?.ApplyPosition(currentX);
            arrowView?.Apply(currentX, currentSize);
            frameDebugView?.Apply(currentX, currentSize);
        }

        // 幅を指定段階へ切り替え、表示も即座に更新する。
        public void SetSizeStage(int stageIndex)
        {
            SetupArrowLogic();
            arrowLogic?.SetSizeStage(stageIndex);
            ApplyArrowViews();
        }

        // レーン数を幅の段階へ変換する。1 未満は 1 とし、最も近い段階を選ぶ。
        public void SetSizeByLaneWidth(int laneWidth)
        {
            int stageIndex = ArrowLogic.GetStageIndexForLaneWidth(Mathf.Max(1, laneWidth));
            SetSizeStage(stageIndex);
        }

        // 判定処理へ渡す現在の枠状態を返す。ロジック未初期化なら既定値を返す。
        public ArrowJudgmentFrame GetArrowJudgmentFrame()
        {
            return HasLogic ? arrowLogic.GetArrowJudgmentFrame() : default;
        }

        // 開曲時の幅と中央位置へ戻し、直後の入力差分を 1 回無視して位置飛びを抑える。
        public void ResetForSong(int initialLaneWidth)
        {
            SetupArrowLogic();
            SetSizeByLaneWidth(initialLaneWidth);
            arrowLogic.SetAbsoluteX(0f);
            ignoreNextPositionDelta = true;
            ApplyArrowViews();
        }

        // 長押しによる幅変更の開始値と目標段階を記録する。
        public void BeginLongSizeChange(int targetStageIndex)
        {
            SetupArrowLogic();
            arrowLogic.BeginLongSizeChange(targetStageIndex);
        }

        // 0 ～ 1 の進捗をロジックへ渡して幅を補間し、表示へ反映する。
        public void UpdateLongSizeChange(float progress)
        {
            if (!HasLogic)
            {
                return;
            }

            arrowLogic.UpdateLongSizeChange(progress);
            ApplyArrowViews();
        }

        // 成功時は目標段階へ、失敗時は途中の状態に応じた段階へ確定する。
        public void FinishLongSizeChange(bool success)
        {
            if (!HasLogic)
            {
                return;
            }

            arrowLogic.FinishLongSizeChange(success);
            ApplyArrowViews();
        }

        // 幅は維持して位置だけを中央へ戻し、次の入力差分を無視する。
        public void ResetPositionToCenter()
        {
            SetupArrowLogic();

            arrowLogic.SetAbsoluteX(0f);
            ignoreNextPositionDelta = true;
            ApplyArrowViews();
        }

        // マウスが接続されている場合だけ、実カーソルを画面中央へ移動する。
        private void CenterMouseCursor()
        {
            if (Mouse.current == null)
            {
                return;
            }

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Mouse.current.WarpCursorPosition(screenCenter);
        }

        // 自身が生成した入力管理を解放し、カーソルの表示とロック状態を戻す。
        private void OnDestroy()
        {
            DisposeOwnedInputManager();
            inputManager = null;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // 外部から共有された InputManager は破棄せず、自身が所有するものだけを解放する。
        private void DisposeOwnedInputManager()
        {
            if (!ownsInputManager)
            {
                return;
            }

            inputManager?.Dispose();
            ownsInputManager = false;
        }
    }
}
