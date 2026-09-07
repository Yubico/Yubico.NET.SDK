# Editing this deck

**Edit `slides/*.md`. Run `./watch.sh`. The browser reloads itself.**

Everything else in this directory is generated. `deck.md`, `deck.html` and
`deck.pdf` are overwritten on every build — do not hand-edit them.

---

## The loop

```bash
cd docs/demo
./watch.sh
```

Then edit any file in `slides/`. Save, and `deck.html` reloads in the browser
within about a second. No clicking, no cache-busting.

`watch.sh` does not rebuild the PDF on every keystroke. When you want a fresh
`deck.pdf`:

```bash
./build.sh
```

Other entry points:

| Command | What |
|---|---|
| `./watch.sh` | live-editing loop, browser auto-reload |
| `./build.sh` | full build: `deck.md` + `deck.html` + `deck.pdf` |
| `./build.sh md` | just re-concatenate `deck.md`, no Node needed, instant |
| `./build.sh map` | print the file → slide-number map below, freshly computed |
| `./view.sh` | serve over HTTP, open the standalone diagram viewer |
| `./view.sh deck` | serve over HTTP, open the deck |

---

## Which file is which slide

Files concatenate in filename order. One file usually holds 2–3 slides,
separated by a `---` line.

This table goes stale the moment you add or delete a slide. Regenerate it with
`./build.sh map` rather than trusting it.

| File | Slides | Topic |
|---|---|---|
| `00-intro.md` | 1 | Title and the five headline claims |
| `01-pipeline.md` | 2–3 | Layered-stack diagram, then the pipeline in code |
| `02-transports.md` | 4–7 | Connection diagram, APDU sequence, CTAP sequence, transport-order table |
| `03-discovery.md` | 8–9 | Discovery diagram, discovery API |
| `04-identity-merging.md` | 10–11 | Merging, then identity guarantees |
| `05-observability-events.md` | 12–13 | Device-event API, peer comparison |
| `06-observability-architecture.md` | 14–15 | v1→v2 diagram, the three contracts |
| `08-raw-access-tiers.md` | 16 | Tier 0 / 1 / 2 |
| `09-session-model.md` | 17–18 | Session shape, connection ownership |
| `10-applets-a.md` | 19–21 | Management, PIV, OATH |
| `11-applets-b.md` | 22–24 | OpenPGP, FIDO2, WebAuthn |
| `12-applets-c.md` | 25–27 | SecurityDomain, YubiHSM Auth, YubiOTP |
| `13-coverage-matrix.md` | 28 | Coverage across all five SDKs |
| `14-footprint.md` | 29–30 | Sizes and measurements, provenance |
| `15-takeaways.md` | 31 | Closing |

There is no `07-`. Logging was folded into `06-`, and `08-` was reused for the
tier slide. Numbering is only for ordering — rename freely, order follows.

---

## Common edits

**Delete a slide.** Delete its block, including one of the surrounding `---`
lines. Blocks are separated by a line containing exactly `---`.

**Delete a whole topic.** Delete the file. Nothing else references it.

**Reorder.** Rename files. `12-applets-c.md` → `05-applets-c.md` moves it.

**Add a slide.** Add a `---` line and write below it. Or create a new file with
a number that sorts where you want it.

**Split a file.** Cut a block into a new file, e.g. `11b-webauthn.md`.

**Change the theme, font sizes, colours.** `header.md` holds the Marp front
matter and all CSS. Slide font size is `section { font-size: 21px }`; code
blocks are `pre { font-size: 0.80em }`.

---

## Slide syntax

Standard Markdown, plus a few Marp things used here:

```markdown
---                          <- slide separator (own line, exactly three dashes)

## Slide title

Body text. **bold**, `code`, tables, lists all work.

​```csharp
await using var piv = await key.CreatePivSessionAsync();
​```

![h:520](assets/L4-connection.svg)     <- image, height 520px
![w:1120](assets/L3-apdu-sequence.svg) <- image, width 1120px

<div class="cols">                     <- two-column block, used on applet slides

**ykman (Python)**
​```python
...
​```

**yubikit-android**
​```java
...
​```

</div>

<!-- _class: lead -->                  <- centred title slide

<!-- Anchors: src/Foo/Bar.cs:42 -->    <- HTML comment; invisible in the deck
```

Diagrams live in `assets/`. Any `![](assets/*.svg)` becomes click-to-zoom in
the HTML deck automatically — no extra markup.

---

## The anchor comments

Most slides end with an HTML comment listing the `file:line` that grounds each
claim. They do not render. They exist so a reviewer, or you in three months,
can check a claim without re-deriving it.

`CLAIMS.md` is the full ledger of every claim and its source. If you change a
factual statement on a slide, the honest move is to update the matching row —
but nothing breaks if you do not, and the deck builds either way.

`DEFERRED.md` lists six real bugs found in the SDK while building this, which
were deliberately **not** fixed.

---

## Files

```
slides/*.md      SOURCE OF TRUTH — edit these
header.md        Marp front matter, theme, all CSS
assets/*.svg     diagrams (six from docs/architecture, one hand-drawn)
assets/deck-zoom.js          click-to-zoom overlay
assets/vendor/panzoom.min.js vendored, no CDN dependency

deck.md          GENERATED — concatenation of the above
deck.html        GENERATED — the presentable deck
deck.pdf         GENERATED — vector, zooms losslessly
diagrams.html    standalone pan/zoom diagram browser

build.sh  watch.sh  view.sh
PLAN.md  CLAIMS.md  DEFERRED.md   provenance, safe to ignore while editing
```

---

## Presenting

Open `deck.html` and press <kbd>P</kbd> for presenter view, or use the PDF.
Click any diagram to open the zoom overlay; <kbd>Esc</kbd> closes it.
Keyboard while the overlay is open: <kbd>+</kbd> <kbd>−</kbd> zoom,
<kbd>0</kbd> fit, <kbd>1</kbd> actual size.

Serve over HTTP (`./view.sh`) rather than opening from disk — that lets the
diagrams inline as true vectors and stay sharp at any zoom.
