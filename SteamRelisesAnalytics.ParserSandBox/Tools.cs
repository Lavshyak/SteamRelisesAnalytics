namespace SteamRelisesAnalytics.ParserSandBox;

public static class Tools
{
    

    static int? TryParseYear(string releaseDate)
    {
        if (releaseDate.Length < 4)
            return null;
        
        var mayBeYearStr = releaseDate.Substring(releaseDate.Length - 4, 4);
        if (int.TryParse(mayBeYearStr, out int year))
        {
            return year;
        }

        return null;
    }

    static int? TryGetQuarter(string releaseDate)
    {
        if (releaseDate.First() == 'Q')
        {
            if (int.TryParse(releaseDate.Substring(1, 1), out int quarter))
            {
                return quarter;
            }
        }

        return null;
    }
    
    public enum PositionCheckResult
    {
        // "To be announced" or "Coming soon" or correct year or correct quarter
        SkipThis,
        TooFuture,
        TooPast,
        // correct year and month
        Correct
    }
    
    static DateTime? TryParseDate(string releaseDate)
    {
        if (DateTime.TryParseExact(releaseDate, ["dd MMM, yyyy", "dd MMM, yyyy"], 
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
            return date;

        return null;
    }

    static int? TryParseMonth(string releaseDate)
    {
        if (DateTime.TryParseExact(releaseDate, "MMMM, yyyy", 
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
            return date.Month;

        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="PositionCheckResult"></param>
    /// <param name="Month">1..12</param>
    /// <param name="DayOfMonth">1+</param>
    public record PositionCheckResultAndDateTime(PositionCheckResult PositionCheckResult, int? Month, int? DayOfMonth)
    {
        public static implicit operator PositionCheckResultAndDateTime(PositionCheckResult value)
        {
            if (value == PositionCheckResult.Correct)
            {
                throw new ArgumentException();
            }
            return new PositionCheckResultAndDateTime(value, null, null);
        }

        public static PositionCheckResultAndDateTime FromMonth(int month)
        {
            return new PositionCheckResultAndDateTime(PositionCheckResult.Correct, month, null);
        }
        
        public static PositionCheckResultAndDateTime FromMonthAndDateOfMonth(int month, int dateOfMonth)
        {
            return new PositionCheckResultAndDateTime(PositionCheckResult.Correct, month, dateOfMonth);
        }
    }
    
    // TODO: если надо приложения конкретно за квартал или за год, то это решение не подойдет
    /// <summary>
    /// 
    /// </summary>
    /// <param name="releaseDate"></param>
    /// <param name="correctYear"></param>
    /// <param name="fromMonth">1..12</param>
    /// <param name="toMonth">1..12</param>
    /// <returns></returns>
    public static PositionCheckResultAndDateTime Check(string releaseDate, int correctYear, int fromMonth, int toMonth)
    {
        var year = TryParseYear(releaseDate);
        if (year == null)
        {
            return PositionCheckResult.SkipThis; // "To be announced" or "Coming soon"
        }

        if (year < correctYear)
        {
            return PositionCheckResult.TooPast;
        }
        if (year > correctYear)
        {
            return PositionCheckResult.TooFuture;
        }
        // correct year

        var quarter = TryGetQuarter(releaseDate);
        if (quarter != null)
        {
            var correctQuarterFrom = Math.Ceiling(fromMonth / 3.0);
            var correctQuarterTo = Math.Ceiling(toMonth / 3.0);
            if (quarter < correctQuarterTo)
            {
                return PositionCheckResult.TooPast;
            }
            else if(quarter > correctQuarterTo)
            {
                return PositionCheckResult.TooFuture;
            }
            else
            {
                return PositionCheckResult.SkipThis;
            }
        }
        // no quarter specified

        var exactDate = TryParseDate(releaseDate);
        if (exactDate != null)
        {
            if (exactDate.Value.Month > toMonth)
            {
                return PositionCheckResult.TooFuture;
            }

            if (exactDate.Value.Month < fromMonth)
            {
                return PositionCheckResult.TooPast;
            }
            
            return PositionCheckResultAndDateTime.FromMonthAndDateOfMonth(exactDate.Value.Month, exactDate.Value.Day);
        }

        var month = TryParseMonth(releaseDate);
        if (month != null)
        {
            if (month.Value > toMonth)
            {
                return PositionCheckResult.TooFuture;
            }

            if (month.Value < fromMonth)
            {
                return PositionCheckResult.TooPast;
            }
            
            return PositionCheckResultAndDateTime.FromMonth(month.Value);
        }

        return PositionCheckResult.SkipThis;
    }
}