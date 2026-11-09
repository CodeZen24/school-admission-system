# Assumptions

This file records choices the assignment does not define explicitly.

- One class record represents a class-and-shift admission unit, such as `Class 6 - Morning Shift`.
- API identifiers will be UUID strings. The IDs shown in the assignment are examples, not a required format.
- `X-Student-Id` is treated as the student's 17-digit birth-registration number. It identifies ownership but is not secure authentication.
- PostgreSQL time will decide when a hold expires.
- Seat counts will be calculated from stored hold states instead of maintained as separate counters.
- C# uses XML documentation comments where documentation is useful. TypeScript uses JSDoc/TSDoc. Inline comments are reserved for decisions or invariants that are not clear from the code.

API error behavior and state-transition decisions will be added here as those features are implemented.
