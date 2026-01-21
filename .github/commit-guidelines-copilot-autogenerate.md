# GIT COMMIT RULES (Summary for Copilot Auto-Generate)

1. ONLY commit files you personally modified. Never use `git add .` or `git commit -a`.
2. ALWAYS run `git status` first. If other files are staged, leave them alone and only `git add` your specific files.
3. NO HALLUCINATED INTENT: Do not invent reasons for commits. Describe WHAT changed, not WHY (unless the 'why' was in your instructions).
   - BAD: "rename X to improve clarity"
   - GOOD: "rename X to Y"
4. FORMAT: Use conventional commits: `<type>(<scope>): <factual description>`.