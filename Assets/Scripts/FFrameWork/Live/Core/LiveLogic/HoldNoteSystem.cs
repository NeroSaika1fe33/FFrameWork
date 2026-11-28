using System;
using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    // 継続入力を要するノーツと、そのホールド状態に連動する音響効果の寿命を管理する。
    internal sealed class HoldNoteSystem
    {
        private readonly List<NoteBase> activeHoldNoteList = new List<NoteBase>(16);
        private readonly List<NoteBase> activeAudioEffectNoteList = new List<NoteBase>(16);

        private PresentationRoot presentationRoot;

        // ホールド状態の変化を通知する音響・表示側の窓口を設定する。
        public void SetPresentationRoot(PresentationRoot presentationRoot)
        {
            this.presentationRoot = presentationRoot;
        }

        // リトライや再初期化に備え、音響効果を終了して追跡一覧を空にする。
        public void Clear()
        {
            EndAllTrackedAudioEffects();
            activeHoldNoteList.Clear();
            activeAudioEffectNoteList.Clear();
        }

        // 対象の長押しノーツだけを重複なく登録する。固定レーンの長押しはこの一覧では扱わない。
        public void TrackIfHoldNote(NoteBase note)
        {
            if (!IsHoldNote(note) || activeHoldNoteList.Contains(note))
            {
                return;
            }

            activeHoldNoteList.Add(note);
        }

        // ノーツに紐付く音響効果を終了してから、継続入力の追跡対象から外す。
        public void Remove(NoteBase note)
        {
            EndTrackedAudioEffect(note);
            activeHoldNoteList.Remove(note);
        }

        // 既に有効なホールドについて、現在の枠・入力・楽曲時刻で継続条件を再確認する。
        // 入力が途切れた効果と終了済みノーツを整理し、新規ホールドの判定は後続の更新で行う。
        public void RefreshActiveHoldNotes(
            bool isAutoMode,
            bool hasArrowLogic,
            ArrowJudgmentFrame frame,
            float musicTime,
            float sizeDelta,
            bool isFreeLanePressed,
            bool hasInputManager)
        {
            if (activeHoldNoteList.Count == 0) return;

            for (int i = activeHoldNoteList.Count - 1; i >= 0; i--)
            {
                NoteBase note = activeHoldNoteList[i];
                if (note.State == NoteState.Done)
                {
                    EndTrackedAudioEffect(note);
                    activeHoldNoteList.RemoveAt(i);
                    continue;
                }

                if (note is SliderLongNote slider && hasArrowLogic)
                {
                    bool wasActive = slider.IsHoldJudgmentActive;
                    if (!wasActive) continue;

                    slider.RefreshArrowHoldState(frame, musicTime);
                    EndLongEffectIfInactive(wasActive, slider.IsHoldJudgmentActive, slider);
                }
                else if (note is FreeLaneLongNote freeLane && !isAutoMode && hasInputManager && hasArrowLogic)
                {
                    bool wasActive = freeLane.IsHoldJudgmentActive;
                    if (!wasActive) continue;

                    freeLane.RefreshFreeLaneHoldState(frame, musicTime, isFreeLanePressed);
                    EndLongEffectIfInactive(wasActive, freeLane.IsHoldJudgmentActive, freeLane);
                }
                else if (note is VolumeLongNote volume && hasInputManager && hasArrowLogic)
                {
                    bool wasActive = volume.IsHoldJudgmentActive;
                    if (!wasActive) continue;

                    volume.RefreshArrowVolumeHoldState(frame, musicTime, sizeDelta);
                    EndLongEffectIfInactive(wasActive, volume.IsHoldJudgmentActive, volume);
                }
            }
        }

        // 可動枠による長押し判定を更新し、有効化・継続・解除に合わせて音響効果を操作する。
        // VolumeLong では入力猶予を反映した effectiveSizeDelta を音響側にも渡す。
        public void UpdateActiveHoldInputs(
            bool isAutoMode,
            bool hasArrowLogic,
            ArrowJudgmentFrame frame,
            float musicTime,
            float sizeDelta,
            bool isFreeLanePressed,
            bool hasInputManager)
        {
            if (activeHoldNoteList.Count == 0 || !hasArrowLogic) return;

            for (int i = 0; i < activeHoldNoteList.Count; i++)
            {
                NoteBase note = activeHoldNoteList[i];
                if (note == null || note.State == NoteState.Done) continue;

                if (note is SliderLongNote slider)
                {
                    bool wasActive = slider.IsHoldJudgmentActive;
                    slider.TryUpdateByArrow(frame, musicTime);
                    UpdateSliderLongEffect(wasActive, slider);
                }
                else if (note is FreeLaneLongNote freeLane && !isAutoMode && hasInputManager)
                {
                    bool wasActive = freeLane.IsHoldJudgmentActive;
                    freeLane.TryUpdateFreeLaneHold(frame, musicTime, isFreeLanePressed);
                    UpdateFreeLaneLongEffect(wasActive, freeLane);
                }
                else if (note is VolumeLongNote volume && hasInputManager)
                {
                    bool wasActive = volume.IsHoldJudgmentActive;
                    volume.TryUpdateByArrowVolume(frame, musicTime, sizeDelta, out float effectiveSizeDelta);
                    UpdateVolumeLongEffect(wasActive, volume, effectiveSizeDelta);
                }
            }
        }

        // 自動判定前後のホールド状態を比較し、FreeLaneLong の音響効果を手動演奏と同様に制御する。
        public void UpdateFreeLaneLongAutoEffect(NoteBase note, bool wasActive)
        {
            if (!(note is FreeLaneLongNote freeLaneLongNote)) return;

            bool isActive = freeLaneLongNote.IsHoldJudgmentActive;
            bool isDone = freeLaneLongNote.State == NoteState.Done;
            if (!wasActive && isActive && !isDone)
            {
                BeginTrackedAudioEffect(freeLaneLongNote, () => presentationRoot != null && presentationRoot.BeginFreeLaneLongEffect());
            }
            if (isActive && !isDone)
            {
                presentationRoot?.UpdateFreeLaneLongEffect();
            }
            if (isDone || (wasActive && !isActive))
            {
                EndTrackedAudioEffect(freeLaneLongNote);
            }
        }

        // ノーツ側で終了・解除された効果を再確認し、停止通知の取りこぼしを補う。
        public void ReconcileTrackedAudioEffects()
        {
            for (int i = activeAudioEffectNoteList.Count - 1; i >= 0; i--)
            {
                NoteBase note = activeAudioEffectNoteList[i];
                if (note == null)
                {
                    activeAudioEffectNoteList.RemoveAt(i);
                    continue;
                }

                if (note.State == NoteState.Done || !IsLongEffectActive(note))
                {
                    EndTrackedAudioEffect(note);
                }
            }
        }

        // 追跡中の全効果を終了し、パラメータを復元する。
        public void EndAllTrackedAudioEffects()
        {
            while (activeAudioEffectNoteList.Count > 0)
            {
                NoteBase note = activeAudioEffectNoteList[activeAudioEffectNoteList.Count - 1];
                if (note == null)
                {
                    activeAudioEffectNoteList.RemoveAt(activeAudioEffectNoteList.Count - 1);
                    continue;
                }

                EndTrackedAudioEffect(note);
            }
        }

        // 終端ノーツの結果にかかわらず、親の長押し音響効果を通常状態へ戻す。
        public void ForceEndLongAudioEffectByEndNote(NoteBase noteInfo)
        {
            // 終端の Miss / Pass も、成功時と同じようにパラメータを通常値へ戻す。
            if (noteInfo == null || !(noteInfo.ParentNote is NoteBase parentNote) ||
                !(noteInfo is FreeLaneLongEndNote || noteInfo is FixedLongEndNote ||
                  noteInfo is SliderLongEndNote || noteInfo is VolumeLongEndNote))
            {
                return;
            }

            activeAudioEffectNoteList.Remove(parentNote);
            EndAudioEffectByLongNote(parentNote);
        }

        // 横移動ホールドの状態変化から、音響効果の開始・更新・終了を決める。
        private void UpdateSliderLongEffect(bool wasActive, SliderLongNote note)
        {
            if (!wasActive && ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                BeginTrackedAudioEffect(note, () => presentationRoot != null && presentationRoot.BeginSliderLongEffect());
            }
            if (ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                presentationRoot?.UpdateSliderLongEffect();
            }
            EndLongEffectIfInactive(wasActive, note.IsHoldJudgmentActive, note);
        }

        // フリーレーンのホールド状態に合わせて音響効果を制御する。
        private void UpdateFreeLaneLongEffect(bool wasActive, FreeLaneLongNote note)
        {
            if (!wasActive && ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                BeginTrackedAudioEffect(note, () => presentationRoot != null && presentationRoot.BeginFreeLaneLongEffect());
            }
            if (ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                presentationRoot?.UpdateFreeLaneLongEffect();
            }
            EndLongEffectIfInactive(wasActive, note.IsHoldJudgmentActive, note);
        }

        // 幅操作の有効差分を使ってフィルターを制御する。開始時に差分が 0 なら正方向を使う。
        private void UpdateVolumeLongEffect(bool wasActive, VolumeLongNote note, float effectiveSizeDelta)
        {
            if (!wasActive && ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                float initialDirection = Mathf.Approximately(effectiveSizeDelta, 0f) ? 1f : effectiveSizeDelta;
                BeginTrackedAudioEffect(note, () => presentationRoot != null && presentationRoot.BeginVolumeLongEffect(initialDirection));
            }
            if (ShouldUpdateLongEffect(note.IsHoldJudgmentActive, note))
            {
                presentationRoot?.UpdateVolumeLongEffect(effectiveSizeDelta);
            }
            EndLongEffectIfInactive(wasActive, note.IsHoldJudgmentActive, note);
        }

        // ホールドが有効で、ノーツがまだ終了していない場合だけ効果を更新する。
        private static bool ShouldUpdateLongEffect(bool isActive, NoteBase note)
        {
            return isActive && note.State != NoteState.Done;
        }

        // ノーツ終了、または有効なホールドからの離脱を効果の終了条件とする。
        private static bool ShouldEndLongEffect(bool wasActive, bool isActive, NoteBase note)
        {
            return note.State == NoteState.Done || (wasActive && !isActive);
        }

        // ホールドが終了条件に入った場合に、追跡中の効果を終了する。
        private void EndLongEffectIfInactive(bool wasActive, bool isActive, NoteBase note)
        {
            if (ShouldEndLongEffect(wasActive, isActive, note))
            {
                EndTrackedAudioEffect(note);
            }
        }

        // 同じノーツの重複開始を防ぎ、開始処理より先に復元対象へ登録する。
        // 開始処理が失敗しても、一部のパラメータ変更を戻せるよう追跡を続ける。
        private void BeginTrackedAudioEffect(NoteBase note, Func<bool> beginEffect)
        {
            if (note == null || beginEffect == null || activeAudioEffectNoteList.Contains(note))
            {
                return;
            }

            // 一部のパラメータだけ反映された場合も、終了時の復元対象として保持する。
            activeAudioEffectNoteList.Add(note);
            beginEffect();
        }

        // 登録済みノーツだけを一覧から取り除き、種類に対応した終了処理を呼ぶ。
        private void EndTrackedAudioEffect(NoteBase note)
        {
            if (note == null)
            {
                return;
            }

            int index = activeAudioEffectNoteList.IndexOf(note);
            if (index < 0)
            {
                return;
            }

            activeAudioEffectNoteList.RemoveAt(index);
            EndAudioEffectByLongNote(note);
        }

        // ノーツ種類ごとのホールド状態を読み、効果が継続可能かを判定する。
        private static bool IsLongEffectActive(NoteBase note)
        {
            if (note is SliderLongNote slider)
            {
                return slider.IsHoldJudgmentActive;
            }
            if (note is FreeLaneLongNote freeLane)
            {
                return freeLane.IsHoldJudgmentActive;
            }
            if (note is VolumeLongNote volume)
            {
                return volume.IsHoldJudgmentActive;
            }

            return false;
        }

        // ノーツ種類に応じた音響パラメータの復元を依頼する。
        private void EndAudioEffectByLongNote(NoteBase note)
        {
            if (note is SliderLongNote)
            {
                presentationRoot?.EndSliderLongEffect();
            }
            else if (note is FreeLaneLongNote)
            {
                presentationRoot?.EndFreeLaneLongEffect();
            }
            else if (note is VolumeLongNote)
            {
                presentationRoot?.EndVolumeLongEffect();
            }
        }

        // 可動枠による継続判定を持つ 3 種類のノーツを識別する。
        private static bool IsHoldNote(NoteBase note)
        {
            return note is SliderLongNote || note is FreeLaneLongNote || note is VolumeLongNote;
        }
    }
}
