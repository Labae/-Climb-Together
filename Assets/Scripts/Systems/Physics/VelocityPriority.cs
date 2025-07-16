namespace Systems.Physics
{
    public static class VelocityPriority
    {
        public const int Background = 0;
        public const int Movement = 10;
        public const int Jump = 20;
        public const int Dash = 30;
        public const int Knockback = 40;
        public const int Override = 100;
    }
}
