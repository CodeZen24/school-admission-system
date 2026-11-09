# AI Usage & Engineering Responsibility

AI was used throughout this project as a productivity aid to accelerate development, explore implementation approaches, assist with debugging, and improve testing and documentation.

I directed the process with clear requirements and technical context. AI suggestions were never accepted blindly. I regularly reviewed the generated work, identified inconsistencies and smaller mistakes, refined my instructions, and guided corrections to keep the implementation aligned with the project requirements and architecture.

## A Critical Issue I Identified

During a deeper technical review, I identified a potentially serious PostgreSQL timing issue that had been missed during the initial AI-assisted implementation.

Using `NOW()` inside a transaction can return the transaction-start timestamp, even when the transaction has been waiting for a database row lock. This could cause incorrect hold-expiration decisions and reduce a student's actual reservation time.

I investigated the issue and corrected the relevant operations to use PostgreSQL's `clock_timestamp()` after acquiring the required lock. I then verified the implementation through code review and automated integration tests.

## My Approach

I used AI to work faster, not to replace engineering judgment. Understanding the requirements, evaluating suggestions, identifying risks, making technical decisions, and taking responsibility for the final implementation remained my responsibility.