# AI Usage Disclosure

I used Google Antigravity and OpenAI Codex as part of my development workflow for this assignment.

They helped me break down the assignment brief, keep track of the implementation plan, and think through concurrent seat requests, expired holds, and repeat requests. I also used them for small code drafts, test ideas, Docker configuration, and requirement reviews.

I reviewed the code and documentation myself, made the final technical decisions, and corrected work that did not fit the assignment. One early generated version used application-server time in part of the hold-expiry flow. I caught that during the database and concurrency review, then changed those transitions to use PostgreSQL time. I also removed early generated additions that were outside the agreed scope.

Before submitting, I verified the project with formatting and build checks, PostgreSQL integration tests in CI, a frontend production build, Docker image builds, and the GitHub Actions workflow. I remain responsible for the final code, tests, documentation, and submission.
