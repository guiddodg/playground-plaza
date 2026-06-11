/// <summary>
/// Global gate for gameplay input. When locked (e.g. a full-screen menu like the
/// inventory is open), the player movement and camera orbit controllers ignore
/// input so the character and camera stay frozen.
/// </summary>
public static class GameplayInputLock
{
    public static bool Locked { get; private set; }

    public static void SetLocked(bool locked)
    {
        Locked = locked;
    }
}
