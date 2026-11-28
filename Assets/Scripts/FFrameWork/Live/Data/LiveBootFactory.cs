using UnityEngine;

namespace FFrameWork.Live
{
    // 選曲・プレビュー共通の楽曲情報から、演奏開始に必要なデータを組み立てる。
    public static class LiveBootFactory
    {
        // 楽曲情報、難易度、判定設定、音符速度を起動データへまとめる。song が null なら null を返す。
        // 総ノーツ数が正のときだけプレビュー用のランクしきい値を補完する。
        public static FreeLiveBootData CreateFreeLiveBootData(SongCatalogItem song, bool isAuto)
        {
            if (song == null)
            {
                return null;
            }

            string difficulty = GetDifficultyName(song.Difficulty);
            FreeLiveBootData bootData = new FreeLiveBootData(
                song.MusicId,
                difficulty,
                song.VocalId,
                0,
                song.LivePlayMode,
                song.MusicCategory);

            bootData.IsAuto = isAuto;
            bootData.canSkipDisplayMusicInfo = song.CanSkipDisplayMusicInfo;
            bootData.LiveSettingData = new LiveSettingData
            {
                NoteSpeed = LivePlayerSettings.NoteSpeed,
                TimingAdjustData = song.TimingAdjustData
            };
            bootData.BundleBuildData = song.BundleBuildData;
            bootData.InitialArrowLaneWidth = song.InitialArrowLaneWidth;

            LiveMusicData musicData = bootData.MusicData ?? new LiveMusicData();
            musicData.Music = new MasterMusic
            {
                id = song.MusicId,
                title = song.Title,
                artist = song.Artist,
                fillerSec = song.FillerSec
            };
            musicData.Difficulty = new MasterMusicDifficulty
            {
                id = song.MusicId * 10 + (int)song.Difficulty,
                musicId = song.MusicId,
                musicDifficulty = difficulty,
                playLevel = song.PlayLevel
            };
            if (song.TotalNoteCount > 0)
            {
                musicData.Score = CreatePreviewScore(song.PlayLevel, song.TotalNoteCount);
            }

            musicData.JacketTexture = song.Jacket;
            bootData.MusicData = musicData;
            return bootData;
        }

        // 音符速度を現在の共通設定で上書きする。曲固有のタイミング調整値は保持する。
        public static void ApplyGlobalPlayerSettings(LiveSettingData settings)
        {
            if (settings == null)
            {
                return;
            }

            settings.NoteSpeed = LivePlayerSettings.NoteSpeed;
        }

        // ノーツ数 × 1000 を基準にランクしきい値を作る。実際のプレイスコアを算出する処理ではない。
        // 0 以下のノーツ数は 1 として扱う。
        public static MasterPlayLevelScore CreatePreviewScore(int playLevel, int totalNoteCount)
        {
            int noteCount = Mathf.Max(1, totalNoteCount);
            int targetScore = noteCount * 1000;
            return new MasterPlayLevelScore
            {
                liveType = "free",
                playLevel = playLevel,
                s = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateS),
                a = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateA),
                b = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateB),
                c = Mathf.FloorToInt(targetScore * ScoreGaugeCalculator.RankRateC)
            };
        }

        // 難易度を小文字のデータ識別名へ変換する。未指定の None は easy とする。
        public static string GetDifficultyName(MusicDifficulty difficulty)
        {
            return difficulty == MusicDifficulty.None ? "easy" : difficulty.ToString().ToLowerInvariant();
        }
    }
}
