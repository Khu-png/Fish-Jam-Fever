using System.Collections.Generic;
using System.IO;
using FishSort;
using UnityEditor;
using UnityEngine;

// Creates sample LevelData assets (ported from the HTML preview generator).
public static class LevelDataGenerator
{
    private const string MENU_PATH = "Tools/FishSort/Generate Sample Levels";
    private const string OUTPUT_FOLDER = "Assets/_Game/ScriptableObject/Levels";
    private const string FILE_FORMAT = "Level_{0:00}.asset";
    private const int TANK_CAPACITY = 3;
    private const int ICE_TAPS = 3;
    private const int MIN_BUBBLE_FISH = 2;
    private const int BUBBLE_FISH_RANGE = 4;
    private const int ICE_ROW = 1;
    private const float ICE_DEPTH_PENALTY = 2f;
    private const float DEPTH_NOISE = 0.6f;

    private struct Recipe
    {
        public int types, total, columns, ice, seed;
        public Recipe(int types, int total, int columns, int ice, int seed)
        {
            this.types = types; this.total = total; this.columns = columns; this.ice = ice; this.seed = seed;
        }
    }

    private static readonly Recipe[] Recipes =
    {
        new Recipe(7, 33, 4, 0, 21),
        new Recipe(9, 54, 4, 1, 33),
        new Recipe(12, 120, 5, 2, 57)
    };

    // Ten gentle levels (few types, few fish, hardly any ice) that are played before the first sample level.
    private const string EASY_MENU_PATH = "Tools/FishSort/Generate Easy Levels";
    private const int FIRST_EASY_NUMBER = 5;
    private static readonly Recipe[] EasyRecipes =
    {
        new Recipe(3, 12, 3, 0, 101),
        new Recipe(4, 15, 3, 0, 102),
        new Recipe(4, 18, 4, 0, 103),
        new Recipe(5, 18, 4, 0, 104),
        new Recipe(5, 21, 4, 0, 105),
        new Recipe(6, 24, 4, 0, 106),
        new Recipe(6, 27, 4, 0, 107),
        new Recipe(6, 27, 4, 1, 108),
        new Recipe(7, 30, 4, 0, 109),
        new Recipe(7, 30, 4, 1, 110)
    };

    public static int EasyLevelCount => EasyRecipes.Length;

