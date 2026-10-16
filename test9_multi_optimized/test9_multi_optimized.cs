using System;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    // Match the Python PositionMode enum
    public enum PositionMode
    {
        Single,     // One position at a time
        Multiple    // Multiple concurrent positions
    }

    // Entry strategy enum for dropdown
    public enum EntryStrategyType
    {
        RSI,
        ConnorsRSI,
        ConnorsRSI_v2,
        BB_Stretch,
        MA_Stretch,
        MR_BollingerRSI,
        MR_MAReversion,
        MR_StochasticVolume,
        TF_EMACrossover,
        KD_MAStretch
    }

    // Exit strategy enum for dropdown
    public enum ExitStrategyType
    {
        RSI_Exit,
        BB_Exit,
        MA_Exit,
        Stretch_Exit,
        KD_MAStretch_Exit
    }

    // MA Type enum for dropdown
    public enum MaType
    {
        simple,
        exponential
    }
    
    // Filter mode enum for dropdown
    public enum FilterMode
    {
        None,           // No filter
        AwayFromMA,     // Only take trades when price is % away from MA
        WithinMA        // Only take trades when price is % within MA
    }
    
    // Sizing method enum for dropdown
    public enum SizingMethod
    {
        Percentage,     // Position face value = percentage of equity (exposure, not risk)
        FixedVolume,    // Use fixed volume in lots
        RiskPercent     // Risk a percentage of equity per trade, sized from the stop distance
    }

    // Market regime classification
    public enum MarketRegime
    {
        LowVolRanging,     // Low volatility, ranging (good for mean reversion)
        HighVolRanging,    // High volatility, still ranging
        HighVolTrending    // High volatility, trending (mean reversion dangerous)
    }

    // How KD_MAStretch measures "far from the MA"
    public enum KdStretchMode
    {
        PercentOfMA,    // Kevin's InputVar33 x Stretch Scale, as a fraction of the MA
        AtrMultiple     // A multiple of ATR
    }

    // Stop loss threshold type
    public enum StopLossThresholdType
    {
        Pips,
        Percent
    }

    // Base class for entry strategy - matches Python entry strategy pattern
    public abstract class EntryStrategy
    {
        protected cAlgo.API.Internals.IIndicatorsAccessor Indicators { get; set; }
        
        public void SetIndicators(cAlgo.API.Internals.IIndicatorsAccessor indicators)
        {
            Indicators = indicators;
        }
        
        public abstract string Name { get; }
        public abstract bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters);
        public abstract bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters);
    }

    // Base class for exit strategy - matches Python exit strategy pattern
    public abstract class ExitStrategy
    {
        protected cAlgo.API.Internals.IIndicatorsAccessor Indicators { get; set; }
        
        public void SetIndicators(cAlgo.API.Internals.IIndicatorsAccessor indicators)
        {
            Indicators = indicators;
        }
        
        public abstract string Name { get; }
        public abstract bool CheckLongExit(Bars bars, int index, Position position, Dictionary<string, object> parameters);
        public abstract bool CheckShortExit(Bars bars, int index, Position position, Dictionary<string, object> parameters);
    }

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess, AddIndicators = false)]
    public class MultiStrategyBacktester : Robot
    {
        #region Parameters
        [Parameter("Entry Strategy", DefaultValue = EntryStrategyType.MA_Stretch, Group = "Basic")]
        public EntryStrategyType EntryStrategyName { get; set; }

        [Parameter("Exit Strategy", DefaultValue = ExitStrategyType.RSI_Exit, Group = "Basic")]
        public ExitStrategyType ExitStrategyName { get; set; }

        [Parameter("Position Mode", DefaultValue = PositionMode.Multiple, Group = "Basic")]
        public PositionMode Mode { get; set; }
        
        [Parameter("Order Label", DefaultValue = "MultiStrategy", Group = "Basic")]
        public string OrderLabel { get; set; }

        [Parameter("Verbose Logging", DefaultValue = false, Group = "Basic")]
        public bool VerboseLogging { get; set; }

        [Parameter("Use Market Conditions", DefaultValue = true, Group = "Basic")]
        public bool UseMarketConditions { get; set; }

        [Parameter("Auto-Switch Strategy by Regime", DefaultValue = true, Group = "Basic")]
        public bool AutoSwitchStrategyByRegime { get; set; }

        // Low Vol Ranging Strategy
        [Parameter("Low Vol Ranging - Entry", DefaultValue = EntryStrategyType.MA_Stretch, Group = "Regime Strategies")]
        public EntryStrategyType LowVolRangingEntry { get; set; }

        [Parameter("Low Vol Ranging - Exit", DefaultValue = ExitStrategyType.RSI_Exit, Group = "Regime Strategies")]
        public ExitStrategyType LowVolRangingExit { get; set; }

        // High Vol Ranging Strategy
        [Parameter("High Vol Ranging - Entry", DefaultValue = EntryStrategyType.MA_Stretch, Group = "Regime Strategies")]
        public EntryStrategyType HighVolRangingEntry { get; set; }

        [Parameter("High Vol Ranging - Exit", DefaultValue = ExitStrategyType.MA_Exit, Group = "Regime Strategies")]
        public ExitStrategyType HighVolRangingExit { get; set; }

        // High Vol Trending Strategy
        [Parameter("High Vol Trending - Entry", DefaultValue = EntryStrategyType.TF_EMACrossover, Group = "Regime Strategies")]
        public EntryStrategyType HighVolTrendingEntry { get; set; }

        [Parameter("High Vol Trending - Exit", DefaultValue = ExitStrategyType.Stretch_Exit, Group = "Regime Strategies")]
        public ExitStrategyType HighVolTrendingExit { get; set; }

        [Parameter("Sizing Method", DefaultValue = SizingMethod.Percentage, Group = "Position Sizing")]
        public SizingMethod SizingMethod { get; set; }

        [Parameter("Position Size % (Base)", DefaultValue = 3.0, Group = "Position Sizing")]
        public double PositionSizePct { get; set; }

        [Parameter("Risk Per Trade %", DefaultValue = 1.0, MinValue = 0.01, Step = 0.05, Group = "Position Sizing")]
        public double RiskPerTradePct { get; set; }

        [Parameter("Fixed Volume (Lots)", DefaultValue = 0.1, Group = "Position Sizing")]
        public double FixedVolume { get; set; }

        [Parameter("Size Multiplier (Low Vol Ranging)", DefaultValue = 1.2, Group = "Position Sizing")]
        public double LowVolRangingSizeMult { get; set; }

        [Parameter("Size Multiplier (High Vol Ranging)", DefaultValue = 0.7, Group = "Position Sizing")]
        public double HighVolRangingSizeMult { get; set; }

        [Parameter("Size Multiplier (High Vol Trending)", DefaultValue = 0.3, Group = "Position Sizing")]
        public double HighVolTrendingSizeMult { get; set; }

        [Parameter("ADX Trending Threshold", DefaultValue = 20.0, Group = "Market Conditions")]
        public double AdxTrendingThreshold { get; set; }

        [Parameter("Volatility High Threshold (% of price)", DefaultValue = 0.01, Group = "Market Conditions")]
        public double VolatilityHighThreshold { get; set; }

        [Parameter("Skip Trending Market Entries", DefaultValue = false, Group = "Market Conditions")]
        public bool SkipEntriesInTrending { get; set; }

        [Parameter("Max Bars", DefaultValue = 25, Group = "Position Limits")]
        public int MaxBars { get; set; }

        [Parameter("Max Concurrent Positions", DefaultValue = 3, Group = "Position Limits")]
        public int MaxConcurrent { get; set; }

        #region === STOP LOSS PARAMETERS ===
        [Parameter("Enable Stop Loss", DefaultValue = true, Group = "Stop Loss")]
        public bool EnableStopLoss { get; set; }

        [Parameter("Stop Loss Lookback Period (Bars)", DefaultValue = 20, Group = "Stop Loss")]
        public int StopLossLookbackPeriod { get; set; }

        [Parameter("Stop Loss Threshold Type", DefaultValue = StopLossThresholdType.Percent, Group = "Stop Loss")]
        public StopLossThresholdType StopLossThresholdType { get; set; }

        // Ranging Market Stop Loss
        [Parameter("Ranging Stop Loss Threshold", DefaultValue = 1.5, Group = "Stop Loss")]
        public double RangingStopLossThreshold { get; set; }

        // Trending Up Stop Loss
        [Parameter("Trending Up Stop Loss Threshold", DefaultValue = 2.0, Group = "Stop Loss")]
        public double TrendingUpStopLossThreshold { get; set; }

        // Trending Down Stop Loss
        [Parameter("Trending Down Stop Loss Threshold", DefaultValue = 2.0, Group = "Stop Loss")]
        public double TrendingDownStopLossThreshold { get; set; }
        #endregion

        [Parameter("Enable ADX Above Threshold Block", DefaultValue = true, Group = "Trade Filters")]
        public bool EnableAdxAboveThresholdBlock { get; set; }

        [Parameter("ADX Period", DefaultValue = 14, Group = "Trade Filters")]
        public int AdxPeriod { get; set; }

        [Parameter("ADX Threshold", DefaultValue = 20.0, Group = "Trade Filters")]
        public double AdxThreshold { get; set; }

        #region === ENTRY STRATEGY PARAMETERS ===

        #region RSI Entry
        [Parameter("RSI Period", DefaultValue = 14, Group = "RSI Entry")]
        public int RsiPeriod { get; set; }

        [Parameter("Oversold Level", DefaultValue = 30, Group = "RSI Entry")]
        public int RsiOversold { get; set; }

        [Parameter("Overbought Level", DefaultValue = 70, Group = "RSI Entry")]
        public int RsiOverbought { get; set; }
        #endregion

        #region ConnorsRSI Entry
        [Parameter("RSI Period", DefaultValue = 3, Group = "ConnorsRSI Entry")]
        public int CrsiRsiPeriod { get; set; }

        [Parameter("Upper RSI", DefaultValue = 70, Group = "ConnorsRSI Entry")]
        public int CrsiUpperRsi { get; set; }

        [Parameter("Lower RSI", DefaultValue = 30, Group = "ConnorsRSI Entry")]
        public int CrsiLowerRsi { get; set; }
        #endregion

        #region ConnorsRSI_v2 Entry
        [Parameter("Connors2 RSI Period", DefaultValue = 3, Group = "ConnorsRSI_v2 Entry")]
        public int ConnorsV2RsiPeriod { get; set; }
        [Parameter("ConnorsRSI_v2 Entry Level", DefaultValue = 25, Group = "ConnorsRSI_v2 Entry")]
        public double ConnorsEntry { get; set; }
        [Parameter("ConnorsRSI_v2 Exit Level", DefaultValue = 70, Group = "ConnorsRSI_v2 Entry")]
        public double ConnorsExit { get; set; }
        #endregion

        #region BB_Stretch Entry
        [Parameter("BB Window", DefaultValue = 20, Group = "BB_Stretch Entry")]
        public int BbWindow { get; set; }

        [Parameter("BB Std Dev", DefaultValue = 2.0, Group = "BB_Stretch Entry")]
        public double BbStdDev { get; set; }
        #endregion

        #region MA_Stretch Entry
        [Parameter("MA Period", DefaultValue = 30, Group = "MA_Stretch Entry")]
        public int MaStretchMaPeriod { get; set; }

        [Parameter("ATR Period", DefaultValue = 15, Group = "MA_Stretch Entry")]
        public int MaStretchAtrPeriod { get; set; }

        [Parameter("Filter Mode", DefaultValue = FilterMode.None, Group = "MA_Stretch Entry")]
        public FilterMode MaStretchFilterMode { get; set; }

        [Parameter("Price MA Threshold %", DefaultValue = 1.0, Group = "MA_Stretch Entry")]
        public double MaStretchPriceMaThreshold { get; set; }

        [Parameter("Trend MA Period", DefaultValue = 200, Group = "MA_Stretch Entry")]
        public int MaStretchTrendPeriod { get; set; }
        
        [Parameter("Volatility Threshold %", DefaultValue = 10, Group = "MA_Stretch Entry")]
        public double MaStretchVolatilityThreshold { get; set; }
        #endregion

        #region MR_BollingerRSI Entry
        [Parameter("Bollinger Window", DefaultValue = 20, Group = "MR_BollingerRSI Entry")]
        public int MrBbWindow { get; set; }

        [Parameter("Bollinger Std Dev", DefaultValue = 2.0, Group = "MR_BollingerRSI Entry")]
        public double MrBbStdDev { get; set; }

        [Parameter("RSI Window", DefaultValue = 14, Group = "MR_BollingerRSI Entry")]
        public int MrBbRsiWindow { get; set; }

        [Parameter("Oversold Threshold", DefaultValue = 30, Group = "MR_BollingerRSI Entry")]
        public int MrBbOversold { get; set; }

        [Parameter("Overbought Threshold", DefaultValue = 70, Group = "MR_BollingerRSI Entry")]
        public int MrBbOverbought { get; set; }
        #endregion

        #region MR_MAReversion Entry
        [Parameter("MA Period", DefaultValue = 50, Group = "MR_MAReversion Entry")]
        public int MrMaPeriod { get; set; }

        [Parameter("MA Type", DefaultValue = MaType.simple, Group = "MR_MAReversion Entry")]
        public MaType MrMaType { get; set; }
        #endregion

        #region MR_StochasticVolume Entry
        [Parameter("%K Period", DefaultValue = 14, Group = "MR_StochasticVolume Entry")]
        public int StochKPeriod { get; set; }

        [Parameter("%D Period", DefaultValue = 3, Group = "MR_StochasticVolume Entry")]
        public int StochDPeriod { get; set; }

        [Parameter("Slowing", DefaultValue = 3, Group = "MR_StochasticVolume Entry")]
        public int StochSlowing { get; set; }

        [Parameter("Oversold Level", DefaultValue = 20, Group = "MR_StochasticVolume Entry")]
        public int StochOversold { get; set; }

        [Parameter("Overbought Level", DefaultValue = 80, Group = "MR_StochasticVolume Entry")]
        public int StochOverbought { get; set; }

        [Parameter("Volume Threshold (x avg)", DefaultValue = 1.5, Group = "MR_StochasticVolume Entry")]
        public double StochVolumeThreshold { get; set; }
        #endregion

        #region TF_EMACrossover Entry
        [Parameter("Short EMA Window", DefaultValue = 5, Group = "TF_EMACrossover Entry")]
        public int TfShortWindow { get; set; }

        [Parameter("Long EMA Window", DefaultValue = 20, Group = "TF_EMACrossover Entry")]
        public int TfLongWindow { get; set; }

        [Parameter("Trend EMA Window", DefaultValue = 50, Group = "TF_EMACrossover Entry")]
        public int TfTrendWindow { get; set; }
        #endregion

        #region KD_MAStretch Entry (Kevin Davey)
        // ==================================================================================
        //  KD_MAStretch - NOTES (Kevin Davey's moving average stretch)
        // ==================================================================================
        //  Idea:  close far BELOW its InputVar2-length SMA -> buy; far ABOVE -> sell short.
        //         Exit when the close crosses back over the same MA (InputVar4).
        //
        //  Kevin's original stretch (Stretch Mode = PercentOfMA, Stretch Scale = 1.0):
        //         InputVar33 = 0.5 * (0.004667 * InputVar3 - 0.08667)   -> a fraction of the MA
        //         InputVar3 25 -> 1.50%, 30 -> 2.67%, 35 -> 3.83%, 40 -> 5.00%
        //         Sized for the S&P (ES), which moves ~2-3x more per day than major forex pairs,
        //         so on forex these are rare events (and almost never happen intraday).
        //
        //  Two ways to fit forex:
        //   1. PercentOfMA + Stretch Scale: stretch = Scale x InputVar33. Keeps Kevin's formula
        //      and the 25-40 grid. Rough starting Scales for EURUSD-type majors:
        //         Daily 0.35 (guess)  |  H6 0.33 (measured, see below)  |  H1 0.08 (guess)
        //      Each pair and timeframe needs its own Scale (GBPJPY moves far more than EURCHF).
        //   2. AtrMultiple: stretch = ATR Multiple x ATR(ATR Period). Adapts to the pair and
        //      timeframe by itself, so one grid (1.5-3.0, step 0.5) works everywhere.
        //   -> One market, staying closest to Kevin: use 1. Several pairs/timeframes: use 2.
        //
        //  Measured: EURUSD H6, MA 10, ATR 14, Jan-Oct 2026 (816 bars). Distance of closes
        //  from the MA at the 90th / 95th / 98th percentile:
        //         % of MA: 0.49% / 0.63% / 0.82%      ATRs: 1.83 / 2.25 / 2.77
        //      Kevin's 1.50% (InputVar3 25) was reached on 0.0% of closes -> no trades.
        //      Scale 0.33 turns 25-40 into 0.50 / 0.88 / 1.26 / 1.65% - only the first two trade.
        //      ATR grid 1.5 / 2.0 / 2.5 / 3.0 spans ~85th-99th percentile - all four useful.
        //      Only ~10 months of data: re-check over several years before settling.
        //
        //  Picking values from data: leave "Print Stretch Stats" on and run a backtest. At the
        //  END of the run the log shows how far closes get from the MA (90th/95th/98th
        //  percentiles, in % of MA and in ATRs) and how often the current setting is reached.
        //  Good thresholds sit around the 90th-98th percentile (the most extreme 2-10% of
        //  closes). Example: if the 95th percentile is 0.60%, a Scale giving ~0.6% at
        //  InputVar3 = 25 (0.6 / 1.5 = 0.40) is a sensible start.
        //
        //  To match Kevin's TradeStation test: Entry KD_MAStretch, Exit KD_MAStretch_Exit,
        //  Position Mode Single, Use Market Conditions off, ADX block off, Stop Loss off,
        //  Max Bars 0.
        // ==================================================================================
        [Parameter("MA Length (InputVar2)", DefaultValue = 10, MinValue = 5, MaxValue = 20, Step = 5, Group = "KD_MAStretch")]
        public int KdMaPeriod { get; set; }

        [Parameter("Stretch Mode", DefaultValue = KdStretchMode.PercentOfMA, Group = "KD_MAStretch")]
        public KdStretchMode KdStretchMode { get; set; }

        [Parameter("Stretch Input (InputVar3)", DefaultValue = 25, MinValue = 25, MaxValue = 40, Step = 5, Group = "KD_MAStretch")]
        public int KdStretchInput { get; set; }

        [Parameter("Stretch Scale (PercentOfMA, 1 = Kevin)", DefaultValue = 1.0, MinValue = 0.01, Step = 0.05, Group = "KD_MAStretch")]
        public double KdStretchScale { get; set; }

        [Parameter("ATR Multiple (AtrMultiple)", DefaultValue = 2.0, MinValue = 1.5, MaxValue = 3.0, Step = 0.5, Group = "KD_MAStretch")]
        public double KdAtrMultiple { get; set; }

        [Parameter("ATR Period (AtrMultiple)", DefaultValue = 14, MinValue = 1, Group = "KD_MAStretch")]
        public int KdAtrPeriod { get; set; }

        [Parameter("Exit On MA Cross (InputVar4)", DefaultValue = true, Group = "KD_MAStretch")]
        public bool KdExitOnMa { get; set; }

        [Parameter("Print Stretch Stats", DefaultValue = true, Group = "KD_MAStretch")]
        public bool PrintKdStretchStats { get; set; }

        // InputVar33 in Kevin's code: maps InputVar3 to a stretch as a fraction of the MA
        private double KdStretchFraction => 0.5 * (0.004667 * KdStretchInput - 0.08667);

        // The stretch actually used in PercentOfMA mode
        private double KdEffectiveStretch => KdStretchScale * KdStretchFraction;
        #endregion

        #endregion

        #region === EXIT STRATEGY PARAMETERS ===

        #region RSI_Exit
        [Parameter("RSI Period", DefaultValue = 14, Group = "RSI_Exit")]
        public int ExitRsiPeriod { get; set; }

        [Parameter("Exit Overbought (Long Exit)", DefaultValue = 70, Group = "RSI_Exit")]
        public int ExitOverbought { get; set; }

        [Parameter("Exit Oversold (Short Exit)", DefaultValue = 30, Group = "RSI_Exit")]
        public int ExitOversold { get; set; }
        
        // Price-based RSI exit parameters with MA filter
        [Parameter("Use Trend RSI Filter", DefaultValue = true, Group = "RSI_Exit")]
        public bool UseTrendRsiFilter { get; set; }
        
        [Parameter("RSI Trend MA Period", DefaultValue = 200, Group = "RSI_Exit")]
        public int RsiTrendMaPeriod { get; set; }
        
        [Parameter("ADX Period (Trend Strength)", DefaultValue = 14, Group = "RSI_Exit")]
        public int RsiAdxPeriod { get; set; }
        
        [Parameter("ADX Trending Threshold", DefaultValue = 25.0, Group = "RSI_Exit")]
        public double RsiAdxTrendingThreshold { get; set; }
        
        // Ranging Market Parameters
        [Parameter("Ranging - RSI Period", DefaultValue = 3, Group = "RSI_Exit")]
        public int RangingRsiPeriod { get; set; }
        
        [Parameter("Ranging - Overbought (Long Exit)", DefaultValue = 90, Group = "RSI_Exit")]
        public int RangingOverbought { get; set; }
        
        [Parameter("Ranging - Oversold (Short Exit)", DefaultValue = 20, Group = "RSI_Exit")]
        public int RangingOversold { get; set; }
        
        // Trending Up Market Parameters
        [Parameter("Trending Up - RSI Period", DefaultValue = 14, Group = "RSI_Exit")]
        public int TrendingUpRsiPeriod { get; set; }
        
        [Parameter("Trending Up - Overbought (Long Exit)", DefaultValue = 80, Group = "RSI_Exit")]
        public int TrendingUpOverbought { get; set; }
        
        [Parameter("Trending Up - Oversold (Short Exit)", DefaultValue = 40, Group = "RSI_Exit")]
        public int TrendingUpOversold { get; set; }
        
        // Trending Down Market Parameters
        [Parameter("Trending Down - RSI Period", DefaultValue = 14, Group = "RSI_Exit")]
        public int TrendingDownRsiPeriod { get; set; }
        
        [Parameter("Trending Down - Overbought (Long Exit)", DefaultValue = 60, Group = "RSI_Exit")]
        public int TrendingDownOverbought { get; set; }
        
        [Parameter("Trending Down - Oversold (Short Exit)", DefaultValue = 40, Group = "RSI_Exit")]
        public int TrendingDownOversold { get; set; }
        #endregion

        #region BB_Exit
        [Parameter("Bollinger Window", DefaultValue = 20, Group = "BB_Exit")]
        public int ExitBbWindow { get; set; }

        [Parameter("Bollinger Std Dev", DefaultValue = 2.0, Group = "BB_Exit")]
        public double ExitBbStdDev { get; set; }
        #endregion

        #region MA_Exit
        [Parameter("MA Period", DefaultValue = 50, Group = "MA_Exit")]
        public int ExitMaPeriod { get; set; }

        [Parameter("MA Type", DefaultValue = MaType.simple, Group = "MA_Exit")]
        public MaType ExitMaType { get; set; }
        #endregion

        #region Stretch_Exit
        [Parameter("MA Period", DefaultValue = 50, Group = "Stretch_Exit")]
        public int StretchExitMaPeriod { get; set; }

        [Parameter("ATR Period", DefaultValue = 14, Group = "Stretch_Exit")]
        public int StretchExitAtrPeriod { get; set; }

        [Parameter("Exit Within (x ATR of MA)", DefaultValue = 0.5, MinValue = 0.0, Step = 0.05, Group = "Stretch_Exit")]
        public double StretchExitAtrMultiple { get; set; }
        #endregion

        #endregion
        #endregion

        #region Fields
        private Dictionary<string, EntryStrategy> _entryStrategies = new Dictionary<string, EntryStrategy>();
        private Dictionary<string, ExitStrategy> _exitStrategies = new Dictionary<string, ExitStrategy>();
        private EntryStrategy _currentEntry;
        private ExitStrategy _currentExit;
        
        // Market condition indicators (only used when UseMarketConditions is true)
        private AverageTrueRange _atr;
        private DirectionalMovementSystem _adx;
        private SimpleMovingAverage _maFast;
        private SimpleMovingAverage _maSlow;
        
        // Market condition classification
        private MarketRegime _currentRegime;

        // KD_MAStretch stats readout (see PrintStretchStats)
        private SimpleMovingAverage _kdStatsMa;
        private AverageTrueRange _kdStatsAtr;
        private readonly List<double> _kdPercentDistances = new List<double>();
        private readonly List<double> _kdAtrDistances = new List<double>();
        #endregion

        protected override void OnStart()
        {
            // Register all entry strategies (same as Python ENTRY_STRATEGIES)
            RegisterEntryStrategies();
            RegisterExitStrategies();

            // Get the current combination based on parameters
            _currentEntry = _entryStrategies[EntryStrategyName.ToString()];
            _currentExit = _exitStrategies[ExitStrategyName.ToString()];

            // Always initialize indicators needed for stop loss and market conditions
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Simple);
            _adx = Indicators.DirectionalMovementSystem(14);
            _maFast = Indicators.SimpleMovingAverage(Bars.ClosePrices, 20);
            _maSlow = Indicators.SimpleMovingAverage(Bars.ClosePrices, 50);

            if (PrintKdStretchStats)
            {
                _kdStatsMa = Indicators.SimpleMovingAverage(Bars.ClosePrices, KdMaPeriod);
                _kdStatsAtr = Indicators.AverageTrueRange(KdAtrPeriod, MovingAverageType.Simple);
            }
            
            if (UseMarketConditions)
            {
                Print("Market condition indicators initialized");
            }

            if (EnableAdxAboveThresholdBlock)
            {
                Print("ADX Above Threshold Block initialized with threshold: {0}", AdxThreshold);
            }

            Print("Multi-Strategy Framework started");
            Print($"Entry: {EntryStrategyName}, Exit: {ExitStrategyName}, Mode: {Mode}");
            Print($"Market Conditions: {(UseMarketConditions ? "Enabled" : "Disabled")}");
            Print($"Stop Loss: {(EnableStopLoss ? "Enabled" : "Disabled")}");
            if (KdStretchMode == KdStretchMode.AtrMultiple)
            {
                Print($"KD_MAStretch: MA {KdMaPeriod}, stretch {KdAtrMultiple} x ATR({KdAtrPeriod})");
            }
            else
            {
                Print($"KD_MAStretch: MA {KdMaPeriod}, InputVar3 {KdStretchInput} x Scale {KdStretchScale} -> stretch {KdEffectiveStretch:P2} of the MA");
            }
            if (KdStretchMode == KdStretchMode.PercentOfMA && KdEffectiveStretch <= 0)
            {
                Print("Warning: KD_MAStretch stretch is not positive; the entry will fire on almost every bar");
            }

            if (SizingMethod == SizingMethod.RiskPercent && !EnableStopLoss)
            {
                Print("Warning: RiskPercent sizing needs a stop loss to size from; falling back to Percentage sizing");
            }
        }

        private void RegisterEntryStrategies()
        {
            var rsiEntry = new RSIEntry();
            rsiEntry.SetIndicators(Indicators);
            _entryStrategies.Add("RSI", rsiEntry);
            
            var connorsRsiEntry = new ConnorsRSIEntry();
            connorsRsiEntry.SetIndicators(Indicators);
            _entryStrategies.Add("ConnorsRSI", connorsRsiEntry);
            
            var connorsRsiEntryV2 = new ConnorsRSIEntryV2();
            connorsRsiEntryV2.SetIndicators(Indicators);
            _entryStrategies.Add("ConnorsRSI_v2", connorsRsiEntryV2);
            
            var bbStretchEntry = new BBStretchEntry();
            bbStretchEntry.SetIndicators(Indicators);
            _entryStrategies.Add("BB_Stretch", bbStretchEntry);
            
            var maStretchEntry = new MAStretchEntry();
            maStretchEntry.SetIndicators(Indicators);
            _entryStrategies.Add("MA_Stretch", maStretchEntry);
            
            var mrBollingerRsiEntry = new MRBollingerRSIEntry();
            mrBollingerRsiEntry.SetIndicators(Indicators);
            _entryStrategies.Add("MR_BollingerRSI", mrBollingerRsiEntry);
            
            var mrMaReversionEntry = new MRMAReversionEntry();
            mrMaReversionEntry.SetIndicators(Indicators);
            _entryStrategies.Add("MR_MAReversion", mrMaReversionEntry);
            
            var mrStochasticVolumeEntry = new MRStochasticVolumeEntry();
            mrStochasticVolumeEntry.SetIndicators(Indicators);
            _entryStrategies.Add("MR_StochasticVolume", mrStochasticVolumeEntry);
            
            var tfEmaCrossoverEntry = new TFEMACrossoverEntry();
            tfEmaCrossoverEntry.SetIndicators(Indicators);
            _entryStrategies.Add("TF_EMACrossover", tfEmaCrossoverEntry);
            
            var kdMaStretchEntry = new KDMAStretchEntry();
            kdMaStretchEntry.SetIndicators(Indicators);
            _entryStrategies.Add("KD_MAStretch", kdMaStretchEntry);
        }

        private void RegisterExitStrategies()
        {
            var rsiExit = new RSIExit();
            rsiExit.SetIndicators(Indicators);
            _exitStrategies.Add("RSI_Exit", rsiExit);
            
            var bollingerExit = new BollingerExit();
            bollingerExit.SetIndicators(Indicators);
            _exitStrategies.Add("BB_Exit", bollingerExit);
            
            var maExit = new MAExit();
            maExit.SetIndicators(Indicators);
            _exitStrategies.Add("MA_Exit", maExit);
            
            var stretchExit = new StretchExit();
            stretchExit.SetIndicators(Indicators);
            _exitStrategies.Add("Stretch_Exit", stretchExit);
            
            var kdMaStretchExit = new KDMAStretchExit();
            kdMaStretchExit.SetIndicators(Indicators);
            _exitStrategies.Add("KD_MAStretch_Exit", kdMaStretchExit);
        }

        protected override void OnBar()
        {
            // OnBar fires when a new bar opens; signals are evaluated on the bar that just closed,
            // never on the new bar, which holds only its first tick at this point
            int currentBarIndex = Bars.Count - 1;
            int lastIndex = currentBarIndex - 1;
            if (lastIndex < 1)
                return;

            if (PrintKdStretchStats)
                RecordStretchStats(lastIndex);

            // Get parameters (adjusted if market conditions are enabled)
            Dictionary<string, object> entryParams = GetEntryParameters();
            Dictionary<string, object> exitParams = GetExitParameters();
            double sizeMultiplier = 1.0;

            // Update market conditions and adjust parameters if enabled
            if (UseMarketConditions)
            {
                UpdateMarketConditions(lastIndex);

                // Auto-switch strategy based on regime if enabled
                if (AutoSwitchStrategyByRegime)
                {
                    SwitchStrategyByRegime();
                }

                entryParams = AdjustParametersBasedOnMarketConditions(entryParams);
                exitParams = AdjustParametersBasedOnMarketConditions(exitParams);
                sizeMultiplier = GetCurrentRegimeSizeMultiplier();
            }

            // Check for exits on this bot's positions first. FindAll returns a snapshot,
            // so closing positions inside the loop is safe
            foreach (var position in Positions.FindAll(OrderLabel, SymbolName))
            {
                // Each position exits with the strategy it was opened with (stored in its comment),
                // so a regime switch never hands an open trade to a different exit
                ExitStrategy exit = _exitStrategies.TryGetValue(position.Comment ?? "", out var openedWith)
                    ? openedWith
                    : _currentExit;

                bool shouldExit = position.TradeType == TradeType.Buy
                    ? exit.CheckLongExit(Bars, lastIndex, position, exitParams)
                    : exit.CheckShortExit(Bars, lastIndex, position, exitParams);

                // Max bars exit, counted from the position's entry time so it survives bot restarts
                if (!shouldExit && MaxBars > 0)
                {
                    int entryBar = Bars.OpenTimes.GetIndexByTime(position.EntryTime);
                    if (entryBar >= 0 && currentBarIndex - entryBar >= MaxBars)
                    {
                        shouldExit = true;
                    }
                }

                if (shouldExit)
                {
                    ClosePosition(position);
                }
            }

            // Single: one position at a time; Multiple: up to MaxConcurrent
            int openCount = Positions.FindAll(OrderLabel, SymbolName).Length;
            int maxOpen = Mode == PositionMode.Single ? 1 : MaxConcurrent;
            if (openCount >= maxOpen)
                return;

            // Skip entries in trending markets if enabled (protects mean reversion from strong trends)
            if (ShouldSkipEntry())
            {
                Log("Skipping entry - current regime is HighVolTrending and SkipEntriesInTrending is enabled");
                return;
            }

            if (ShouldFilterTrade(lastIndex))
                return;

            if (_currentEntry.CheckLongSignal(Bars, lastIndex, entryParams))
            {
                OpenPosition(TradeType.Buy, lastIndex, sizeMultiplier);
            }
            else if (_currentEntry.CheckShortSignal(Bars, lastIndex, entryParams))
            {
                OpenPosition(TradeType.Sell, lastIndex, sizeMultiplier);
            }
        }

        private void OpenPosition(TradeType tradeType, int index, double sizeMultiplier)
        {
            double? stopLossPips = null;
            if (EnableStopLoss)
            {
                double stopPrice = CalculateInitialStopLoss(tradeType, index);
                double entryPrice = tradeType == TradeType.Buy ? Symbol.Ask : Symbol.Bid;
                double distance = tradeType == TradeType.Buy ? entryPrice - stopPrice : stopPrice - entryPrice;
                if (distance <= 0)
                {
                    Print($"Skipping {tradeType}: price {entryPrice:F5} is already beyond the stop {stopPrice:F5}");
                    return;
                }
                stopLossPips = distance / Symbol.PipSize;
            }

            double volume = CalculateVolume(sizeMultiplier, stopLossPips);

            // The stop goes to the broker with the order, so the position stays protected even if
            // the bot stops; the comment records the exit strategy this position is managed with
            var result = ExecuteMarketOrder(tradeType, SymbolName, volume, OrderLabel, stopLossPips, null, _currentExit.Name);
            if (!result.IsSuccessful)
            {
                Print($"{tradeType} order failed: {result.Error}");
            }
        }

        private bool ShouldFilterTrade(int index)
        {
            // The ADX block protects mean-reversion entries from trends; it must not block the
            // trend-following entry, which only trades when ADX is high
            if (_currentEntry is TFEMACrossoverEntry)
                return false;

            if (EnableAdxAboveThresholdBlock && _adx != null)
            {
                double adxValue = _adx.ADX[index];

                if (adxValue > AdxThreshold)
                {
                    Log($"Trade filtered: ADX {adxValue:F2} is above threshold {AdxThreshold}");
                    return true;
                }
            }

            return false;
        }

        private double CalculateVolume(double sizeMultiplier, double? stopLossPips)
        {
            double rawVolume;

            if (SizingMethod == SizingMethod.FixedVolume)
            {
                // Fixed volume in lots, converted with the symbol's own lot size
                rawVolume = Symbol.QuantityToVolumeInUnits(FixedVolume);
            }
            else if (SizingMethod == SizingMethod.RiskPercent && stopLossPips.HasValue)
            {
                // Risk a fixed % of equity: size the trade so that hitting the stop loses that amount
                double riskAmount = Account.Equity * (RiskPerTradePct * sizeMultiplier / 100.0);
                rawVolume = riskAmount / (stopLossPips.Value * Symbol.PipValue);
            }
            else
            {
                // Notional sizing: the position's face value is a % of equity. This sets exposure,
                // not risk, and assumes the account currency matches the symbol's quote currency
                double notional = Account.Equity * (PositionSizePct * sizeMultiplier / 100.0);
                rawVolume = notional / Symbol.Ask;
            }

            // Clamp to the allowed range and round down to a valid volume step
            double volume = Math.Max(Symbol.VolumeInUnitsMin, Math.Min(Symbol.VolumeInUnitsMax, rawVolume));
            return Symbol.NormalizeVolumeInUnits(volume, RoundingMode.Down);
        }

        private void Log(string message)
        {
            if (VerboseLogging)
                Print(message);
        }

        // Get entry parameters from cTrader UI inputs (all strategies are adjustable)
        private Dictionary<string, object> GetEntryParameters()
        {
            var parameters = new Dictionary<string, object>();
            
            // RSI Entry
            parameters["rsi_period"] = RsiPeriod;
            parameters["oversold"] = RsiOversold;
            parameters["overbought"] = RsiOverbought;
            
            // ConnorsRSI Entry
            parameters["crsi_rsi_period"] = CrsiRsiPeriod;
            parameters["upper_rsi"] = CrsiUpperRsi;
            parameters["lower_rsi"] = CrsiLowerRsi;
            
            // ConnorsRSI_v2 Entry
            parameters["connorsv2_rsi_period"] = ConnorsV2RsiPeriod;
            parameters["connorsv2_upper_rsi"] = 100 - ConnorsEntry;
            parameters["connorsv2_lower_rsi"] = ConnorsEntry;
            
            // BB_Stretch Entry
            parameters["bb_window"] = BbWindow;
            parameters["bb_std"] = BbStdDev;
            
            // MA_Stretch Entry
            parameters["stretch_ma_period"] = MaStretchMaPeriod;
            parameters["stretch_atr_period"] = MaStretchAtrPeriod;
            parameters["filter_mode"] = MaStretchFilterMode;
            parameters["price_ma_threshold"] = MaStretchPriceMaThreshold;
            parameters["trend_ma_period"] = MaStretchTrendPeriod;
            parameters["volatility_threshold"] = MaStretchVolatilityThreshold;
            
            // MR_BollingerRSI Entry
            parameters["window"] = MrBbWindow;
            parameters["std_dev"] = MrBbStdDev;
            parameters["rsi_window"] = MrBbRsiWindow;
            parameters["oversold_threshold"] = MrBbOversold;
            parameters["overbought_threshold"] = MrBbOverbought;
            
            // MR_MAReversion Entry
            parameters["mr_ma_period"] = MrMaPeriod;
            parameters["mr_ma_type"] = MrMaType.ToString();
            
            // MR_StochasticVolume Entry
            parameters["k_period"] = StochKPeriod;
            parameters["d_period"] = StochDPeriod;
            parameters["slowing"] = StochSlowing;
            parameters["oversold_level"] = StochOversold;
            parameters["overbought_level"] = StochOverbought;
            parameters["volume_threshold"] = StochVolumeThreshold;
            
            // TF_EMACrossover Entry
            parameters["short_window"] = TfShortWindow;
            parameters["long_window"] = TfLongWindow;
            parameters["trend_window"] = TfTrendWindow;
            
            // KD_MAStretch Entry
            parameters["kd_ma_period"] = KdMaPeriod;
            parameters["kd_stretch"] = KdEffectiveStretch;
            parameters["kd_stretch_mode"] = KdStretchMode;
            parameters["kd_atr_multiple"] = KdAtrMultiple;
            parameters["kd_atr_period"] = KdAtrPeriod;
            
            return parameters;
        }

        // Get exit parameters from cTrader UI inputs (all strategies are adjustable)
        private Dictionary<string, object> GetExitParameters()
        {
            var parameters = new Dictionary<string, object>();
            
            // RSI_Exit
            parameters["rsi_period"] = ExitRsiPeriod;
            parameters["exit_overbought"] = ExitOverbought;
            parameters["exit_oversold"] = ExitOversold;

            // Trend-based RSI exit parameters
            parameters["use_trend_rsi_filter"] = UseTrendRsiFilter;
            parameters["rsi_trend_ma_period"] = RsiTrendMaPeriod;
            parameters["rsi_adx_period"] = RsiAdxPeriod;
            parameters["rsi_adx_trending_threshold"] = RsiAdxTrendingThreshold;
            
            // Ranging parameters
            parameters["ranging_rsi_period"] = RangingRsiPeriod;
            parameters["ranging_overbought"] = RangingOverbought;
            parameters["ranging_oversold"] = RangingOversold;
            
            // Trending up parameters
            parameters["trending_up_rsi_period"] = TrendingUpRsiPeriod;
            parameters["trending_up_overbought"] = TrendingUpOverbought;
            parameters["trending_up_oversold"] = TrendingUpOversold;
            
            // Trending down parameters
            parameters["trending_down_rsi_period"] = TrendingDownRsiPeriod;
            parameters["trending_down_overbought"] = TrendingDownOverbought;
            parameters["trending_down_oversold"] = TrendingDownOversold;            
            
            // BB_Exit
            parameters["bb_window"] = ExitBbWindow;
            parameters["bb_std"] = ExitBbStdDev;
            
            // MA_Exit
            parameters["exit_ma_period"] = ExitMaPeriod;
            parameters["exit_ma_type"] = ExitMaType.ToString();
            
            // Stretch_Exit
            parameters["stretch_exit_ma_period"] = StretchExitMaPeriod;
            parameters["stretch_exit_atr_period"] = StretchExitAtrPeriod;
            parameters["stretch_exit_atr_multiple"] = StretchExitAtrMultiple;
            
            // KD_MAStretch_Exit
            parameters["kd_ma_period"] = KdMaPeriod;
            parameters["kd_exit_on_ma"] = KdExitOnMa;
            
            return parameters;
        }

        // KD_MAStretch readout: how far closes actually get from the MA on this symbol and
        // timeframe, so the stretch can be set from data instead of guesses. Distances are
        // collected on every closed bar (RecordStretchStats) and printed at the end of the run
        private void RecordStretchStats(int index)
        {
            double ma = _kdStatsMa.Result[index];
            double atr = _kdStatsAtr.Result[index];
            if (double.IsNaN(ma) || double.IsNaN(atr) || ma <= 0 || atr <= 0)
                return;
            
            double distance = Math.Abs(Bars.ClosePrices[index] - ma);
            _kdPercentDistances.Add(distance / ma);
            _kdAtrDistances.Add(distance / atr);
        }
        
        private void PrintStretchStats()
        {
            var percentDistances = new List<double>(_kdPercentDistances);
            var atrDistances = new List<double>(_kdAtrDistances);
            
            if (percentDistances.Count < 100)
            {
                Print($"KD_MAStretch stats: only {percentDistances.Count} usable bars ({Bars.Count} bars loaded), need at least 100 for a useful readout");
                return;
            }
            
            percentDistances.Sort();
            atrDistances.Sort();
            
            double threshold = KdStretchMode == KdStretchMode.AtrMultiple ? KdAtrMultiple : KdEffectiveStretch;
            var distances = KdStretchMode == KdStretchMode.AtrMultiple ? atrDistances : percentDistances;
            int beyond = distances.Count - distances.FindLastIndex(d => d <= threshold) - 1;
            
            Print($"KD_MAStretch stats: {percentDistances.Count} bars, {SymbolName} {TimeFrame}, MA {KdMaPeriod}, ATR {KdAtrPeriod}");
            Print($"  Distance from MA in % of MA - 90th {Percentile(percentDistances, 0.90):P2}, 95th {Percentile(percentDistances, 0.95):P2}, 98th {Percentile(percentDistances, 0.98):P2}");
            Print($"  Distance from MA in ATRs    - 90th {Percentile(atrDistances, 0.90):F2}, 95th {Percentile(atrDistances, 0.95):F2}, 98th {Percentile(atrDistances, 0.98):F2}");
            Print($"  Current setting ({KdStretchMode}) is reached on {(double)beyond / distances.Count:P1} of closes, either side of the MA");
        }
        
        private static double Percentile(List<double> sorted, double fraction)
        {
            return sorted[(int)Math.Floor(fraction * (sorted.Count - 1))];
        }

        protected override void OnStop()
        {
            if (PrintKdStretchStats)
            {
                PrintStretchStats();
            }

            Print("Multi-Strategy Framework stopped");
        }
        
        #region Market Condition Methods
        private void UpdateMarketConditions(int index)
        {
            // Calculate volatility (ATR normalized by price)
            double currentVolatility = _atr.Result[index] / Bars.ClosePrices[index];
            // Calculate trend strength (ADX)
            double currentTrendStrength = _adx.ADX[index];
            
            // Classify regime based on thresholds
            bool isHighVol = currentVolatility > VolatilityHighThreshold;
            bool isTrending = currentTrendStrength > AdxTrendingThreshold;
            
            if (!isHighVol && !isTrending)
                _currentRegime = MarketRegime.LowVolRanging;
            else if (isHighVol && !isTrending)
                _currentRegime = MarketRegime.HighVolRanging;
            else
                _currentRegime = MarketRegime.HighVolTrending;
            
            Log($"Market regime: {_currentRegime}, Volatility={currentVolatility:F4}, ADX={currentTrendStrength:F2}");
        }
        
        private Dictionary<string, object> AdjustParametersBasedOnMarketConditions(Dictionary<string, object> parameters)
        {
            // Create a copy of the parameters
            var adjustedParams = new Dictionary<string, object>(parameters);
            
            // Adjust strategy parameters based on regime
            switch (_currentRegime)
            {
                case MarketRegime.LowVolRanging:
                    // Mean reversion works best here - use conservative, stable parameters
                    AdjustParametersForLowVolRanging(adjustedParams);
                    break;
                    
                case MarketRegime.HighVolRanging:
                    // Higher volatility - widen bands, tighten stops
                    AdjustParametersForHighVolRanging(adjustedParams);
                    break;
                    
                case MarketRegime.HighVolTrending:
                    // Mean reversion dangerous here - more conservative parameters
                    AdjustParametersForHighVolTrending(adjustedParams);
                    break;
            }
            
            return adjustedParams;
        }
        
        private double GetCurrentRegimeSizeMultiplier()
        {
            switch (_currentRegime)
            {
                case MarketRegime.LowVolRanging:
                    return LowVolRangingSizeMult;
                case MarketRegime.HighVolRanging:
                    return HighVolRangingSizeMult;
                case MarketRegime.HighVolTrending:
                    return HighVolTrendingSizeMult;
                default:
                    return 1.0;
            }
        }
        
        private void AdjustParametersForLowVolRanging(Dictionary<string, object> parameters)
        {
            // Low volatility ranging: tighter bands, mean reversion works well
            // Widen RSI thresholds to capture more opportunities
            if (parameters.ContainsKey("oversold_threshold"))
                parameters["oversold_threshold"] = (int)parameters["oversold_threshold"] + 2;
            if (parameters.ContainsKey("overbought_threshold"))
                parameters["overbought_threshold"] = (int)parameters["overbought_threshold"] - 2;

            // Standard MA periods work well - keep as-is

            Log("Applied LowVolRanging parameter adjustments");
        }
        
        private void AdjustParametersForHighVolRanging(Dictionary<string, object> parameters)
        {
            // High volatility ranging: widen bands to avoid false breaks
            if (parameters.ContainsKey("oversold_threshold"))
                parameters["oversold_threshold"] = Math.Max(10, (int)parameters["oversold_threshold"] - 5);
            if (parameters.ContainsKey("overbought_threshold"))
                parameters["overbought_threshold"] = Math.Min(90, (int)parameters["overbought_threshold"] + 5);
            
            // Widen Bollinger bands effectively by increasing std dev implicitly through earlier sizing
            if (parameters.ContainsKey("std_dev"))
                parameters["std_dev"] = (double)parameters["std_dev"] * 1.2;
            
            Log("Applied HighVolRanging parameter adjustments");
        }
        
        private void AdjustParametersForHighVolTrending(Dictionary<string, object> parameters)
        {
            // High volatility trending: mean reversion is risky, tighten everything
            if (parameters.ContainsKey("oversold_threshold"))
                parameters["oversold_threshold"] = Math.Max(5, (int)parameters["oversold_threshold"] - 10);
            if (parameters.ContainsKey("overbought_threshold"))
                parameters["overbought_threshold"] = Math.Min(95, (int)parameters["overbought_threshold"] + 10);
            
            // Use longer MAs to avoid noise
            foreach (string key in new[] { "stretch_ma_period", "mr_ma_period", "exit_ma_period", "stretch_exit_ma_period" })
            {
                // Lengthen by 10 up to a cap of 100; periods already above 100 are left alone
                if (parameters.ContainsKey(key) && (int)parameters[key] < 100)
                    parameters[key] = Math.Min(100, (int)parameters[key] + 10);
            }
            
            // For RSI exits, widen targets more to let trend run
            if (parameters.ContainsKey("exit_overbought"))
                parameters["exit_overbought"] = Math.Min(85, (int)parameters["exit_overbought"] + 5);
            if (parameters.ContainsKey("exit_oversold"))
                parameters["exit_oversold"] = Math.Max(15, (int)parameters["exit_oversold"] - 5);
            
            Log("Applied HighVolTrending parameter adjustments");
        }
        
        // Check if we should skip entry based on regime
        private bool ShouldSkipEntry()
        {
            if (!UseMarketConditions)
                return false;
            
            return SkipEntriesInTrending && _currentRegime == MarketRegime.HighVolTrending;
        }

        // Switch entry/exit strategy based on current market regime
        private void SwitchStrategyByRegime()
        {
            EntryStrategyType newEntryStrategy = EntryStrategyName;
            ExitStrategyType newExitStrategy = ExitStrategyName;

            switch (_currentRegime)
            {
                case MarketRegime.LowVolRanging:
                    newEntryStrategy = LowVolRangingEntry;
                    newExitStrategy = LowVolRangingExit;
                    break;
                case MarketRegime.HighVolRanging:
                    newEntryStrategy = HighVolRangingEntry;
                    newExitStrategy = HighVolRangingExit;
                    break;
                case MarketRegime.HighVolTrending:
                    newEntryStrategy = HighVolTrendingEntry;
                    newExitStrategy = HighVolTrendingExit;
                    break;
            }

            // Only switch if strategy has actually changed
            bool strategyChanged = false;
            if (newEntryStrategy.ToString() != _currentEntry.Name)
            {
                _currentEntry = _entryStrategies[newEntryStrategy.ToString()];
                strategyChanged = true;
            }
            
            if (newExitStrategy.ToString() != _currentExit.Name)
            {
                _currentExit = _exitStrategies[newExitStrategy.ToString()];
                strategyChanged = true;
            }

            if (strategyChanged)
            {
                Log($"Strategy switched to: {_currentEntry.Name} + {_currentExit.Name} (Regime: {_currentRegime})");
            }
        }
        #endregion

        #region Stop Loss Methods
        // Determine trend direction (up, down, or ranging)
        private enum TrendDirection
        {
            Up,
            Down,
            Ranging
        }

        private TrendDirection GetTrendDirection(int index)
        {
            if (_adx == null || _maFast == null || _maSlow == null)
                return TrendDirection.Ranging;

            double adxValue = _adx.ADX[index];
            bool isTrending = adxValue > AdxTrendingThreshold;

            if (!isTrending)
                return TrendDirection.Ranging;

            double maFastValue = _maFast.Result[index];
            double maSlowValue = _maSlow.Result[index];

            if (maFastValue > maSlowValue)
                return TrendDirection.Up;
            else
                return TrendDirection.Down;
        }

        // Calculate the highest high in the lookback period
        private double CalculateHighestHigh(Bars bars, int index, int period)
        {
            int startIndex = Math.Max(0, index - period + 1);
            double highest = bars.HighPrices[startIndex];
            
            for (int i = startIndex + 1; i <= index; i++)
            {
                if (bars.HighPrices[i] > highest)
                    highest = bars.HighPrices[i];
            }
            
            return highest;
        }

        // Calculate the lowest low in the lookback period
        private double CalculateLowestLow(Bars bars, int index, int period)
        {
            int startIndex = Math.Max(0, index - period + 1);
            double lowest = bars.LowPrices[startIndex];
            
            for (int i = startIndex + 1; i <= index; i++)
            {
                if (bars.LowPrices[i] < lowest)
                    lowest = bars.LowPrices[i];
            }
            
            return lowest;
        }

        // Convert threshold from pips or percent to price
        private double ConvertThresholdToPrice(double threshold, StopLossThresholdType type, double currentPrice)
        {
            if (type == StopLossThresholdType.Pips)
            {
                return threshold * Symbol.PipSize;
            }
            else
            {
                return currentPrice * (threshold / 100.0);
            }
        }

        // Calculate initial stop loss level when opening a position
        private double CalculateInitialStopLoss(TradeType tradeType, int index)
        {
            TrendDirection trend = GetTrendDirection(index);
            double currentPrice = Bars.ClosePrices[index];
            double threshold;
            double stopLossPrice;

            if (trend == TrendDirection.Ranging)
            {
                threshold = ConvertThresholdToPrice(RangingStopLossThreshold, StopLossThresholdType, currentPrice);
                if (tradeType == TradeType.Buy)
                {
                    double lowestLow = CalculateLowestLow(Bars, index, StopLossLookbackPeriod);
                    stopLossPrice = lowestLow - threshold;
                }
                else
                {
                    double highestHigh = CalculateHighestHigh(Bars, index, StopLossLookbackPeriod);
                    stopLossPrice = highestHigh + threshold;
                }
            }
            else if (trend == TrendDirection.Up)
            {
                threshold = ConvertThresholdToPrice(TrendingUpStopLossThreshold, StopLossThresholdType, currentPrice);
                if (tradeType == TradeType.Buy)
                {
                    // For long positions in uptrend: below recent low or below entry by threshold
                    double lowestLow = CalculateLowestLow(Bars, index, StopLossLookbackPeriod);
                    stopLossPrice = Math.Min(lowestLow - threshold, currentPrice - threshold);
                }
                else
                {
                    // For short positions in uptrend: above recent high or above entry by threshold
                    double highestHigh = CalculateHighestHigh(Bars, index, StopLossLookbackPeriod);
                    stopLossPrice = Math.Max(highestHigh + threshold, currentPrice + threshold);
                }
            }
            else // TrendDirection.Down
            {
                threshold = ConvertThresholdToPrice(TrendingDownStopLossThreshold, StopLossThresholdType, currentPrice);
                if (tradeType == TradeType.Buy)
                {
                    // For long positions in downtrend: below recent low or below entry by threshold
                    double lowestLow = CalculateLowestLow(Bars, index, StopLossLookbackPeriod);
                    stopLossPrice = Math.Min(lowestLow - threshold, currentPrice - threshold);
                }
                else
                {
                    // For short positions in downtrend: above recent high or above entry by threshold
                    double highestHigh = CalculateHighestHigh(Bars, index, StopLossLookbackPeriod);
                    stopLossPrice = Math.Max(highestHigh + threshold, currentPrice + threshold);
                }
            }

            Log($"Initial Stop Loss set for {tradeType}: {stopLossPrice:F5} (Trend: {trend})");
            return stopLossPrice;
        }
        #endregion
    }

    #region Entry Strategy Implementations
    // RSI Entry Strategy
    public class RSIEntry : EntryStrategy
    {
        public override string Name => "RSI";
        
        public override bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int rsiPeriod = (int)parameters["rsi_period"];
            int oversold = (int)parameters["oversold"];
            
            var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
            return rsi.Result[index] < oversold && rsi.Result[index - 1] >= oversold;
        }
        
        public override bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int rsiPeriod = (int)parameters["rsi_period"];
            int overbought = (int)parameters["overbought"];
            
            var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
            return rsi.Result[index] > overbought && rsi.Result[index - 1] <= overbought;
        }
    }

    // ConnorsRSI Entry Strategy (simplified)
    public class ConnorsRSIEntry : EntryStrategy
    {
        public override string Name => "ConnorsRSI";
        
        public override bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int rsiPeriod = (int)parameters["crsi_rsi_period"];
            int lowerRsi = (int)parameters["lower_rsi"];
            
            var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
            return rsi.Result[index] < lowerRsi;
        }
        
        public override bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int rsiPeriod = (int)parameters["crsi_rsi_period"];
            int upperRsi = (int)parameters["upper_rsi"];
            
            var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
            return rsi.Result[index] > upperRsi;
        }
    }

    // ConnorsRSI_v2 Entry Strategy
    public class ConnorsRSIEntryV2 : EntryStrategy
    {
        public override string Name => "ConnorsRSI_v2";
        
        public override bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int rsiPeriod = (int)parameters["connorsv2_rsi_period"];
            double lowerRsi = (double)parameters["connorsv2_lower_rsi"];
            
            var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
            return rsi.Result[index] < lowerRsi;
        }
        
        public override bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int rsiPeriod = (int)parameters["connorsv2_rsi_period"];
            double upperRsi = (double)parameters["connorsv2_upper_rsi"];
            
            var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
            return rsi.Result[index] > upperRsi;
        }
    }

    // BB_Stretch Entry Strategy
    public class BBStretchEntry : EntryStrategy
    {
        public override string Name => "BB_Stretch";
        
        public override bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int bbWindow = (int)parameters["bb_window"];
            double bbStd = (double)parameters["bb_std"];
            
            var bb = Indicators.BollingerBands(bars.ClosePrices, bbWindow, bbStd, MovingAverageType.Simple);
            return bars.LowPrices[index] < bb.Bottom[index];
        }
        
        public override bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int bbWindow = (int)parameters["bb_window"];
            double bbStd = (double)parameters["bb_std"];
            
            var bb = Indicators.BollingerBands(bars.ClosePrices, bbWindow, bbStd, MovingAverageType.Simple);
            return bars.HighPrices[index] > bb.Top[index];
        }
    }

    // MA_Stretch Entry Strategy
    public class MAStretchEntry : EntryStrategy
    {
        public override string Name => "MA_Stretch";
        
        public override bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int maPeriod = (int)parameters["stretch_ma_period"];
            int atrPeriod = (int)parameters["stretch_atr_period"];
            
            var ma = Indicators.SimpleMovingAverage(bars.ClosePrices, maPeriod);
            var atr = Indicators.AverageTrueRange(atrPeriod, MovingAverageType.Simple);
            
            // Long: Price stretches below MA by at least 1 ATR
            double stretch = ma.Result[index] - bars.LowPrices[index];
            return stretch >= atr.Result[index];
        }
        
        public override bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int maPeriod = (int)parameters["stretch_ma_period"];
            int atrPeriod = (int)parameters["stretch_atr_period"];
            
            var ma = Indicators.SimpleMovingAverage(bars.ClosePrices, maPeriod);
            var atr = Indicators.AverageTrueRange(atrPeriod, MovingAverageType.Simple);
            
            // Short: Price stretches above MA by at least 1 ATR
            double stretch = bars.HighPrices[index] - ma.Result[index];
            return stretch >= atr.Result[index];
        }
    }

    // MR_BollingerRSI Entry Strategy
    public class MRBollingerRSIEntry : EntryStrategy
    {
        public override string Name => "MR_BollingerRSI";
        
        public override bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int window = (int)parameters["window"];
            double stdDev = (double)parameters["std_dev"];
            int rsiWindow = (int)parameters["rsi_window"];
            int oversoldThreshold = (int)parameters["oversold_threshold"];
            
            var bb = Indicators.BollingerBands(bars.ClosePrices, window, stdDev, MovingAverageType.Simple);
            var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiWindow);
            
            return bars.LowPrices[index] < bb.Bottom[index] && rsi.Result[index] < oversoldThreshold;
        }
        
        public override bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int window = (int)parameters["window"];
            double stdDev = (double)parameters["std_dev"];
            int rsiWindow = (int)parameters["rsi_window"];
            int overboughtThreshold = (int)parameters["overbought_threshold"];
            
            var bb = Indicators.BollingerBands(bars.ClosePrices, window, stdDev, MovingAverageType.Simple);
            var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiWindow);
            
            return bars.HighPrices[index] > bb.Top[index] && rsi.Result[index] > overboughtThreshold;
        }
    }

    // MR_MAReversion Entry Strategy
    public class MRMAReversionEntry : EntryStrategy
    {
        public override string Name => "MR_MAReversion";
        
        public override bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int maPeriod = (int)parameters["mr_ma_period"];
            
            var ma = Indicators.SimpleMovingAverage(bars.ClosePrices, maPeriod);
            // Price crosses below MA and then back above (simplified)
            return bars.ClosePrices[index] < ma.Result[index] && bars.ClosePrices[index - 1] >= ma.Result[index - 1];
        }
        
        public override bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int maPeriod = (int)parameters["mr_ma_period"];
            
            var ma = Indicators.SimpleMovingAverage(bars.ClosePrices, maPeriod);
            // Price crosses above MA and then back below (simplified)
            return bars.ClosePrices[index] > ma.Result[index] && bars.ClosePrices[index - 1] <= ma.Result[index - 1];
        }
    }

    // MR_StochasticVolume Entry Strategy
    public class MRStochasticVolumeEntry : EntryStrategy
    {
        public override string Name => "MR_StochasticVolume";
        
        public override bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int kPeriod = (int)parameters["k_period"];
            int dPeriod = (int)parameters["d_period"];
            int slowing = (int)parameters["slowing"];
            int oversoldLevel = (int)parameters["oversold_level"];
            
            var stochastic = Indicators.StochasticOscillator(bars, kPeriod, dPeriod, slowing, MovingAverageType.Simple);
            return stochastic.PercentK[index] < oversoldLevel;
        }
        
        public override bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int kPeriod = (int)parameters["k_period"];
            int dPeriod = (int)parameters["d_period"];
            int slowing = (int)parameters["slowing"];
            int overboughtLevel = (int)parameters["overbought_level"];
            
            var stochastic = Indicators.StochasticOscillator(bars, kPeriod, dPeriod, slowing, MovingAverageType.Simple);
            return stochastic.PercentK[index] > overboughtLevel;
        }
    }

    // TF_EMACrossover Entry Strategy
    public class TFEMACrossoverEntry : EntryStrategy
    {
        public override string Name => "TF_EMACrossover";
        
        public override bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int shortWindow = (int)parameters["short_window"];
            int longWindow = (int)parameters["long_window"];
            
            var emaShort = Indicators.ExponentialMovingAverage(bars.ClosePrices, shortWindow);
            var emaLong = Indicators.ExponentialMovingAverage(bars.ClosePrices, longWindow);
            
            return emaShort.Result[index] > emaLong.Result[index] && emaShort.Result[index - 1] <= emaLong.Result[index - 1];
        }
        
        public override bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            int shortWindow = (int)parameters["short_window"];
            int longWindow = (int)parameters["long_window"];
            
            var emaShort = Indicators.ExponentialMovingAverage(bars.ClosePrices, shortWindow);
            var emaLong = Indicators.ExponentialMovingAverage(bars.ClosePrices, longWindow);
            
            return emaShort.Result[index] < emaLong.Result[index] && emaShort.Result[index - 1] >= emaLong.Result[index - 1];
        }
    }

    // KD_MAStretch - Kevin Davey's moving average stretch entry: the close is far from its
    // InputVar2-length MA. "Far" is a % of the MA (Percent mode) or a multiple of ATR (ATR mode);
    // see the KD_MAStretch notes above the robot's parameters
    public class KDMAStretchEntry : EntryStrategy
    {
        public override string Name => "KD_MAStretch";
        
        public override bool CheckLongSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            double ma = Indicators.SimpleMovingAverage(bars.ClosePrices, (int)parameters["kd_ma_period"]).Result[index];
            
            // Long: the close is below the MA by more than the stretch
            return ma - bars.ClosePrices[index] > StretchDistance(index, ma, parameters);
        }
        
        public override bool CheckShortSignal(Bars bars, int index, Dictionary<string, object> parameters)
        {
            double ma = Indicators.SimpleMovingAverage(bars.ClosePrices, (int)parameters["kd_ma_period"]).Result[index];
            
            // Short: the close is above the MA by more than the stretch
            return bars.ClosePrices[index] - ma > StretchDistance(index, ma, parameters);
        }
        
        // The stretch as a price distance from the MA
        private double StretchDistance(int index, double ma, Dictionary<string, object> parameters)
        {
            if ((KdStretchMode)parameters["kd_stretch_mode"] == KdStretchMode.AtrMultiple)
            {
                var atr = Indicators.AverageTrueRange((int)parameters["kd_atr_period"], MovingAverageType.Simple);
                return (double)parameters["kd_atr_multiple"] * atr.Result[index];
            }
            
            // Kevin's InputVar33 x MA, scaled for the market being traded
            return (double)parameters["kd_stretch"] * ma;
        }
    }
    #endregion

    #region Exit Strategy Implementations
    // RSI Exit Strategy
    public class RSIExit : ExitStrategy
    {
        public override string Name => "RSI_Exit";
        
        private DirectionalMovementSystem GetADX(Bars bars, int period)
        {
            return Indicators.DirectionalMovementSystem(period);
        }
        
        private SimpleMovingAverage GetMA(Bars bars, int period)
        {
            return Indicators.SimpleMovingAverage(bars.ClosePrices, period);
        }
        
        public override bool CheckLongExit(Bars bars, int index, Position position, Dictionary<string, object> parameters)
        {
            bool useTrendFilter = (bool)parameters["use_trend_rsi_filter"];
            
            if (useTrendFilter)
            {
                int adxPeriod = (int)parameters["rsi_adx_period"];
                double adxThreshold = (double)parameters["rsi_adx_trending_threshold"];
                int maPeriod = (int)parameters["rsi_trend_ma_period"];
                
                var adx = GetADX(bars, adxPeriod);
                var ma = GetMA(bars, maPeriod);
                bool isTrending = adx.ADX[index] > adxThreshold;
                bool isUptrend = bars.ClosePrices[index] > ma.Result[index];
                
                if (isTrending)
                {
                    if (isUptrend)
                    {
                        int rsiPeriod = (int)parameters["trending_up_rsi_period"];
                        int overbought = (int)parameters["trending_up_overbought"];
                        var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
                        return rsi.Result[index] > overbought;
                    }
                    else
                    {
                        int rsiPeriod = (int)parameters["trending_down_rsi_period"];
                        int overbought = (int)parameters["trending_down_overbought"];
                        var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
                        return rsi.Result[index] > overbought;
                    }
                }
                else
                {
                    int rsiPeriod = (int)parameters["ranging_rsi_period"];
                    int overbought = (int)parameters["ranging_overbought"];
                    var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
                    return rsi.Result[index] > overbought;
                }
            }
            else
            {
                int rsiPeriod = (int)parameters["rsi_period"];
                int overbought = (int)parameters["exit_overbought"];
                var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
                return rsi.Result[index] > overbought;
            }
        }
        
        public override bool CheckShortExit(Bars bars, int index, Position position, Dictionary<string, object> parameters)
        {
            bool useTrendFilter = (bool)parameters["use_trend_rsi_filter"];
            
            if (useTrendFilter)
            {
                int adxPeriod = (int)parameters["rsi_adx_period"];
                double adxThreshold = (double)parameters["rsi_adx_trending_threshold"];
                int maPeriod = (int)parameters["rsi_trend_ma_period"];
                
                var adx = GetADX(bars, adxPeriod);
                var ma = GetMA(bars, maPeriod);
                bool isTrending = adx.ADX[index] > adxThreshold;
                bool isUptrend = bars.ClosePrices[index] > ma.Result[index];
                
                if (isTrending)
                {
                    if (isUptrend)
                    {
                        int rsiPeriod = (int)parameters["trending_up_rsi_period"];
                        int oversold = (int)parameters["trending_up_oversold"];
                        var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
                        return rsi.Result[index] < oversold;
                    }
                    else
                    {
                        int rsiPeriod = (int)parameters["trending_down_rsi_period"];
                        int oversold = (int)parameters["trending_down_oversold"];
                        var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
                        return rsi.Result[index] < oversold;
                    }
                }
                else
                {
                    int rsiPeriod = (int)parameters["ranging_rsi_period"];
                    int oversold = (int)parameters["ranging_oversold"];
                    var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
                    return rsi.Result[index] < oversold;
                }
            }
            else
            {
                int rsiPeriod = (int)parameters["rsi_period"];
                int oversold = (int)parameters["exit_oversold"];
                var rsi = Indicators.RelativeStrengthIndex(bars.ClosePrices, rsiPeriod);
                return rsi.Result[index] < oversold;
            }
        }
    }

    // BB_Exit Strategy
    public class BollingerExit : ExitStrategy
    {
        public override string Name => "BB_Exit";
        
        public override bool CheckLongExit(Bars bars, int index, Position position, Dictionary<string, object> parameters)
        {
            int bbWindow = (int)parameters["bb_window"];
            double bbStd = (double)parameters["bb_std"];
            
            var bb = Indicators.BollingerBands(bars.ClosePrices, bbWindow, bbStd, MovingAverageType.Simple);
            return bars.HighPrices[index] > bb.Top[index];
        }
        
        public override bool CheckShortExit(Bars bars, int index, Position position, Dictionary<string, object> parameters)
        {
            int bbWindow = (int)parameters["bb_window"];
            double bbStd = (double)parameters["bb_std"];
            
            var bb = Indicators.BollingerBands(bars.ClosePrices, bbWindow, bbStd, MovingAverageType.Simple);
            return bars.LowPrices[index] < bb.Bottom[index];
        }
    }

    // MA_Exit Strategy
    public class MAExit : ExitStrategy
    {
        public override string Name => "MA_Exit";
        
        public override bool CheckLongExit(Bars bars, int index, Position position, Dictionary<string, object> parameters)
        {
            int maPeriod = (int)parameters["exit_ma_period"];
            string maType = (string)parameters["exit_ma_type"];
            
            MovingAverage ma;
            if (maType == "exponential")
                ma = Indicators.ExponentialMovingAverage(bars.ClosePrices, maPeriod);
            else
                ma = Indicators.SimpleMovingAverage(bars.ClosePrices, maPeriod);
            
            return bars.ClosePrices[index] >= ma.Result[index];
        }
        
        public override bool CheckShortExit(Bars bars, int index, Position position, Dictionary<string, object> parameters)
        {
            int maPeriod = (int)parameters["exit_ma_period"];
            string maType = (string)parameters["exit_ma_type"];
            
            MovingAverage ma;
            if (maType == "exponential")
                ma = Indicators.ExponentialMovingAverage(bars.ClosePrices, maPeriod);
            else
                ma = Indicators.SimpleMovingAverage(bars.ClosePrices, maPeriod);
            
            return bars.ClosePrices[index] <= ma.Result[index];
        }
    }
    
    // Stretch_Exit Strategy
    public class StretchExit : ExitStrategy
    {
        public override string Name => "Stretch_Exit";
        
        public override bool CheckLongExit(Bars bars, int index, Position position, Dictionary<string, object> parameters)
        {
            int maPeriod = (int)parameters["stretch_exit_ma_period"];
            int atrPeriod = (int)parameters["stretch_exit_atr_period"];
            
            var ma = Indicators.SimpleMovingAverage(bars.ClosePrices, maPeriod);
            var atr = Indicators.AverageTrueRange(atrPeriod, MovingAverageType.Simple);
            
            double currentPrice = bars.ClosePrices[index];
            double maValue = ma.Result[index];
            double atrValue = atr.Result[index];
            
            // Exit once price has reverted to within N ATRs of the MA
            double threshold = (double)parameters["stretch_exit_atr_multiple"];
            double stretch = Math.Abs(currentPrice - maValue) / atrValue;
            return stretch <= threshold;
        }

        public override bool CheckShortExit(Bars bars, int index, Position position, Dictionary<string, object> parameters)
        {
            int maPeriod = (int)parameters["stretch_exit_ma_period"];
            int atrPeriod = (int)parameters["stretch_exit_atr_period"];
            
            var ma = Indicators.SimpleMovingAverage(bars.ClosePrices, maPeriod);
            var atr = Indicators.AverageTrueRange(atrPeriod, MovingAverageType.Simple);
            
            double currentPrice = bars.ClosePrices[index];
            double maValue = ma.Result[index];
            double atrValue = atr.Result[index];

            // Exit once price has reverted to within N ATRs of the MA
            double threshold = (double)parameters["stretch_exit_atr_multiple"];
            double stretch = Math.Abs(currentPrice - maValue) / atrValue;
            return stretch <= threshold;
        }
    }

    // KD_MAStretch_Exit - Kevin Davey's moving average stretch exit: leave once the close
    // crosses back over the same InputVar2-length MA the entry used (only when InputVar4 is on)
    public class KDMAStretchExit : ExitStrategy
    {
        public override string Name => "KD_MAStretch_Exit";
        
        public override bool CheckLongExit(Bars bars, int index, Position position, Dictionary<string, object> parameters)
        {
            if (!(bool)parameters["kd_exit_on_ma"])
                return false;
            
            var ma = Indicators.SimpleMovingAverage(bars.ClosePrices, (int)parameters["kd_ma_period"]);
            return bars.ClosePrices[index] > ma.Result[index];
        }
        
        public override bool CheckShortExit(Bars bars, int index, Position position, Dictionary<string, object> parameters)
        {
            if (!(bool)parameters["kd_exit_on_ma"])
                return false;
            
            var ma = Indicators.SimpleMovingAverage(bars.ClosePrices, (int)parameters["kd_ma_period"]);
            return bars.ClosePrices[index] < ma.Result[index];
        }
    }
    #endregion
}
