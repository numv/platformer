    using Godot;

    public static class Helper
    {
        public static float Lerp(this float firstFloat, float secondFloat, float by)
        {
            return firstFloat * (1 - by) + secondFloat * by;
        } 
        public static Vector2 Lerp(this Vector2 firstVector, Vector2 secondVector, float by)
        {
            float retX = Lerp(firstVector.x, secondVector.x, by);
            float retY = Lerp(firstVector.y, secondVector.y, by);
            return new Vector2(retX, retY);
        }
    }