    // Writes Level_05 .. Level_14 (the other level assets are not touched) and returns where Level_05 is.
    [MenuItem(EASY_MENU_PATH)]
    public static void GenerateEasyLevels()
    {
        Directory.CreateDirectory(OUTPUT_FOLDER);
        for (int i = 0; i < EasyRecipes.Length; i++)
        {
            Generate(EasyRecipes[i], out BubbleData[] bubbles, out int[] sequence);
            SaveLevel(FIRST_EASY_NUMBER + i, EasyRecipes[i].columns, bubbles, sequence);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("FishSort: easy levels written to " + OUTPUT_FOLDER);
    }

    public static string EasyLevelPath(int index)
    {
        return OUTPUT_FOLDER + "/" + string.Format(FILE_FORMAT, FIRST_EASY_NUMBER + index);
    }

    [MenuItem(MENU_PATH)]
    private static void GenerateAll()
    {
        Directory.CreateDirectory(OUTPUT_FOLDER);
        SaveLevel(1, 3, TutorialBubbles(), new[] { 2, 1, 0 });
        for (int i = 0; i < Recipes.Length; i++)
        {
            Generate(Recipes[i], out BubbleData[] bubbles, out int[] sequence);
            SaveLevel(i + 2, Recipes[i].columns, bubbles, sequence);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("FishSort: sample levels written to " + OUTPUT_FOLDER);
    }

    private static BubbleData[] TutorialBubbles()
    {
        return new[]
        {
            new BubbleData { column = 0, fishTypes = new[] { 2, 2, 2 } },
            new BubbleData { column = 1, fishTypes = new[] { 1, 1, 1 } },
            new BubbleData { column = 2, fishTypes = new[] { 0, 0, 0 } }
        };
    }

    private static void SaveLevel(int number, int columns, BubbleData[] bubbles, int[] sequence)
    {
        string path = OUTPUT_FOLDER + "/" + string.Format(FILE_FORMAT, number);
        LevelData asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<LevelData>();
            asset.Initialize(columns, bubbles, sequence);
            AssetDatabase.CreateAsset(asset, path);
        }
        else
        {
            asset.Initialize(columns, bubbles, sequence);
            EditorUtility.SetDirty(asset);
        }

        if (!asset.Validate(TANK_CAPACITY, out string error))
        {
            Debug.LogError(path + ": " + error, asset);
        }
    }

    private static void Generate(Recipe recipe, out BubbleData[] bubbles, out int[] sequence)
    {
        System.Random rng = new System.Random(recipe.seed);
        int[] counts = new int[recipe.types];
        for (int t = 0; t < recipe.types; t++)
        {
            counts[t] = TANK_CAPACITY;
        }

        for (int left = recipe.total - recipe.types * TANK_CAPACITY; left > 0; left -= TANK_CAPACITY)
        {
            counts[rng.Next(recipe.types)] += TANK_CAPACITY;
        }

        List<int> pool = new List<int>(recipe.total);
        for (int t = 0; t < recipe.types; t++)
        {
            for (int k = 0; k < counts[t]; k++)
            {
                pool.Add(t);
            }
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int swap = pool[i];
            pool[i] = pool[j];
            pool[j] = swap;
        }

        List<BubbleData> list = new List<BubbleData>();
        List<int> rows = new List<int>();
        while (pool.Count > 0)
        {
            int size = Mathf.Min(pool.Count, MIN_BUBBLE_FISH + rng.Next(BUBBLE_FISH_RANGE));
            if (pool.Count - size == 1)
            {
                size = size < FishSortConst.MAX_FISH_PER_BUBBLE ? size + 1 : size - 1;
            }

            List<int> fish = new List<int>(size);
            for (int k = 0; k < size; k++)
            {
                int pickIndex = pool.FindIndex(type => !fish.Contains(type));
                pickIndex = pickIndex < 0 ? 0 : pickIndex;
                fish.Add(pool[pickIndex]);
                pool.RemoveAt(pickIndex);
            }

            int index = list.Count;
            list.Add(new BubbleData { column = index % recipe.columns, fishTypes = fish.ToArray() });
            rows.Add(index / recipe.columns);
        }

        List<int> iceCandidates = new List<int>();
        for (int i = 0; i < list.Count; i++)
        {
            if (rows[i] == ICE_ROW && list[i].fishTypes.Length >= MIN_BUBBLE_FISH)
            {
                iceCandidates.Add(i);
            }
        }

        for (int k = 0; k < recipe.ice && iceCandidates.Count > 0; k++)
        {
            int pick = rng.Next(iceCandidates.Count);
            BubbleData frozen = list[iceCandidates[pick]];
            frozen.iceTaps = ICE_TAPS;
            list[iceCandidates[pick]] = frozen;
            iceCandidates.RemoveAt(pick);
        }

        bubbles = list.ToArray();
        sequence = BuildSequence(bubbles, rows, recipe.types, rng);
    }

    // A type becomes a goal once its next 3 fish are reachable (shallow first, frozen counts deeper).
    private static int[] BuildSequence(BubbleData[] bubbles, List<int> rows, int typeCount, System.Random rng)
    {
        List<KeyValuePair<float, int>> depthList = new List<KeyValuePair<float, int>>();
        for (int i = 0; i < bubbles.Length; i++)
        {
            float depth = rows[i] + (bubbles[i].iceTaps > 0 ? ICE_DEPTH_PENALTY : 0f);
            for (int k = 0; k < bubbles[i].fishTypes.Length; k++)
            {
                depthList.Add(new KeyValuePair<float, int>(depth + (float)rng.NextDouble() * DEPTH_NOISE, bubbles[i].fishTypes[k]));
            }
        }

        depthList.Sort((a, b) => a.Key.CompareTo(b.Key));
        int[] seen = new int[typeCount];
        List<int> sequence = new List<int>();
        for (int i = 0; i < depthList.Count; i++)
        {
            int type = depthList[i].Value;
            seen[type]++;
            if (seen[type] % TANK_CAPACITY == 0)
            {
                sequence.Add(type);
            }
        }

        return sequence.ToArray();
    }
}
