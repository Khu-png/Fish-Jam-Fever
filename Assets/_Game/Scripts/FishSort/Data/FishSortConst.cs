namespace FishSort
{
    // Shared constants: sorting orders, text formats and UI copy. No magic numbers/strings in gameplay code.
    public static class FishSortConst
    {
        public const int SORT_FISH_IN_TANK = 3;
        public const int SORT_FISH_IN_BUBBLE = 10;
        public const int SORT_FISH_HIGHLIGHT = 40;
        public const int SORT_GLOW_OFFSET = -1;
        public const int SORT_FISH_IN_TRAY = 55;
        public const int SORT_FISH_FLIGHT = 60;

        public const int TUTORIAL_LEVEL = 1;
        public const int FIRST_LEVEL = 1;
        public const int PERCENT = 100;
        public const int MAX_FISH_PER_BUBBLE = 5;

        public const string FORMAT_PROGRESS = "{0}/{1}";
        public const string FORMAT_LEVEL = "Level {0}";
        public const string FORMAT_LOSE_PROGRESS = "Progress: {0}%";

        public const string TIP_TAP_FISH = "Tap a fish.";
        public const string TIP_TARGET_TANK = "It will go to the target tank.";
        public const string TIP_GOAL = "The goal shows here.";
        public const string TIP_COMPLETE = "Get 3 to complete it!";
    }
}
