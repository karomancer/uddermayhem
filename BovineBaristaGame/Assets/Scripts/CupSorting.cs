using UnityEngine;

/// <summary>
/// Cup layers sort around the lane's teat: back wall, liquid and latte art behind it, front wall and
/// overflow in front, so the teat looks like it is in the cup. The teats' own orders live in the scene
/// and each needs 4 free orders below it and 2 above; the right lane of a row sits above the left lane so
/// a cup sliding through its neighbour never interleaves with it. Main.unity: back teats 10 / 17, udder
/// base 20, front teats 30 / 37, tummy 40.
/// </summary>
public static class CupSorting
{
    private const int LayersBehindTeat = 4;

    public static int TeatOrder(TeatPosition lane)
    {
        TeatController teat = TeatController.ForLane(lane);
        if (teat != null) return teat.SortingOrder;

        Debug.LogWarning($"No teat registered for {lane}; using fallback sorting order");
        return lane == TeatPosition.BackLeft || lane == TeatPosition.BackRight ? 10 : 30;
    }

    // Glass back, liquid body, surface, latte art occupy +0..+3
    public static int CupBackOrder(TeatPosition lane)
    {
        return TeatOrder(lane) - LayersBehindTeat;
    }

    // Glass front, overflow occupy +0..+1; single-sprite and tipped cups use +1
    public static int CupFrontOrder(TeatPosition lane)
    {
        return TeatOrder(lane) + 1;
    }

}
