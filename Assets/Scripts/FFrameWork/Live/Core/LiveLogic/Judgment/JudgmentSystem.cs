using System.Collections.Generic;
using UnityEngine;

namespace FFrameWork.Live
{
    // 入力とノーツの候補を絞り込み、同じ物理入力の重複消費を防いで判定する。
    internal sealed class JudgmentSystem
    {
        private const int CandidateCapacity = 128;

        private readonly List<int> physicalInputKeyList = new List<int>(CandidateCapacity);
        private readonly List<InputTmp> physicalInputBestInputList = new List<InputTmp>(CandidateCapacity);
        private readonly List<NoteBase> physicalInputBestNoteList = new List<NoteBase>(CandidateCapacity);
        private readonly LiveBundleBuildData liveBundleBuildData;

        private List<NoteBase> judgmentNoteList;
        private List<InputTmp> activeInputList;
        private MusicScoreInfo currentFrameInfo;
        private PresentationRoot presentationRoot;

        public JudgmentSystem(LiveBundleBuildData liveBundleBuildData)
        {
            this.liveBundleBuildData = liveBundleBuildData;
        }

        // 時刻順の候補作成、入力間の競合解消、物理入力単位の選別、実判定の順に処理する。
        // 渡された候補リストを直接更新し、処理後に activeInputList を空にする。
        public void Execute(
            List<NoteBase> judgmentNoteList,
            List<InputTmp> activeInputList,
            MusicScoreInfo currentFrameInfo,
            PresentationRoot presentationRoot)
        {
            this.judgmentNoteList = judgmentNoteList;
            this.activeInputList = activeInputList;
            this.currentFrameInfo = currentFrameInfo;
            this.presentationRoot = presentationRoot;

            SortJudgmentNotesByTime();
            AllocateInputJudgableEachNotes();
            OverlapJudgmentSorting();
            LeaveNotesFastGeneration();
            LeavePhysicalInputCandidates();
            EvaluateJudgment();
        }

        // 候補を譜面時刻の昇順へ安定ソートし、同時刻の元の順序を保つ。
        private void SortJudgmentNotesByTime()
        {
            for (int i = 1; i < judgmentNoteList.Count; i++)
            {
                NoteBase key = judgmentNoteList[i];
                int j = i - 1;
                while (j >= 0 && judgmentNoteList[j].MusicScoreInfo.time > key.MusicScoreInfo.time)
                {
                    judgmentNoteList[j + 1] = judgmentNoteList[j];
                    j--;
                }
                judgmentNoteList[j + 1] = key;
            }
        }

        // 入力元に対応する種類だけを対象に、レーンと時間条件を満たすノーツを入力の候補へ追加する。
        private void AllocateInputJudgableEachNotes()
        {
            for (int i = 0; i < judgmentNoteList.Count; i++)
            {
                NoteBase judgmentNote = judgmentNoteList[i];
                if (judgmentNote == null) continue;

                for (int j = activeInputList.Count - 1; j >= 0; j--)
                {
                    InputTmp activeInput = activeInputList[j];
                    if (!CanJudgeByInputSource(activeInput.liveInput.source, judgmentNote)) continue;

                    float lane = ConvertLaneBy(judgmentNote, activeInput);
                    if (lane >= 0f && judgmentNote.IsJudgment(ref activeInput.liveInput, lane))
                    {
                        activeInput.CandidateNotes.Add(judgmentNote);
                    }
                }
            }
        }

        // 同じノーツを複数入力が候補に持つ場合、レーン距離の近い入力側へ割り当てる。
        private void OverlapJudgmentSorting()
        {
            for (int i = activeInputList.Count - 1; i >= 0; i--)
            {
                InputTmp input = activeInputList[i];
                for (int k = activeInputList.Count - 1; k >= 0; k--)
                {
                    if (i == k) continue;

                    InputTmp other = activeInputList[k];
                    for (int j = input.CandidateNotes.Count - 1; j >= 0; j--)
                    {
                        NoteBase note = input.CandidateNotes[j];
                        if (!other.CandidateNotes.Contains(note)) continue;

                        float inputDistance = note.LaneDistance(ref currentFrameInfo, ConvertLaneBy(note, input));
                        float otherDistance = note.LaneDistance(ref currentFrameInfo, ConvertLaneBy(note, other));
                        if (inputDistance <= otherDistance)
                        {
                            other.CandidateNotes.Remove(note);
                        }
                        else
                        {
                            input.CandidateNotes.Remove(note);
                        }
                    }
                }
            }
        }

