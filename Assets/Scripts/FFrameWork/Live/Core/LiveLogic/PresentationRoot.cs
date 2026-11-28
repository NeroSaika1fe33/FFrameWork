namespace FFrameWork.Live
{
    // 演奏ロジックから表示と音響への呼び出し窓口。画面更新は LiveViewPresenter、音響操作は LiveEffectPresenter に委譲する。
    internal sealed class PresentationRoot
    {
        private readonly LiveViewPresenter viewPresenter = new LiveViewPresenter();
        private readonly LiveEffectPresenter effectPresenter = new LiveEffectPresenter();

        // 演奏結果を通知する LiveView の一覧を設定する。
        public void SetLiveViews(LiveViewBase[] liveViews)
        {
            viewPresenter.SetLiveViews(liveViews);
        }

        // 長押し操作に連動する音響効果の制御先を設定する。
        public void SetAudioEffectController(AudioEffectController controller)
        {
            effectPresenter.SetAudioEffectController(controller);
        }

        // 譜面と表示先読み時間に基づく表示プールの準備を依頼する。
        public void CreateNotePool(MusicScore musicScore, float noteDisplayTimeOffset)
        {
            viewPresenter.CreateNotePool(musicScore, noteDisplayTimeOffset);
        }

        public void ChangeScoreCalculator(ScoreGaugeCalculator calculator)
        {
            viewPresenter.ChangeScoreCalculator(calculator);
        }

        // 計算側の初期スコアを各ビューへ通知する。
        public void SetupScore(LiveScore score)
        {
            viewPresenter.SetupScore(score);
        }

        public void UpdateScore(LiveScore score, int addScore)
        {
            viewPresenter.UpdateScore(score, addScore);
        }

        public void UpdateClearGauge(int clearGauge)
        {
            viewPresenter.UpdateClearGauge(clearGauge);
        }

        public void UpdateCombo(LiveScore score)
        {
            viewPresenter.UpdateCombo(score);
        }

        public void SpawnNote(INote note)
        {
            viewPresenter.SpawnNote(note);
        }

        public void UnspawnNote(INote note)
        {
            viewPresenter.UnspawnNote(note);
        }

        // 確定した判定結果を、判定表示や視覚効果へ渡す。
        public void JudgmentNote(INote note)
        {
            viewPresenter.JudgmentNote(note);
        }

        // 固定レーン入力をタップ演出へ通知する。ノーツの成否はここでは計算しない。
        public void Unpicked(int lane, ref LiveInput input)
        {
            viewPresenter.Unpicked(lane, ref input);
        }

        // 管理対象の音響効果を一括停止する。楽曲本体の停止とは別の処理。
        public void StopAllAudio()
        {
            effectPresenter.StopAllAudio();
        }

        public bool BeginSliderLongEffect()
        {
            return effectPresenter.BeginSliderLongEffect();
        }

        public void UpdateSliderLongEffect()
        {
            effectPresenter.UpdateSliderLongEffect();
        }

        public void EndSliderLongEffect()
        {
            effectPresenter.EndSliderLongEffect();
        }

        public bool BeginFreeLaneLongEffect()
        {
            return effectPresenter.BeginFreeLaneLongEffect();
        }

        public void UpdateFreeLaneLongEffect()
        {
            effectPresenter.UpdateFreeLaneLongEffect();
        }

        public void EndFreeLaneLongEffect()
        {
            effectPresenter.EndFreeLaneLongEffect();
        }

        // 幅操作の方向を音響側へ渡して効果を開始し、成否を返す。
        public bool BeginVolumeLongEffect(float direction)
        {
            return effectPresenter.BeginVolumeLongEffect(direction);
        }

        public void UpdateVolumeLongEffect(float sizeDelta)
        {
            effectPresenter.UpdateVolumeLongEffect(sizeDelta);
        }

        public void EndVolumeLongEffect()
        {
            effectPresenter.EndVolumeLongEffect();
        }
    }
}
