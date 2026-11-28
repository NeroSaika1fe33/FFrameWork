using System.Collections.Generic;

namespace FFrameWork.Live
{
    // 判定フレーム内で再利用する入力と候補ノーツの作業領域。
    internal sealed class InputTmp
    {
        // 入力時刻・フェーズ・入力元など、今回の判定に使う入力値。
        public LiveInput liveInput;
        // 候補を調べるレーン位置。可動枠入力は laneStart / laneEnd に範囲を持つ。
        public float Lane;
        // この入力で判定可能なノーツ。選別処理で追加・削除される。
        public readonly List<NoteBase> CandidateNotes = new List<NoteBase>(4);

        // 入力値を既定状態にする。再利用時の候補消去は呼び出し側のフレーム処理が担う。
        public InputTmp()
        {
            liveInput = default;
        }
    }
}
