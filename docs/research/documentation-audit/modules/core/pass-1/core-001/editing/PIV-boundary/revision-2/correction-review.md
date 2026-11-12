# PIV correction response — editor recheck

**Status: source-visible scope correction prepared; independent successor review pending.**
The original `../review.md` requested an accurate local qualification and permitted unchanged
bodies. The coordinator subsequently authorized the preferable minimal documentation-fence
correction. Both are consistent: the successor corrects the actual scope and describes its
precise limits. No SDK/test executable source or runtime policy changes.

| Reviewed anchor / concern | Current correction and evidence |
| --- | --- |
| README signing `:180–201`: hashing after PIN allocation but before try | Current `src/Piv/README.md:180–203` initializes empty digest/signature holders first; PIN acquisition is at 185, try at 186, hashing at 188. Existing finally clears PIN/digest/signature at 199–201. A later setup/hash failure enters cleanup; no allocation-failure operation was executed. |
| README decrypt `:215–237`: plaintext fixture allocated after PIN but before try | Current `:216–240` scopes session/certificate/RSA before acquiring PIN at 224, enters try at 225 and allocates expected plaintext at 227. Finally clears PIN, assigned expected plaintext and returned plaintext at 236–238. Provider/source copies are separate, not claimed cleared by this finally. |
| Inventory/path reason overstated complete cleanup | Successor `example-inventory.json` marks exactly these two entries CHANGED-DOCUMENTATION-FENCE, preserves old/current bodies and hashes, and names local owners plus shared typed prerequisites. `path-decisions.json` corrects the README reason instead of repeating the old unqualified wording. |
| PivTool directions implied complete scopes without setup boundary | Current task-guide `:247–250` explains acquisition-protected setup and independent copies. Tool README introduction uses scoped examples; closing directions preserve acquisition-to-finally scope in adaptations and account for extra copies. |

Maps to CORE-PIV-X003/X006. All other reviewed facets remain unchanged. No new API was
invented: the documentation uses the same concrete methods, types, host objects and await/
finally cleanup as the predecessor, with buffer-holder initialization and setup placement
changed only inside the Markdown fences. Compiler/API binding remains unrun and pending.

The full unchanged-body inventory remains separate from the two changed entries: nineteen
Markdown and 23 XML bodies total, forty unchanged. Every `.cs` file in the patch is byte-exact
to the reviewed predecessor. Original profile/RFC/vendor citations and historical evidence
retain the predecessor's exact authority and qualification notes. This editor check adds no
all-copy/provider erasure, hardware acceptance, independent review or runtime proof.
