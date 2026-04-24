# v1.1 stall-rule A/B

## Question

Why did `v1.1-baseline` produce 10.2% unfinished over 500 games while the older pinned `v7f-d8-remove-a-no-leader` report produced 47.7% unfinished over 300 games, even though both appear to use the same board and pieces?

## Config difference at the time

During the investigation, the materialized `v1.1-baseline` INI contained no active stall-rule settings. It only had the commented examples from `variants/yeshybrid.ini`:

```ini
;   nMoveRule         = 50
;   nFoldRule         = 3
```

The materialized `v7f-d8-remove-a-no-leader` INI explicitly appends:

```ini
nMoveRule = 0
nFoldRule = 0
```

Fairy-Stockfish inherits from `chess`, where the documented defaults are `nMoveRule = 50` and `nFoldRule = 3`. So in practice:

- `v1.1-baseline` = stall/draw rules inherited from chess defaults.
- `v7f-d8-remove-a-no-leader` = stall/draw rules explicitly disabled.

## Same-seed check

Earlier low-stall verification:

- Ruleset: `v1.1-baseline`
- Games: 50
- Seed: 20260507
- Result: 22 Party wins, 24 Horde wins, 4 unfinished (8.0%)

Explicit-zero repeat with same seed:

- Ruleset: `v7f-d8-remove-a-no-leader`
- Games: 50
- Seed: 20260507
- Result: 14 Party wins, 13 Horde wins, 23 unfinished (46.0%)

## Conclusion

The discrepancy is real and is explained by stall-rule inheritance. "Removed from the INI" does not mean "disabled"; it means Fairy-Stockfish falls back to the parent `chess` defaults. To truly disable stall rules, the variant needs `nMoveRule = 0` and `nFoldRule = 0`.

The then-current canonical `variants/yeshybrid.ini` therefore did not match its comments. It was running with inherited chess stall rules unless a ruleset explicitly overrode them.

## Recommendation

Decision made: v1.1 keeps chess-style stall rules as gameplay rules.

The INI should state this explicitly with active `nMoveRule = 50` and `nFoldRule = 3`, not rely on inherited defaults. The 500-game result is the final current ship metric: 48.1% Party share of decisive games, 10.2% unfinished.
