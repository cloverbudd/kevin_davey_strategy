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

## Out-of-sample: 1 Jan 2024 – 16 Oct 2026 (2.79 years)

Run on 17 Oct 2026 with the saved MA 20 parameter set (file checked identical to the
in-sample one). Files in `2024-today_out-of-sample/`.

| Metric | In-sample (2020–2023) | Out-of-sample (2024–2026) |
|---|---|---|
| Net profit | +467.36 | +238.51 |
| Net profit per year | +116.84 | **+85.49 (73% of in-sample)** |
| Profit factor | 2.79 | **2.57** |
| Trades per year | 21.5 | 17.6 (49 trades) |
| Win rate | 66% | 76% (37/49) |
| Average trade | +5.43 | +4.87 |
| Max equity drawdown | 1.94% | **1.61%** (166.52) |
| Max consecutive losses | 8 | 5 |
| Swaps | −217.84 | −98.38 |

Per year:

| Year | Trades closed | Net |
|---|---|---|
| 2024 | 21 | +84.65 |
| 2025 | 15 | +205.44 |
| 2026 (to 16 Oct) | 13 | −51.58 |

Long vs short:

| | Long | Short |
|---|---|---|
| Net profit | −51.01 | +289.52 |
| Trades (won) | 22 (10) | 27 (27) |
| Profit factor | 0.66 | no losing trades |
| Swaps | −94.66 | −3.72 |

**Rough pass guide:** profit factor still above ~1.2–1.3, profit per year at least about
half of in-sample, drawdown not much bigger. Worse than in-sample is normal; "worse but
still clearly working" is a pass.

### Verdict: passes the single out-of-sample test

Profit factor 2.57, 73% of the in-sample profit rate and a smaller drawdown are all
inside the pass guide. Caveats before trusting it:

- **All the profit came from shorts.** Longs lost −51.01 — before swaps they were about
  +43.65, so the long side is roughly break-even and swaps tip it negative. In-sample,
  longs had profit factor 2.96.
- **27 of 27 shorts won.** That's a lucky streak, not something to expect. With no stop
  loss, many small wins can hide the risk of one large loss.
- **49 trades is a small sample,** and 2026 is a losing year so far.
- MA 20 is a peak (MA 25 was much weaker), which usually means some decay out-of-sample.

### Next steps

1. Walk-forward windows: optimize 2021–2024 → test 2025 (**done**, see below); optimize
   2022–2025 → test 2026.
2. Monte Carlo on the out-of-sample trade list (Kevin's free spreadsheet).
3. Look at a stop loss: the short side has had no losing trade yet, which is when a stop
   matters most.
4. Incubation on a demo account before real money.

## Walk-forward (4 years in-sample → next 1 year out-of-sample)

Each window is re-optimized on its own 4 years, then the chosen setting runs unchanged on
the following year. Files in `walk-forward/`.

| Window | Optimized on | Chosen setting | Tested on | Test result |
|---|---|---|---|---|
| 1 | 2020–2023 | MA 20, 2.5 × ATR(7), Max Bars 30 | 2024 | +84.65 (21 trades) |
| 2 | 2021–2024 | MA 20, 2.5 × ATR(7), **Max Bars 0** | 2025 | **+205.14 (15 trades)** |
| 3 | 2022–2025 | *to do* | 2026 | |

Window 1's 2024 figure is the 2024 year of the out-of-sample run above (same parameters).

### Window 2: optimize 2021–2024, test 2025

Optimization: 765 runs over 1 Jan 2021 – 31 Dec 2024, optimizing MA Length, Stretch Mode,
ATR Multiple, ATR Period, Stretch Scale, Max Bars and Position Mode. The chosen setting
appears 12 times (runs 205, 511, 538, …) with identical results, because the other values
that varied, such as Stretch Scale, do nothing in AtrMultiple mode.

Chosen setting: same as window 1 except **Max Bars 0** (no time exit). Position Mode
Multiple, Max Concurrent 5, stop loss off, Exit On MA Cross on.

| Metric | In-sample 2021–2024 | Test 2025 |
|---|---|---|
| Net profit | +486.60 | +205.14 |
| Net profit per year | +121.65 | **+205.14 (169% of in-sample)** |
| Profit factor | 3.70 | no losing trades |
| Trades | 83 (20.8 / year) | 15 |
| Win rate | 71% (59/83) | **100% (15/15)** — 3 long, 12 short |
| Average trade | +5.86 | +13.68 |
| Max equity drawdown | 1.40% | 0.69% (open trades) |
| Max consecutive losses | 6 | 0 |
| Swaps | −237.68 | −8.38 |

2025 trades closed by month: Jan 1, Mar 5, May 2, Jul 5, Aug 2 — none after August.

**Read:**

- **The chosen setting barely moved between windows** (only Max Bars changed). Stable
  parameters across walk-forward windows are a good sign.
- **Max Bars made no difference in 2025:** window 1's setting (Max Bars 30) also made
  +205.44 on 15 trades in 2025, so no 2025 trade lasted 30 bars.
- **15 wins out of 15 is not repeatable.** With no stop loss, a run of small wins can
  hide one big loss. It is still a tiny sample, and the strategy was idle from September.
- **Many settings were optimized at once** (7, 765 runs). Kevin warns that every extra
  optimized setting adds room for overfitting. Window 3 is a cleaner test with only MA
  Length and ATR Multiple optimized, and the rest fixed.
