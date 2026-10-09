namespace AbyssalDungeon.Core
{
    public static class Constants
    {
        public static class Scenes
        {
            public const string MainMenu = "MainMenu";
            public const string Lobby = "Lobby";
            public const string ClassSelection = "ClassSelection";
            public const string Dungeon = "Dungeon";
            public const string EndGame = "EndGame";
            public const string Town = "Town";
        }

        public static class Tags
        {
            public const string Player = "Player";
            public const string Enemy = "Enemy";
            public const string Boss = "Boss";
            public const string Projectile = "Projectile";
            public const string Chest = "Chest";
            public const string Door = "Door";
            public const string Trap = "Trap";
            public const string Crystal = "Crystal";
            public const string Interactable = "Interactable";
        }

        public static class Layers
        {
            public const int Player = 6;
            public const int Enemy = 7;
            public const int Ground = 8;
            public const int Obstacle = 9;
            public const int Interactable = 10;
        }

        public static class AnimationParams
        {
            public const string Speed = "Speed";
            public const string IsMoving = "IsMoving";
            public const string Attack = "Attack";
            public const string Death = "Death";
            public const string Hit = "Hit";
        }

        public const int MaxPartySize = 4;
        public const int MaxLevel = 50;
    }
}
