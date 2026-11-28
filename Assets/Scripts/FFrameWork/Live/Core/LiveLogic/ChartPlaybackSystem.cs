using System;
using System.Collections.Generic;

namespace FFrameWork.Live
{
    // 譜面時刻とノーツ走査位置を管理し、表示更新・手動判定候補・自動判定を進める。
    internal sealed class ChartPlaybackSystem
    {
        private MusicScore musicScore;
        private NoteBase[] noteArray = Array.Empty<NoteBase>();
        private int noteStartIndex;
        private float noteDisplayTimeOffset;
        private MusicScoreInfo currentFrameInfo;

        public MusicScore MusicScore => musicScore;
        public NoteBase[] NoteArray => noteArray;
        public MusicScoreInfo CurrentFrameInfo => currentFrameInfo;
        public bool HasMusicScore => musicScore != null;
        public bool IsNotesAllFinished => noteArray == null || noteStartIndex >= noteArray.Length;

        // 譜面と表示先読み時間を設定し、親ノーツと子ノーツを初期化する。
        // configureNote の第 2 引数は、親ノーツなら true、子ノーツなら false。
        public void Setup(MusicScore musicScore, float noteDisplayTimeOffset, Action<NoteBase, bool> configureNote)
        {
            this.musicScore = musicScore ?? new MusicScore();
            this.noteDisplayTimeOffset = noteDisplayTimeOffset;
            this.musicScore.Update(0f);
            currentFrameInfo = this.musicScore.CurrentFrameInfo;
            noteArray = this.musicScore.NoteArray ?? Array.Empty<NoteBase>();
            noteStartIndex = 0;

            for (int i = 0; i < noteArray.Length; i++)
            {
                NoteBase note = noteArray[i];
                if (note == null) continue;

                ConfigureNote(note, true, configureNote);

                if (note.NoteList == null) continue;
                for (int j = 0; j < note.NoteList.Count; j++)
                {
                    ConfigureNote(note.NoteList[j], false, configureNote);
                }
            }
        }

        // 楽曲時刻（秒）を譜面へ渡し、その時点の小節・BPM・速度情報を取得する。
        public void UpdateTime(float musicTime)
        {
            if (musicScore == null)
            {
                return;
            }

            musicScore.Update(musicTime);
            currentFrameInfo = musicScore.CurrentFrameInfo;
        }

        // 走査開始位置からノーツを進め、終了通知・実行後通知・判定候補の収集を行う。
        // 先頭から連続して終了したノーツだけ走査開始位置を進める。演奏中は表示開始条件にかかわらず更新する。
        public void UpdateNotes(
            List<NoteBase> judgmentNoteList,
            Action<NoteBase> onNoteDone,
            Action<NoteBase> onNoteExecuted)
        {
            for (int i = noteStartIndex; noteArray != null && i < noteArray.Length; i++)
            {
                NoteBase note = noteArray[i];
                if (note == null) continue;

                if (note.State == NoteState.Done)
                {
                    if (i == noteStartIndex) noteStartIndex = i + 1;
                    onNoteDone?.Invoke(note);
                    continue;
                }

                float timeOffset = CalcTimeOffset(note);
                if (timeOffset + currentFrameInfo.time + LiveUtility.OneFrameTime <= note.MusicScoreInfo.time)
                {
                    if (note.State != NoteState.Playing) continue;
                }

                note.Excute(currentFrameInfo, timeOffset);
                onNoteExecuted?.Invoke(note);

                if (note.State != NoteState.Done && note.HasJudgment)
                {
                    judgmentNoteList?.Add(note);
                }
            }
        }

        // 時刻に到達した未終了ノーツを自動判定する。配列は譜面時刻の昇順を前提とする。
        // 通知の bool 値は判定前の FreeLaneLong のホールド状態で、音響効果の切り替えに使う。
        public void ExecuteAutoJudgment(Action<NoteBase, bool> onAutoJudged)
        {
            for (int i = noteStartIndex; noteArray != null && i < noteArray.Length; i++)
            {
                NoteBase note = noteArray[i];
                if (note == null) continue;
                if (note.MusicScoreInfo.time > currentFrameInfo.time) break;
                if (note.State == NoteState.Done) continue;

                bool wasFreeLaneLongActive = note is FreeLaneLongNote freeLaneLongNote &&
                                             freeLaneLongNote.IsHoldJudgmentActive;
                note.AutoJudgment(currentFrameInfo);
                onAutoJudged?.Invoke(note, wasFreeLaneLongActive);
            }
        }

        // 表示先読み時間の計算関数をノーツへ渡し、判定・表示の接続処理を呼ぶ。
        private void ConfigureNote(NoteBase note, bool isRoot, Action<NoteBase, bool> configureNote)
        {
            if (note == null)
            {
                return;
            }

            note.CalcTimeOffset = CalcTimeOffset;
            configureNote?.Invoke(note, isRoot);
        }

        // 区間の平均速度倍率とノーツ固有倍率で、基準の表示先読み時間を補正する。
        // 区間速度が 0 の場合は先読み時間も 0 とする。
        private float CalcTimeOffset(NoteBase noteInfo)
        {
            if (noteInfo == null) return 0f;

            float currentProgress = currentFrameInfo.bar + currentFrameInfo.barProgress;
            float noteProgress = noteInfo.MusicScoreInfo.bar + noteInfo.MusicScoreInfo.barProgress;
            float speedRatio = CalcNoteSpeedRatio(currentProgress, noteProgress);

            if (speedRatio.Equals(0f)) return 0f;
            return noteDisplayTimeOffset / speedRatio / noteInfo.speedRatio;
        }

        // 現在位置からノーツ位置までの速度倍率を、小節進行量で重み付けして平均する。
        // 区間内の変速を反映し、到達済みの位置や速度情報がない場合は現在倍率を返す。
        private float CalcNoteSpeedRatio(float currentProgress, float noteProgress)
        {
            if (noteProgress <= currentProgress) return currentFrameInfo.speedRatio;
            MusicScoreInfo[] infos = musicScore?.musicScoreInfoArray;
            if (infos == null || infos.Length == 0) return currentFrameInfo.speedRatio;

            float weighted = 0f;
            float cursor = currentProgress;
            float ratio = currentFrameInfo.speedRatio;

            for (int i = 0; i < infos.Length; i++)
            {
                float eventProgress = infos[i].bar + infos[i].barProgress;
                if (eventProgress <= currentProgress)
                {
                    ratio = infos[i].speedRatio;
                    cursor = currentProgress;
                    continue;
                }
                if (eventProgress >= noteProgress) break;

                weighted += ratio * (eventProgress - cursor);
                cursor = eventProgress;
                ratio = infos[i].speedRatio;
            }

            weighted += ratio * (noteProgress - cursor);
            float length = noteProgress - currentProgress;
            return length.Equals(0f) ? currentFrameInfo.speedRatio : weighted / length;
        }
    }
}
