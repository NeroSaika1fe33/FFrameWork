using FFrameWork.Live;

namespace FFrameWork.Songs
{
    // Inspector の選曲設定を、演奏側で扱う共通の楽曲データへ変換する。
    public static class SongCatalog
    {
        // 選曲設定から譜面本文を解決し、楽曲・FMOD イベント・判定設定を転記する。
        // entry が null なら null。モードとカテゴリは Free / Default、その他の未転記項目は既定値となる。
        public static SongCatalogItem CreateItem(SongSelectEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            return new SongCatalogItem
            {
                MusicId = entry.MusicId,
                Title = entry.Title,
                Artist = entry.Artist,
                Composer = entry.Composer,
                Jacket = entry.Jacket,
                ScoreText = entry.GetSusScoreText(),
                MusicEvent = entry.MusicEvent,
                Difficulty = entry.Difficulty,
                PlayLevel = entry.PlayLevel,
                TotalNoteCount = entry.TotalNoteCount,
                FillerSec = entry.FillerSec,
                InitialArrowLaneWidth = entry.InitialArrowLaneWidth,
                BundleBuildData = entry.BundleBuildData,
                VocalId = 1,
                LivePlayMode = LivePlayMode.Free,
                MusicCategory = MusicCategory.Default
            };
        }
    }
}