        // 各入力の候補を優先度で比較し、一つの判定先に絞る。
        private void LeaveNotesFastGeneration()
        {
            for (int i = activeInputList.Count - 1; i >= 0; i--)
            {
                InputTmp input = activeInputList[i];
                for (int j = input.CandidateNotes.Count - 1; j >= 0; j--)
                {
                    if (input.CandidateNotes.Count < 2) continue;

                    NoteBase firstCandidate = input.CandidateNotes[0];
                    NoteBase candidate = input.CandidateNotes[j];
                    bool removeCandidate = CompareCandidatePriority(firstCandidate, input, candidate, input) >= 0;

                    if (removeCandidate)
                    {
                        input.CandidateNotes.Remove(candidate);
                    }
                    else
                    {
                        input.CandidateNotes.Remove(firstCandidate);
                        j = input.CandidateNotes.Count;
                    }
                }
            }
        }

        // 進行度の大きさ、レーン距離の近さ、譜面時刻の早さ、ID の小ささの順に比較する。
        // 戻り値が正なら左候補、負なら右候補を優先する。
        private int CompareCandidatePriority(NoteBase leftCandidate, InputTmp leftInput, NoteBase rightCandidate, InputTmp rightInput)
        {
            NoteBase left = SelectLeaveComparableNote(leftCandidate, leftInput, leftCandidate);
            NoteBase right = SelectLeaveComparableNote(rightCandidate, rightInput, rightCandidate);

            float progressDiff = left.Progress - right.Progress;
            if (!Mathf.Approximately(progressDiff, 0f))
            {
                return progressDiff > 0f ? 1 : -1;
            }

            float leftDistance = left.LaneDistance(ref currentFrameInfo, ConvertLaneBy(left, leftInput));
            float rightDistance = right.LaneDistance(ref currentFrameInfo, ConvertLaneBy(right, rightInput));
            float distanceDiff = rightDistance - leftDistance;
            if (!Mathf.Approximately(distanceDiff, 0f))
            {
                return distanceDiff > 0f ? 1 : -1;
            }

            if (!left.MusicScoreInfo.time.Equals(right.MusicScoreInfo.time))
            {
                return left.MusicScoreInfo.time < right.MusicScoreInfo.time ? 1 : -1;
            }

            if (left.Id != right.Id)
            {
                return left.Id < right.Id ? 1 : -1;
            }

            return 0;
        }

        // 複合ノーツは未判定の押下時に先頭、それ以外は末尾を比較する。
        // その子ノーツを判定できない場合は元の候補へ戻す。
        private NoteBase SelectLeaveComparableNote(NoteBase note, InputTmp input, NoteBase fallbackNote)
        {
            if (note.NoteList.Count == 1) return note;

            NoteBase comparable;
            if (note.Result == NoteResult.None && input.liveInput.phase == LiveInputPhase.Began)
            {
                comparable = note.NoteList[0];
            }
            else
            {
                comparable = note.NoteList[note.NoteList.Count - 1];
            }

            float lane = ConvertLaneBy(comparable, input);
            return comparable.IsJudgment(ref input.liveInput, lane) ? comparable : fallbackNote;
        }

        // 複数レーンに展開された入力を物理入力キーでまとめ、グループ全体で最優先の組だけを残す。
        private void LeavePhysicalInputCandidates()
        {
            physicalInputKeyList.Clear();
            physicalInputBestInputList.Clear();
            physicalInputBestNoteList.Clear();

            for (int i = activeInputList.Count - 1; i >= 0; i--)
            {
                InputTmp input = activeInputList[i];
                for (int j = input.CandidateNotes.Count - 1; j >= 0; j--)
                {
                    NoteBase candidate = input.CandidateNotes[j];
                    if (candidate == null) continue;

                    int physicalInputKey = GetPhysicalInputKey(input);
                    int index = physicalInputKeyList.IndexOf(physicalInputKey);
                    if (index < 0)
                    {
                        physicalInputKeyList.Add(physicalInputKey);
                        physicalInputBestInputList.Add(input);
                        physicalInputBestNoteList.Add(candidate);
                        continue;
                    }

                    if (CompareCandidatePriority(candidate, input, physicalInputBestNoteList[index], physicalInputBestInputList[index]) > 0)
                    {
                        physicalInputBestInputList[index] = input;
                        physicalInputBestNoteList[index] = candidate;
                    }
                }
            }

            if (physicalInputKeyList.Count == 0) return;

            for (int i = activeInputList.Count - 1; i >= 0; i--)
            {
                InputTmp input = activeInputList[i];
                int index = physicalInputKeyList.IndexOf(GetPhysicalInputKey(input));
                InputTmp bestInput = index >= 0 ? physicalInputBestInputList[index] : null;
                NoteBase bestNote = index >= 0 ? physicalInputBestNoteList[index] : null;

                for (int j = input.CandidateNotes.Count - 1; j >= 0; j--)
                {
                    if (input == bestInput && input.CandidateNotes[j] == bestNote) continue;
                    input.CandidateNotes.RemoveAt(j);
                }
            }
        }

