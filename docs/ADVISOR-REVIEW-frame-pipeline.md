# ARNAV — Advisor Review Request: Frame Upload / Depth Pipeline (Task 2)

Read `AI-ADVISOR-BRIEF.md` first (project context + the REQUIRED Flaw→Fix / no-complexity
response format). This is a focused review of what we just built. Respond per that format.

## What changed since the brief
- **Task 1 (obstacle warning): DONE.** `NavHUD` shows a pulsing red border vignette + "OBSTACLE
  AHEAD" whenever `/nav` returns `obstacle: true`. Verified on device.
- **Task 2 (POST /frame): just built, testing now.** New `FrameUploader.cs`:
  - Every **2.5 s**, grabs the AR camera **CPU image** via `ARCameraManager.TryAcquireLatestCpuImage`.
  - Converts via `XRCpuImage.ConversionParams` (RGBA32, `MirrorY`), **downscaled by /2**.
  - `EncodeToJPG(quality 50)`, then POSTs as **multipart/form-data, field name `file`** to
    `NavClient.baseUrl + "/frame"`.
  - Server returns `{"depth": value}`, stores it; the depth then flows back to Unity through the
    normal `/nav` poll. Unity does NOT consume the `/frame` response (just logs it).
  - Acquire + convert + encode run on the **main thread**; the POST is a coroutine
    (`StartCoroutine(Post(jpg))`) fired from a `WaitForSeconds(2.5)` loop.
- **Task 3 (POST /target UI): not started.**

## Current network load on ONE flaky ngrok tunnel
- `POST /sensors` ≈ 1 Hz
- `GET /nav` @ 2 Hz
- `POST /frame` (JPG) every 2.5 s  ← new this task

## Pressure-test these (find flaws; Flaw → Fix → Complexity cost → Why it syncs)
1. **Obstacle-warning latency chain.** Frame every 2.5 s → upload → server depth → picked up on the
   *next* `/nav` poll. Worst case is several seconds end-to-end. For a *walking* "obstacle ahead"
   warning, is that too slow to be useful — and if so, what's the *smallest* change that helps, vs.
   just accepting it for the demo?
2. **Main-thread cost.** `Convert` + `EncodeToJPG` every 2.5 s on the main thread — real frame-hitch
   risk on a mid-range phone, or negligible at /2 downscale + quality 50? Smallest mitigation if it matters.
3. **Overlapping uploads / contention.** The loop fires a POST coroutine every 2.5 s regardless of
   whether the previous one finished. Over a slow/dropping tunnel, can uploads pile up, and is there
   a trivial guard? Does adding JPGs on top of sensors+nav make the existing ngrok drops worse?
4. **Image format/orientation.** `MirrorY` + a portrait-held phone — likely correct for a server-side
   depth model, or a likely source of garbage depth? (We can't see Sami's model.)
5. Anything else here we'd regret at demo time.

Reminder: "do nothing / accept for the demo" is a valid answer. Reject anything that adds a new
system, dependency, or thread unless it's truly the only fix — and then say so plainly.
