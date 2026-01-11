using System;
using System.Linq;
using System.Collections.Generic;
[Serializable]
public struct MMRData
{
    public int previousMatchUserScore;
    public double MMR;
    public ulong UserUniqueId;
}
public static class MMRSystem
{

    public static double baseK = 10.0;
    public static double ratingScale = 921.59840178;
    public static double marginScale = 0.41495;
    public static double upsetClamp = 1.0;
    public static double loserUpsetShare = 2.9333330419854824;

    public static MMRData[] ComputeMMR(MMRData[] players)
    {
        if (players == null || players.Length < 2)
            return players;


        int n = players.Length;

        var list = players
            .Select(p => new PlayerState
            {
                UniqueId = p.UserUniqueId,
                OldMMR = p.MMR,
                RawScore = p.previousMatchUserScore
            })
            .ToList();

        list.Sort((a, b) => b.RawScore.CompareTo(a.RawScore));

        AssignRanksAndTies(list);

        foreach (var p in list)
        {
            double sum = 0.0;
            foreach (var q in list)
            {
                if (p == q) continue;

                double e = 1.0 / (1.0 + Math.Pow(10.0, (q.OldMMR - p.OldMMR) / ratingScale));
                sum += e;
            }
            p.Expected = sum / (n - 1);
        }

        double minScore = list.Min(x => x.RawScore);
        double maxScore = list.Max(x => x.RawScore);
        bool useRawScores = maxScore != minScore;

        if (useRawScores)
        {
            foreach (var p in list)
                p.Observed = (p.RawScore - minScore) / (maxScore - minScore);
        }
        else
        {
            foreach (var p in list)
                p.Observed = (double)(n - p.Rank) / (n - 1);
        }

        foreach (var p in list)
        {
            double oppAvg = list.Where(x => x != p).Average(x => x.OldMMR);
            double ratingFactor = Math.Max(0.0, Math.Min(upsetClamp, (oppAvg - p.OldMMR) / ratingScale));

            double oppAvgScore = list.Where(x => x != p).Average(x => x.RawScore);
            double marginFactor = Math.Log(Math.Abs(p.RawScore - oppAvgScore) + 1.0) * marginScale;

            p.RatingFactor = ratingFactor;
            p.K = baseK * (1.0 + marginFactor + ratingFactor);
        }

        foreach (var p in list)
            p.RawDelta = p.K * (p.Observed - p.Expected);

        bool anyUpset = list.Any(x => x.Observed > x.Expected && x.RatingFactor > 0);
        if (anyUpset)
        {
            double strongest = list
                .Where(x => x.Observed > x.Expected)
                .Max(x => x.RatingFactor);

            double loseMult = 1.0 + loserUpsetShare * strongest;

            foreach (var p in list)
            {
                if (p.RawDelta < 0)
                    p.RawDelta *= loseMult;
            }
        }

        double sumDelta = list.Sum(p => p.RawDelta);
        double correction = sumDelta / n;
        foreach (var p in list)
            p.Delta = p.RawDelta - correction;

        MMRData[] output = new MMRData[n];
        for (int i = 0; i < n; i++)
        {
            var p = list[i];
            output[i] = new MMRData
            {
                UserUniqueId = p.UniqueId,
                previousMatchUserScore = p.RawScore,
                MMR = p.OldMMR + p.Delta
            };
        }

        return output;
    }

    private class PlayerState
    {
        public ulong UniqueId;
        public double OldMMR;
        public int RawScore;
        public int Rank;
        public double Expected;
        public double Observed;
        public double K;
        public double RatingFactor;
        public double RawDelta;
        public double Delta;
    }

    private static void AssignRanksAndTies(List<PlayerState> list)
    {
        int index = 0;
        while (index < list.Count)
        {
            int start = index;
            int score = list[index].RawScore;

            while (index < list.Count && list[index].RawScore == score)
                index++;

            int end = index - 1;

            double avgRank = (start + 1 + end + 1) / 2.0;

            for (int i = start; i <= end; i++)
                list[i].Rank = (int)Math.Round(avgRank);
        }
    }
}
