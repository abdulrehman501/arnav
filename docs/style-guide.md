# ARNAV — code & writing style guide

House conventions so our code and docs read consistently across both halves of the
project. Keep it short; the goal is work that reads like one team wrote it, by
hand.

## Code

- **Names say what the thing is.** `sendInterval`, `lat`, `PostSensors` — concrete
  and specific. Avoid filler names like `data`, `manager`, `helper`, `temp`,
  `result2` unless that's genuinely what it is.
- **Comment the *why*, not the *what*.** A comment should explain a decision or a
  non-obvious reason. Don't narrate code that already speaks for itself:
  ```csharp
  // good — explains a non-obvious reason
  // new Input System sensors are off by default — enable them first
  InputSystem.EnableDevice(Accelerometer.current);

  // noise — just restates the line
  // set the accelerometer variable
  var a = Accelerometer.current;
  ```
- **Comments are sparse.** A file shouldn't have a comment on every line. If a
  block needs heavy explanation, the code probably needs simplifying instead.
- **Match the surrounding code.** Indentation, brace style, and naming should look
  like what's already in the file, not a different house style dropped in.
- **No dead code or commented-out blocks** left lying around — delete it; git
  remembers.

## Docs & comments — tone

- **Write plainly and concretely.** Prefer "the phone POSTs sensors every second"
  over vague, padded phrasing.
- **Cut filler openers** — "It's important to note that", "In today's world",
  "Let's dive in". Just say the thing.
- **Avoid the over-symmetric cliché** — the "this isn't just X, it's Y" /
  "not only… but also…" pattern. State the point directly.
- **Vary sentence length.** Don't make every paragraph the same shape.
- Skip overused buzzwords (delve, leverage, seamless, robust, tapestry,
  showcase) unless they're literally the right word.

## Git

- **Commit messages:** short imperative summary line, then bullet points for
  detail. Describe what changed and why.
- **No AI co-author trailers** on commits (this is an assessed project).
- Commit source, not generated output (see `.gitignore`). Review staged files
  before committing.

## Why this exists

Consistent, hand-written-looking style keeps the codebase coherent and is part of
keeping the project's work genuinely our own.
