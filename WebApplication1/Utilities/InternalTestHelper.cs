namespace TourGuide.Utilities;

public static class InternalTestHelper
{
    // Set this default up to 100,000 for testing
    private static int internalUserNumber = 100;

    public static void SetInternalUserNumber(int number)
    {
        internalUserNumber = number;
    }

    public static int GetInternalUserNumber()
    {
        return internalUserNumber;
    }
}
