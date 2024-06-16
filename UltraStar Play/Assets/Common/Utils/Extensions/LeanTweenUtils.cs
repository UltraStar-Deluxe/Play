using System.Collections.Generic;

public static class LeanTweenUtils
{
    public static void CancelAndClear(List<int> animationIds)
    {
        animationIds.ForEach(animationId => LeanTween.cancel(animationId));
        animationIds.Clear();
    }
}
