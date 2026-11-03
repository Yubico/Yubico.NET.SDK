# OATH terminal-reset decision

After checking the pinned Python implementation, the user selected **Always terminal**: successful RESET ends every OATH session, plain or SCP. Caller disposes/reopens the connection and creates a new session before further operations. No new SCP reset overload or retained creation-key lifetime is introduced.

This is an intentional difference from Python, which retains SCP parameters, reselects and performs a new handshake while preserving the session/connection objects. It is not a hardware requirement. Caller-owned C# connections remain caller-owned; terminal session disposal must not silently dispose them. Convenience-owned connections follow existing ownership teardown.

The user additionally constrained the campaign: OATH is the explicit behavior-change exception. Similar reset/reopen behavior in other applications is deferred to a separate fix pass and documentation re-review. Do not use this decision to normalize reset across the SDK now.

Implementation owner: Sol6.1 `ses_e5ed0d2b6ffeDK472Vbn3dECpx` in lifecycle worktree, offline only. Scope includes necessary OATH callers/setup helpers and docs migrations; no unrelated applet behavior or shared-Core redesign. Required evidence: no post-reset SELECT/handshake, terminal flags/operation rejection, owned/borrowed cleanup, pre-admission refusal preserving healthy state, and failure precedence. Existing timestamp/auth fixes remain.