        // 入力元を上位ビットに含め、固定キーは inputId、可動枠系は入力元単位で同一操作として扱う。
        private static int GetPhysicalInputKey(InputTmp input)
        {
            if (input == null) return 0;

            int sourceKey = ((int)input.liveInput.source + 1) << 16;
            switch (input.liveInput.source)
            {
                case LiveInputSource.FixedLane:
                    return sourceKey + input.liveInput.inputId;
                case LiveInputSource.FreeLane:
                case LiveInputSource.Arrow:
                case LiveInputSource.ArrowVolume:
                    return sourceKey;
                default:
                    return sourceKey + input.liveInput.inputId;
            }
        }

        // 確定候補を判定した後、固定レーン入力をタップ演出へ通知し、一時候補を消去する。
        private void EvaluateJudgment()
        {
            for (int i = 0; i < activeInputList.Count; i++)
            {
                InputTmp input = activeInputList[i];
                if (input.CandidateNotes.Count > 0)
                {
                    for (int j = 0; j < input.CandidateNotes.Count; j++)
                    {
                        NoteBase note = input.CandidateNotes[j];
                        float lane = ConvertLaneBy(note, input);
                        note.Judgment(ref input.liveInput, lane);
                        judgmentNoteList.Remove(note);
                    }
                    input.CandidateNotes.Clear();
                }
            }

            for (int i = 0; i < activeInputList.Count; i++)
            {
                InputTmp input = activeInputList[i];
                if (input.Lane >= 0f && input.liveInput.source == LiveInputSource.FixedLane)
                {
                    presentationRoot?.Unpicked(Mathf.RoundToInt(input.Lane), ref input.liveInput);
                }
            }

            activeInputList.Clear();
        }

        // 演奏中の SliderFlick はワールド座標からレーンを再計算し、その他は収集済みのレーンを使う。
        private float ConvertLaneBy(NoteBase note, InputTmp input)
        {
            if (note.Category != NoteCategory.SliderFlick || note.State != NoteState.Playing)
            {
                return input.Lane;
            }
            return GetJudgmentLane(input.liveInput.worldPosition, !note.FinishFirstAction);
        }

        // 判定位置間を補間して小数レーンを返す。許容範囲外や判定位置未設定の場合は -1 を返す。
        private float GetJudgmentLane(Vector3 touchPosition, bool checkJudgmentY)
        {
            Vector2[] judgmentPositions = LiveConfig.JudgmentPositions;
            if (judgmentPositions == null || judgmentPositions.Length == 0) return -1f;

            float offsetX = liveBundleBuildData != null ? liveBundleBuildData.JudgmentOffsetX : 1.8f;
            float offsetY = liveBundleBuildData != null ? liveBundleBuildData.JudgmentOffsetY : 2.5f;
            if (checkJudgmentY && offsetY < touchPosition.y - judgmentPositions[0].y) return -1f;
            if (offsetX < judgmentPositions[0].x - touchPosition.x) return -1f;
            if (offsetX < touchPosition.x - judgmentPositions[judgmentPositions.Length - 1].x) return -1f;

            for (int i = 0; i < judgmentPositions.Length; i++)
            {
                if (touchPosition.x - judgmentPositions[i].x <= 0f)
                {
                    if (i == 0) return 0f;
                    return (touchPosition.x - judgmentPositions[i - 1].x) / (judgmentPositions[i].x - judgmentPositions[i - 1].x) + i - 1;
                }
            }
            return judgmentPositions.Length - 1;
        }

        // 入力元とノーツ種類の組合せを制限する。SliderLong と VolumeFlickLong は HoldNoteSystem 側で扱う。
        private static bool CanJudgeByInputSource(LiveInputSource source, NoteBase note)
        {
            if (note == null) return false;
            if (note.Category == NoteCategory.SliderLong || note.Category == NoteCategory.VolumeFlickLong)
            {
                return false;
            }

            bool isFixedLaneNote = note.Category == NoteCategory.FixedNormal || note.Category == NoteCategory.FixedLong;
            switch (source)
            {
                case LiveInputSource.FixedLane:
                    return isFixedLaneNote;
                case LiveInputSource.Arrow:
                    return note.Category == NoteCategory.SliderFlick;
                case LiveInputSource.ArrowVolume:
                    return note.Category == NoteCategory.VolumeFlick;
                case LiveInputSource.FreeLane:
                    return !isFixedLaneNote && note.Category != NoteCategory.SliderFlick && note.Category != NoteCategory.VolumeFlick;
                default:
                    return false;
            }
        }
    }
}
