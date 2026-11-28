using FMODUnity;
using UnityEngine;

namespace FFrameWork.Live
{
    // 選曲画面とライブ起動処理で受け渡す楽曲データ。設定アセットの情報を共通の形式で保持する。
    public sealed class SongCatalogItem
    {
        public int MusicId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Composer { get; set; }
        public Texture2D Jacket { get; set; }
        // 読み込み済みの SUS 譜面本文。ファイルパスではない。
        public string ScoreText { get; set; }
        // 楽曲本体を再生する FMOD イベント参照。
        public EventReference MusicEvent { get; set; }
        public MusicDifficulty Difficulty { get; set; }
        public int PlayLevel { get; set; }
        // プレビュー用のランクしきい値を補完するための総ノーツ数。
        public int TotalNoteCount { get; set; }
        // 楽曲開始前の余白時間（秒）。演奏時間の基準合わせに使う。
        public float FillerSec { get; set; }
        // 開曲時の判定枠の幅。ピクセル数ではなくレーン数で指定する。
        public int InitialArrowLaneWidth { get; set; }
        // 判定・ゲージ・得点などのライブ調整値への参照。
        public LiveBundleBuildData BundleBuildData { get; set; }
        public int VocalId { get; set; }
        public LivePlayMode LivePlayMode { get; set; }
        public MusicCategory MusicCategory { get; set; }
        // 判定タイミングの調整値。実行時に秒へ換算される。
        public float TimingAdjustData { get; set; }
        // 楽曲情報表示のスキップ許可値。起動データへ渡す設定項目。
        public bool CanSkipDisplayMusicInfo { get; set; }
    }
}
