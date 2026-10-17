# KD_MAStretch — EURUSD D1 test log

Kevin Davey's moving average stretch, run through `test9_multi_optimized`.
Process: optimize on 2020–2023 (in-sample), then run the **same parameters, unchanged**
on 2024 → today (out-of-sample). Do not re-optimize or tweak after seeing the
out-of-sample result.

## Parameters (chosen from the 2020–2023 optimization)

Saved in full in `2020-2023_in-sample/parameters.cbotset` — load that file in cTrader for
the out-of-sample run so nothing changes by accident.

| Setting | Value |
|---|---|
| Symbol / timeframe | EURUSD D1 |
| Starting capital | 10,000 |
| Entry / Exit | KD_MAStretch / KD_MAStretch_Exit |
| MA Length (InputVar2) | 20 |
| Stretch Mode | AtrMultiple |
| ATR Multiple / ATR Period | 2.5 / 7 |
| Exit On MA Cross (InputVar4) | on |
| Position Mode / Max Concurrent | **Multiple / 5** (not Kevin's Single — adds up to 5 positions while stretched) |
| Max Bars | **30** (time exit is active) |
| Stop loss | off |
| Use Market Conditions / ADX block | off / off |
| Sizing | Percentage, 3% → 1,000 units (0.01 lot) per trade |

## In-sample: 1 Jan 2020 – 29 Dec 2023

| Metric | All | Long | Short |
|---|---|---|---|
| Net profit | **+467.36** (+4.7%) | +240.23 | +227.13 |
| Profit factor | **2.79** | 2.96 | 2.63 |
| Trades | **86** | 48 | 38 |
| Winning trades | 57 (66%) | 30 | 27 |
| Average trade | **+5.43** | +5.00 | +5.98 |
| Largest win / loss | +44.46 / −26.91 | | |
| Max consecutive losses | 8 | | |
| Swaps | **−217.84** | −210.79 | −7.05 |
| Commissions | −5.16 | | |

- Max equity drawdown: **198.08 (1.94%)**; max balance drawdown: 141.39 (1.39%)
- Ending equity: 10,467.36

Per year (closed trades, net after costs):

| Year | Trades closed | Net |
|---|---|---|
| 2020 | 19 | +99.29 |
| 2021 | 28 | −44.49 |
| 2022 | 18 | +232.51 |
| 2023 | 21 | +180.05 |
| **Average** | **21.5 / year** | **+116.84 / year** |

### Neighbour check: MA 15 and MA 25 (same settings otherwise, 2020–2023)

| Metric | MA 15 | MA 20 (chosen) | MA 25 |
|---|---|---|---|
| Net profit | +202.68 | **+467.36** | +196.76 |
| Profit factor | 2.28 | **2.79** | 1.38 |
| Trades | 51 | 86 | 103 |
| Win rate | 61% (31/51) | **66%** | 54% (56/103) |
| Average trade | +3.97 | **+5.43** | +1.91 |
| Max equity drawdown | 232.35 (2.30%) | **198.08 (1.94%)** | 329.31 (3.26%) |
| Max consecutive losses | | 8 | 11 |
| Swaps | −95.61 | −217.84 | −300.37 |
| Losing years | | 2021 | 2020, 2021 |

MA 25 per year: 2020 −49.63, 2021 −125.78, 2022 +192.10, 2023 +180.07.

MA 20 is the best of the three. MA 15 is still solid, but MA 25 drops off sharply (profit
factor 1.38, two losing years), so MA 20 sits at the **top of a hill**, not in the middle
of a broad plateau: good on the 15 side, steep on the 25 side. Expect out-of-sample to
come in clearly below the in-sample numbers. Longer MAs hold trades longer, so swaps grow
with MA length.

**MA 20 kept for the out-of-sample test.** Decided before running 2024–today; run only
this one setting out-of-sample.

### Things to watch

- **Swaps are large:** −217.84, almost half of the net profit, nearly all on longs. If
  holding costs change, results change. Keep the same broker/swap settings for the
  out-of-sample run.
- **MA Length 20 is the top of the optimized range (5–20).** Check the neighbours in the
  optimization results (MA 15, ATR Multiple 2.0 / 3.0) — a plateau is more trustworthy
  than an edge value.
- Small size (0.01 lot) makes the dollar figures small; judge on profit factor, average
  trade, win rate and drawdown %, not dollars.
- 2021 was a losing year, so a single losing year out-of-sample isn't by itself a failure.

## Out-of-sample: 1 Jan 2024 – today

*To fill in after the run.* Save that run's files to `2024-today_out-of-sample/` before
running anything else — cTrader overwrites its Backtesting folder on every run.

| Metric | In-sample (2020–2023) | Out-of-sample (2024–today) |
|---|---|---|
| Net profit per year | +116.84 | |
| Profit factor | 2.79 | |
| Trades per year | 21.5 | |
| Win rate | 66% | |
| Average trade | +5.43 | |
| Max equity drawdown | 1.94% | |

**Rough pass guide:** profit factor still above ~1.2–1.3, profit per year at least about
half of in-sample, drawdown not much bigger. Worse than in-sample is normal; "worse but
still clearly working" is a pass.
