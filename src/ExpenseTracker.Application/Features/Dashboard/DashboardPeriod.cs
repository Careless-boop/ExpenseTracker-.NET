namespace ExpenseTracker.Application.Features.Dashboard
{
    internal readonly record struct DashboardPeriod(DateTime From, DateTime To)
    {
        /// <summary>
        /// Defaults to the current month. <paramref name="to"/> is pushed to the end of its day so a
        /// caller passing a bare date still includes transactions recorded later that day.
        /// </summary>
        public static DashboardPeriod Resolve(DateTime? from, DateTime? to)
        {
            var now = DateTime.UtcNow;

            var start = from?.Date ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = (to?.Date ?? start.AddMonths(1).AddDays(-1)).AddDays(1).AddTicks(-1);

            return new DashboardPeriod(start, end);
        }

        /// <summary>The window of equal length immediately before this one.</summary>
        public DashboardPeriod Previous()
        {
            var length = To - From;
            return new DashboardPeriod(From - length, From.AddTicks(-1));
        }
    }
}
