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
| 3 | 2022–2025 | MA 20, 2.5 × ATR(7), Max Bars 0 | 2026 (to 16 Oct) | **−51.58 (13 trades)** |
| **Total** | | | **2024 – 16 Oct 2026** | **+238.21 (49 trades), 2 of 3 years profitable** |

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

### Window 3: optimize 2022–2025, test 2026

Optimization: 204 runs over 1 Jan 2022 – 31 Dec 2025, optimizing MA Length (5–20), ATR
Multiple (1.5–2.5) and ATR Period (1–19). Fixed: AtrMultiple mode, Position Mode
Multiple, Max Concurrent 5, Max Bars 0, stop loss off. All runs listed in
`walk-forward/window3_opt-2022-2025_test-2026/optimization_2022-2025_all-runs.txt`.

Chosen setting (run 158): **MA 20, 2.5 × ATR(7), Max Bars 0** — the same as window 2, and
the same entry as window 1.

| Metric | In-sample 2022–2025 | Test 2026 (1 Jan – 16 Oct) |
|---|---|---|
| Net profit | +712.14 | **−51.58** |
| Net profit per year | +178.04 | −51.58 (in 9.5 months) |
| Profit factor | **9.23** | **0.53** |
| Trades | 75 (18.8 / year) | 13 |
| Win rate | 83% (62/75) | 46% (6/13) |
| Average trade | +9.50 | −3.97 |
| Largest losing trade | −14.34 | **−30.61** |
| Max consecutive losses | 5 | 5 |
| Max equity drawdown | 1.19% (120.56) | **1.66% (166.52)** |
| Swaps | −149.36 | −46.85 |

2026 long vs short: longs **−98.94** (9 trades, 2 won, profit factor 0.09, swaps −46.36);
shorts +47.36 (4 trades, all won). Every 2026 loss was a long.
Trades closed by month: Feb 1, Mar 2, Jul 2, Aug 3, Oct 5.

Neighbours in the 2022–2025 optimization (net / profit factor / trades):

| | ATR × 1.5 | ATR × 2.0 | ATR × 2.5 |
|---|---|---|---|
| MA 5 | 15.72 / 1.26 / 20 | 10.01 / 2.35 / 3 | 0 trades |
| MA 10 | 427.64 / 2.57 / 121 | 281.09 / 9.73 / 41 | 30.94 / 4.87 / 7 |
| MA 15 | 229.19 / 1.28 / 178 | — | 360.79 / 8.41 / 39 |
| MA 20 | 88.87 / 1.09 / 183 | 577.65 / 2.47 / 127 | **712.14 / 9.23 / 75** |

(ATR Period 7. MA 15 × 2.0 at period 7 was not in the run list.) At MA 20 × 2.5, every
ATR Period from 4 to 19 made +552 to +712 with profit factor 5–12: a broad plateau on ATR
Period.

**Read:**

- **Profit factor 9.23 in-sample is a warning, not a strength.** It is far above the
  2.79 and 3.70 of the earlier windows, because 2022–2025 contains the all-winning 2025.
  Results that good usually don't survive, and 2026 didn't.
- **The setting is at the top edge of both optimized ranges** (MA 20 of 5–20, ATR 2.5 of
  1.5–2.5), so the true best area may lie beyond what was tested.
- **The 2026 loss is entirely longs.** Longs have been weak out-of-sample in every
  window, and swaps make them worse.
- The 2026 drawdown (1.66%) is the worst of the whole out-of-sample period, but small, and
  the largest loss (−30.61) is about twice the in-sample largest.

## Walk-forward summary

| Test year | Window | Net | Trades |
|---|---|---|---|
| 2024 | 1 | +84.65 | 21 |
| 2025 | 2 | +205.14 | 15 |
| 2026 (to 16 Oct) | 3 | −51.58 | 13 |
| **Total** | | **+238.21** | **49** |

- Out-of-sample profit per year ≈ **+85**, versus an average in-sample rate of ≈ **+139**
  per year across the three windows: about **62%** kept. A common rule of thumb looks for
  at least ~50%.
- The optimizer chose **the same setting in all three windows**. That stability is the
  strongest evidence so far that this isn't pure curve-fitting.
- But the out-of-sample record is small (49 trades), carried by shorts, and the most
  recent year lost money.

**Verdict:** a qualified pass. Promising enough for Monte Carlo and demo incubation,
not proven enough for real money.

**Careful with "fixes":** dropping longs, or adding a filter for them, would be a change
made *after* seeing the walk-forward results. Kevin calls that cheating — all 2020–2026
data has now been used for decisions, so any such version can only be validated on
**new** data (incubation), not by another backtest of these years.
