namespace NYSE.TimeHelper
{
    public class ExchangeTimeOptions
    {
        public string Open { get; set; } = "07:00:00";
        public string PreMarketUsa { get; set; } = "14:00:00";
        public string PreMarketUsaWinter { get; set; } = "15:00:00";
        public string OpenUsa { get; set; } = "16:30:00";
        public string OpenUsaWinter { get; set; } = "17:30:00";
        public string PostMarketUsa { get; set; } = "23:00:00";
        public string PostMarketUsaWinter { get; set; } = "00:00:00";
        public string Close { get; set; } = "01:45:00";
    }
}