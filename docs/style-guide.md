# ARNAV — code & writing style guide

Conventions so the code and docs read consistently across both halves of the
project — the Unity client and the Python server. Keep it short.

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

## Project files

- Keep source and configuration in the project; keep generated output (Unity's
  `Library/`, `Temp/`, build artefacts, Python virtual environments and caches)
  out of it. Those are rebuilt automatically and differ per machine.
- Do not put credentials, tokens, or personal server addresses in source or
  documentation. Those are environment-specific and belong only on the machine
  running the project.

## Why this exists

Consistent style keeps the codebase coherent and keeps the two halves of the
project readable to anyone picking it up.
