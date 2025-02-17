### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------------------------------
PER0001 | Usage    | Error    | Callers of methods requiring body or head should verify that they are not null or require body or head themselves.
PER0002 | Usage    | Warning  | Body or head requirement on a method that does not use body or head.
PER0003 | Usage    | Error    | Body/head requirement cannot override inherited state.
