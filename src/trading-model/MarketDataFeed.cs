namespace trading_model
{
    public class MarketDataFeed
    {
        public string symbol { get; set; }

        public DateTime timestamp { get; set; }

        public decimal avgAskPrice { get; set; }
        public decimal avgBidPrice { get; set; }

    }
}