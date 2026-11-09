# AI Usage

I used OpenAI Codex during planning and implementation, and briefly used Google Antigravity for a second pass.

AI was useful for:

- turning the assignment PDF into a checklist
- discussing concurrency and hold-expiration edge cases
- drafting routine project setup and test scaffolding
- reviewing diffs, configuration, and documentation for omissions

I made the final technical decisions and reviewed every accepted change against the assignment. Generated code was not kept simply because it compiled.

## Corrections made

The Antigravity pass was started without the full roadmap and added extra tests, comments, and documentation changes. I compared those changes with the agreed scope and removed them before the backend foundation was committed.

Suggestions about seat allocation and expiration are still only design notes. They will be accepted only after they are implemented against PostgreSQL and proven with integration tests.

## Verification

For accepted changes, I inspect the diff, run the formatter, build with warnings treated as errors, and run the relevant automated and manual checks. The initial smoke tests passed before a Windows enterprise policy began blocking locally built DLLs; that machine restriction is being tracked separately and will not be treated as successful final test evidence.
