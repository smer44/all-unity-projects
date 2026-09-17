using UnityEngine;

[CreateAssetMenu(fileName = "SimpleDate", menuName = "Custom Time/Simple Date")]
public class SimpleDate : AbstractDate
{
    private const int MonthsPerYear = 12;
    private const int DaysPerMonth = 30;
    private const int DaysPerWeek = 7;
    private const int DaysPerYear = MonthsPerYear * DaysPerMonth;

    private static readonly string[] MonthNames =
    {
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December"
    };

    private static readonly string[] DayOfWeekNames =
    {
        "Monday",
        "Tuesday",
        "Wednesday",
        "Thursday",
        "Friday",
        "Saturday",
        "Sunday"
    };


    [Header("Date")]
    [Tooltip("Calendar year. Year 1 is the first year.")]
    [Min(1)]
    [SerializeField] private int currentYear = 1;

    [Tooltip("Calendar month, from 1 to 12.")]
    [Range(1, MonthsPerYear)]
    [SerializeField] private int month = 1;

    [Tooltip("Calendar day of month, from 1 to 30.")]
    [Range(1, DaysPerMonth)]
    [SerializeField] private int day = 1;

    [Tooltip("Zero-based full date count. 0 = Year 1, January 01, Monday.")]
    [SerializeField, HideInInspector] private int days = 0;


    public override int Days()
    {
        return days;
    }

    public override AbstractDate NewNextDay()
    {
        return CreateRuntime(Days() + 1);
    }

    public override int DayOfWeek()
    {
        return Days() % DaysPerWeek;
    }

    public void SetDays(int days)
    {
        SetDateFromDays(Mathf.Max(0, days));
    }

    private static SimpleDate CreateRuntime(int days)
    {
        SimpleDate dateTime = CreateInstance<SimpleDate>();
        dateTime.SetDays(days);
        return dateTime;
    }


    public override string PP()
    {
        return $"{currentYear:D4} {MonthNames[month - 1]} {day:D2} {DayOfWeekNames[DayOfWeek()]}";
    }

    private void OnEnable()
    {
        UpdateDaysFromDate();
    }

    private void UpdateDaysFromDate()
    {
        ClampDateFields();
        days = (currentYear - 1) * DaysPerYear
            + (month - 1) * DaysPerMonth
            + (day - 1);
    }

    private void SetDateFromDays(int totalDays)
    {
        days = totalDays;

        currentYear = days / DaysPerYear + 1;
        int dayOfYear = days % DaysPerYear;
        month = dayOfYear / DaysPerMonth + 1;
        day = dayOfYear % DaysPerMonth + 1;
    }

    private void ClampDateFields()
    {
        currentYear = Mathf.Max(1, currentYear);
        month = Mathf.Clamp(month, 1, MonthsPerYear);
        day = Mathf.Clamp(day, 1, DaysPerMonth);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateDaysFromDate();
    }
#endif    

}
