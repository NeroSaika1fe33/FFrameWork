using System;

namespace FFrameWork.Live
{
    // ノーツの得点処理を ScoreLogic へ接続し、結果イベントを表示側へ中継する。
    internal sealed class ScoreSystem
    {
        private readonly LiveBundleBuildData liveBundleBuildData;
        private readonly PresentationRoot presentationRoot;
        private ScoreLogic scoreLogic;

        // 計算用設定と表示通知先を保持する。ScoreLogic 本体は Setup 時に用意する。
        public ScoreSystem(LiveBundleBuildData liveBundleBuildData, PresentationRoot presentationRoot)
        {
            this.liveBundleBuildData = liveBundleBuildData;
            this.presentationRoot = presentationRoot;
        }

        public LiveScore Score => scoreLogic?.Score ?? default;
        public bool IsAllPerfectCombo => scoreLogic != null && scoreLogic.IsAllPerfectCombo;
        public bool IsPerfectCombo => scoreLogic != null && scoreLogic.IsPerfectCombo;
        public bool IsClearGauge => scoreLogic != null && scoreLogic.IsClearGauge;

        // 既定の ScoreLogic を生成し、起動データとノーツで初期化する。
        public void Setup(LiveBootDataBase bootData, NoteBase[] notes)
        {
            SetScoreLogic(new ScoreLogic(liveBundleBuildData), bootData, notes);
        }

        // 旧ロジックの購読を解除し、初期化より先に新ロジックのイベントを購読する。
        // これにより Setup 中の初期スコア通知も表示側へ届く。
        public void SetScoreLogic(ScoreLogic scoreLogic, LiveBootDataBase bootData, NoteBase[] notes)
        {
            DetachScoreLogicEvents(this.scoreLogic);
            this.scoreLogic = scoreLogic;
            AttachScoreLogicEvents(this.scoreLogic);
            this.scoreLogic?.Setup(bootData, notes, Array.Empty<EventBase>());
        }

        // 購読を解除してロジック参照を手放し、破棄後の表示通知を防ぐ。
        public void Dispose()
        {
            DetachScoreLogicEvents(scoreLogic);
            scoreLogic = null;
        }

        // コンティニュー時のゲージと猶予時間の再設定を ScoreLogic へ委譲する。
        public void Continue(float time)
        {
            scoreLogic?.Continue(time);
        }

        // ノーツの得点・コンボ・ダメージ・判定補正コールバックを現在の ScoreLogic に接続する。
        public void ConfigurePlaybackNote(NoteBase note)
        {
            if (note == null)
            {
                return;
            }

            note.OnUpdateScore = noteInfo => scoreLogic?.CalculateScore(noteInfo);
            note.OnUpdateCombo = noteInfo => scoreLogic?.UpdateCombo(noteInfo);
            note.OnDamage = noteInfo => scoreLogic?.Damage(noteInfo);
            note.OnUpdateResult = resultTuple => scoreLogic != null ? scoreLogic.UpdateResult(resultTuple) : resultTuple;
        }

        // ノーツの結果を ScoreLogic へ渡し、集計と判定通知を更新する。
        public void UpdateNoteResult(NoteBase note)
        {
            scoreLogic?.UpdateNoteResult(note);
        }

        // 計算器変更、初期化、得点・ゲージ・コンボ・判定の各通知を表示側へ接続する。
        private void AttachScoreLogicEvents(ScoreLogic logic)
        {
            if (logic == null)
            {
                return;
            }

            logic.OnScoreCalculatorChanged += HandleScoreCalculatorChanged;
            logic.OnScoreSetup += HandleScoreSetup;
            logic.OnScoreUpdated += HandleScoreUpdated;
            logic.OnClearGaugeUpdated += HandleClearGaugeUpdated;
            logic.OnComboUpdated += HandleComboUpdated;
            logic.OnNoteJudged += HandleScoreNoteJudged;
        }

        // 差し替えや終了時に AttachScoreLogicEvents と対になる購読をすべて解除する。
        private void DetachScoreLogicEvents(ScoreLogic logic)
        {
            if (logic == null)
            {
                return;
            }

            logic.OnScoreCalculatorChanged -= HandleScoreCalculatorChanged;
            logic.OnScoreSetup -= HandleScoreSetup;
            logic.OnScoreUpdated -= HandleScoreUpdated;
            logic.OnClearGaugeUpdated -= HandleClearGaugeUpdated;
            logic.OnComboUpdated -= HandleComboUpdated;
            logic.OnNoteJudged -= HandleScoreNoteJudged;
        }

        private void HandleScoreCalculatorChanged(ScoreGaugeCalculator calculator)
        {
            presentationRoot.ChangeScoreCalculator(calculator);
        }

        private void HandleScoreSetup(LiveScore score)
        {
            presentationRoot.SetupScore(score);
        }

        private void HandleScoreUpdated(LiveScore score, int addScore)
        {
            presentationRoot.UpdateScore(score, addScore);
        }

        private void HandleClearGaugeUpdated(int clearGauge)
        {
            presentationRoot.UpdateClearGauge(clearGauge);
        }

        private void HandleComboUpdated(LiveScore score)
        {
            presentationRoot.UpdateCombo(score);
        }

        private void HandleScoreNoteJudged(INote note)
        {
            presentationRoot.JudgmentNote(note);
        }
    }
}
