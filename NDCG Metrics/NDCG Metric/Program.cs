internal class Program
{
    private static void Main(string[] args)
    {
        var dataset = new List<List<int>>
            {
                new List<int> { 3, 1, 2, 0, 2 },
                new List<int> { 1,4, 0, 1, 2 },  //! understand how this calculation works
                new List<int> { 0,1, 2, 1, 0 }
            };

        Console.WriteLine("NDCG@5: " + Ndcg(dataset, 5));
        for (int i = 0; i < dataset.Count; i++)
        {
            dataset[i] = dataset[i].OrderBy(x => x).ToList();
            Console.WriteLine("\nWorst NDCG@5: " + Ndcg(dataset, 5));
            dataset[i] = dataset[i].OrderByDescending(x => x).ToList();
            Console.WriteLine("Best NDCG@5: " + Ndcg(dataset, 5));
        }
      
        Console.ReadKey();
    }


    public static double Ndcg(List<List<int>> rankedDataset, int p, List<int> customGain = null)
    {
        if (!rankedDataset.Any())
        {
            return 0;
        }

        var gain = customGain;
        if (gain == null)
        {
            gain = new int[p].ToList();
            for (int i = 0; i < p; ++i)
            {
                gain[i] = (int)Math.Pow(2, i) - 1;
            }
        }

        double ndcgSum = 0;
        int count = 0;
        foreach (var list in rankedDataset)
        {
            double dcg = 0;
            for (var i = 0; i < list.Count && i < p; i++)
            {
                dcg += gain[list[i]] / Math.Log(i + 2);
            }

            double idcg = 0;
            var optimalOrder = list.OrderByDescending(x => x).ToList();
            for (var i = 0; i < optimalOrder.Count && i < p; i++)
            {
                idcg += gain[optimalOrder[i]] / Math.Log(i + 2);
            }
            if (Math.Abs(idcg) < 0.000001)
            {
                continue;
            }

            ++count;
            ndcgSum += dcg / idcg;
        }
        return ndcgSum / count;
    }
}