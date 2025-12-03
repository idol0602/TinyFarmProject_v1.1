using System;
using UnityEngine;

//
// Hệ thống sự kiện ngày mới dùng cho Crop và các hệ thống khác.
// Không cần gắn vào bất kỳ GameObject nào.
//

public static class DayAndNightEvents
{
    /// <summary>
    /// Sự kiện bắn ra mỗi khi sang NGÀY MỚI.
    /// Tham số int = số ngày hiện tại (1,2,3…)
    /// </summary>
    public static Action<int> OnNewDay;

    /// <summary>
    /// Gọi sự kiện ngày mới (chỉ được gọi từ DayAndNightManager).
    /// </summary>
    public static void InvokeNewDay(int day)
    {
        OnNewDay?.Invoke(day);
    }
}
