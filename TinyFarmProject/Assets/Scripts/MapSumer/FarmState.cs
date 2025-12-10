using UnityEngine;

public static class FarmState
{
    // Đánh dấu rằng player vừa ngủ qua ngày
    public static bool IsSleepTransition = false;

    // Đánh dấu cần SAVE sau khi quay lại MapSummer
    public static bool NeedSaveAfterReturn = false;
}
