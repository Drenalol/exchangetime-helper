using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace NYSE.TimeHelper
{
    public class ExchangeTimeHelper
    {
        private readonly Dictionary<ExchangeTime, TimeSpan> _times;
        private static DateTimeOffset? _winterTimeNextCheck;
        private static bool _isWinterTime;

        public ExchangeTimeHelper(IOptions<ExchangeTimeOptions> options = null)
        {
            var exchangeTimeOptions = options?.Value ?? new ExchangeTimeOptions();
            _times = new Dictionary<ExchangeTime, TimeSpan>
            {
                {ExchangeTime.Open, TimeSpan.Parse(exchangeTimeOptions.Open)},
                {ExchangeTime.PreMarketUsa, TimeSpan.Parse(exchangeTimeOptions.PreMarketUsa)},
                {ExchangeTime.PreMarketUsaWinter, TimeSpan.Parse(exchangeTimeOptions.PreMarketUsaWinter)},
                {ExchangeTime.OpenUsa, TimeSpan.Parse(exchangeTimeOptions.OpenUsa)},
                {ExchangeTime.OpenUsaWinter, TimeSpan.Parse(exchangeTimeOptions.OpenUsaWinter)},
                {ExchangeTime.PostMarketUsa, TimeSpan.Parse(exchangeTimeOptions.PostMarketUsa)},
                {ExchangeTime.PostMarketUsaWinter, TimeSpan.Parse(exchangeTimeOptions.PostMarketUsaWinter)},
                {ExchangeTime.Close, TimeSpan.Parse(exchangeTimeOptions.Close)}
            };
        }

        private enum ExchangeTime
        {
            Open,
            PreMarketUsa,
            PreMarketUsaWinter,
            OpenUsa,
            OpenUsaWinter,
            PostMarketUsa,
            PostMarketUsaWinter,
            Close
        }

        private bool InternalIsWinterTime(DateTimeOffset dateTime)
        {
            if (_winterTimeNextCheck != null && _winterTimeNextCheck > DateTimeOffset.Now)
                return _isWinterTime;

            _isWinterTime = IsWinterTime(dateTime);
            _winterTimeNextCheck = DateTimeOffset.Now.AddDays(1).Date;

            return _isWinterTime;
        }

        public bool IsWinterTime(DateTimeOffset dateTime)
        {
            if (dateTime < new DateTimeOffset(dateTime.Year, 11, 1, 0, 0, 0, 0, TimeSpan.Zero)
                && dateTime > new DateTimeOffset(dateTime.Year, 4, 1, 0, 0, 0, 0, TimeSpan.Zero))
                return false;

            const int novemberMonday = 1;
            const int marchSunday = 2;

            var period = dateTime.Month is > 3 and <= 12;

            var firstMondayNovember = new DateTimeOffset(dateTime.Year - (period ? 0 : 1), 11, 1, 0, 0, 0, 0, TimeSpan.Zero);
            var secondSundayMarch = new DateTimeOffset(dateTime.Year + (period ? 1 : 0), 3, 1, 0, 0, 0, 0, TimeSpan.Zero);

            var novemberMondayCounter = 0;
            var marchSundayCounter = 0;

            while (true)
            {
                if (firstMondayNovember.DayOfWeek == DayOfWeek.Monday && ++novemberMondayCounter == novemberMonday)
                    break;

                firstMondayNovember = firstMondayNovember.AddDays(1);
            }

            while (true)
            {
                if (secondSundayMarch.DayOfWeek == DayOfWeek.Sunday && ++marchSundayCounter == marchSunday)
                    break;

                secondSundayMarch = secondSundayMarch.AddDays(1);
            }

            return dateTime > firstMondayNovember && dateTime < secondSundayMarch;
        }

        public bool IsExchangeOpen(DateTimeOffset dateTime) => dateTime.TimeOfDay >= _times[ExchangeTime.Open] || dateTime.TimeOfDay <= _times[ExchangeTime.Close];

        public bool IsPreMarketUsa(DateTimeOffset dateTime)
        {
            var isWinter = InternalIsWinterTime(dateTime);
            var preMarketTime = isWinter
                ? ExchangeTime.PreMarketUsaWinter
                : ExchangeTime.PreMarketUsa;
            var openTime = isWinter
                ? ExchangeTime.OpenUsaWinter
                : ExchangeTime.OpenUsa;
            
            return dateTime.TimeOfDay >= _times[preMarketTime] && dateTime.TimeOfDay <= _times[openTime];
        }

        public bool IsUsaOpen(DateTimeOffset dateTime)
        {
            var isWinter = InternalIsWinterTime(dateTime);
            var openTime = isWinter
                ? ExchangeTime.OpenUsaWinter
                : ExchangeTime.OpenUsa;
            var postMarketTime = isWinter
                ? ExchangeTime.PostMarketUsaWinter
                : ExchangeTime.PostMarketUsa;
            
            return dateTime.TimeOfDay >= _times[openTime] && dateTime.TimeOfDay <= _times[postMarketTime];
        }

        public bool IsPostMarketUsa(DateTimeOffset dateTime)
        {
            var isWinter = InternalIsWinterTime(dateTime);
            var postMarketTime = isWinter
                ? ExchangeTime.PostMarketUsaWinter
                : ExchangeTime.PostMarketUsa;
            
            return dateTime.TimeOfDay >= _times[postMarketTime] || dateTime.TimeOfDay <= _times[ExchangeTime.Close];
        }

        public DateTimeOffset GetNextDate()
        {
            var now = DateTimeOffset.Now;
            var delay = 0.0;

            if (now.DayOfWeek == DayOfWeek.Saturday && now.TimeOfDay > _times[ExchangeTime.Close] || now.DayOfWeek == DayOfWeek.Sunday || now.DayOfWeek == DayOfWeek.Monday && now.TimeOfDay < _times[ExchangeTime.Open])
            {
                var days = now.DayOfWeek switch
                {
                    DayOfWeek.Saturday => 2,
                    DayOfWeek.Sunday => 1,
                    _ => 0
                };

                delay = (now.AddDays(days).Date.Add(_times[ExchangeTime.Open]) - now).TotalSeconds + 1;
            }
            else if (!IsExchangeOpen(now))
                delay = (_times[ExchangeTime.Open] - now.TimeOfDay).TotalSeconds;

            return now.AddSeconds(delay);
        }
    }
}