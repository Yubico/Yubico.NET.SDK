# Ralph Loop Workflow Overview

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           RALPH LOOP ECOSYSTEM                                   │
└─────────────────────────────────────────────────────────────────────────────────┘

                    ┌──────────────────────────────────────┐
                    │         ENTRY POINTS                 │
                    └──────────────────────────────────────┘

    ╔═══════════════════╗          ╔═══════════════════╗          ╔════════════════╗
    ║  Product Request  ║          ║  Feature Request  ║          ║  Quick Task    ║
    ║  (formal spec)    ║          ║  (implementation) ║          ║  (ad-hoc)      ║
    ╚═════════╤═════════╝          ╚═════════╤═════════╝          ╚═══════╤════════╝
              │                              │                            │
              ▼                              ▼                            │
    ┌─────────────────────┐        ┌─────────────────────┐               │
    │ product-orchestrator│        │     write-plan      │               │
    │                     │        │                     │               │
    │ Creates PRD with:   │        │ Creates plan with:  │               │
    │ • User stories      │        │ • Tasks             │               │
    │ • Acceptance criteria│       │ • File paths        │               │
    │ • Error states      │        │ • TDD steps         │               │
    │ • Security audit    │        │ • Commit points     │               │
    └──────────┬──────────┘        └──────────┬──────────┘               │
               │                              │                          │
               ▼                              ▼                          │
    ┌──────────────────────────────────────────────────────┐             │
    │              docs/specs/{feature}/final_spec.md      │             │
    │              docs/plans/YYYY-MM-DD-{feature}.md      │             │
    └──────────────────────┬───────────────────────────────┘             │
                           │                                             │
                    ┌──────┴──────┐                                      │
                    ▼             ▼                                      │
          ┌─────────────────┐  ┌─────────────────┐                       │
          │  prd-to-ralph   │  │  plan-to-ralph  │                       │
          │                 │  │                 │                       │
          │ Maps:           │  │ Maps:           │                       │
          │ • Stories→Phases│  │ • Tasks→Phases  │                       │
          │ • AC→Checkboxes │  │ • Steps→Tasks   │                       │
          │ • Errors→Tasks  │  │ • Files→Files   │                       │
          └────────┬────────┘  └────────┬────────┘                       │
                   │                    │                                │
                   └─────────┬──────────┘                                │
                             ▼                                           │
    ┌─────────────────────────────────────────────────────────────────┐  │
    │                      PROGRESS FILE                              │  │
    │              docs/ralph-loop/{feature}-progress.md              │  │
    │                                                                 │  │
    │  ┌───────────────────────────────────────────────────────────┐  │  │
    │  │ ---                                                       │  │  │
    │  │ type: progress          ◄── Triggers protocol injection   │  │  │
    │  │ feature: my-feature                                       │  │  │
    │  │ ---                                                       │  │  │
    │  │                                                           │  │  │
    │  │ ## Phase 1: Core (P0)   ◄── Priority ordering             │  │  │
    │  │ **Files:** Src, Test    ◄── Explicit paths                │  │  │
    │  │ - [ ] 1.1: Task         ◄── Checkbox tracking             │  │  │
    │  │ - [x] 1.2: Done                                           │  │  │
    │  └───────────────────────────────────────────────────────────┘  │  │
    └──────────────────────────────┬──────────────────────────────────┘  │
                                   │                                     │
                                   │         ┌───────────────────────────┘
                                   │         │
                                   ▼         ▼
    ┌─────────────────────────────────────────────────────────────────────────────┐
    │                                                                             │
    │                           ralph-loop.ts                                     │
    │                                                                             │
    │   ┌─────────────────────────────┐    ┌─────────────────────────────┐        │
    │   │     PROGRESS FILE MODE      │    │        AD-HOC MODE          │        │
    │   │                             │    │                             │        │
    │   │ Detects: type: progress     │    │ Input: plain prompt         │        │
    │   │                             │    │                             │        │
    │   │ Auto-injects:               │    │ Injects:                    │        │
    │   │ ✓ TDD Loop (RED→GREEN)      │    │ ✓ Skill awareness           │        │
    │   │ ✓ Security protocol         │    │ ✓ Autonomy directives       │        │
    │   │ ✓ Git discipline            │    │ ✓ Git exploration           │        │
    │   │ ✓ Build commands            │    │                             │        │
    │   │ ✓ Task context              │    │                             │        │
    │   │ ✓ Progress file content     │    │                             │        │
    │   │                             │    │                             │        │
    │   │ Re-reads file each iteration│    │ Same prompt each iteration  │        │
    │   └─────────────────────────────┘    └─────────────────────────────┘        │
    │                                                                             │
    └───────────────────────────────────────┬─────────────────────────────────────┘
                                            │
                                            ▼
    ┌─────────────────────────────────────────────────────────────────────────────┐
    │                            EXECUTION LOOP                                   │
    │                                                                             │
    │   ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐   │
    │   │ Iter 1  │───▶│ Iter 2  │───▶│ Iter 3  │───▶│  ...    │───▶│ Iter N  │   │
    │   └─────────┘    └─────────┘    └─────────┘    └─────────┘    └─────────┘   │
    │                                                                             │
    │   Each iteration:                                                           │
    │   1. Read progress file (if progress mode)                                  │
    │   2. Find next unchecked task                                               │
    │   3. Execute with injected protocol                                         │
    │   4. Agent marks [x] and commits                                            │
    │   5. Check for <promise>DONE</promise>                                      │
    │                                                                             │
    └───────────────────────────────────────┬─────────────────────────────────────┘
                                            │
                          ┌─────────────────┴─────────────────┐
                          ▼                                   ▼
               ┌─────────────────────┐             ┌─────────────────────┐
               │   ✅ COMPLETED      │             │   🛑 MAX ITERATIONS │
               │                     │             │                     │
               │ <promise>DONE</promise>│          │ Agent got stuck     │
               │ detected            │             │ or task too complex │
               └─────────────────────┘             └─────────────────────┘


                    ┌──────────────────────────────────────┐
                    │         SUPPORTING SKILLS            │
                    └──────────────────────────────────────┘

    ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐
    │ write-ralph-prompt│  │    build-project  │  │   test-project    │
    │                   │  │                   │  │                   │
    │ Guidance for      │  │ MANDATORY         │  │ MANDATORY         │
    │ ad-hoc prompts    │  │ dotnet toolchain.cs   │  │ dotnet toolchain.cs   │
    │ (when no progress │  │ build             │  │ test              │
    │ file needed)      │  │                   │  │                   │
    └───────────────────┘  └───────────────────┘  └───────────────────┘

    ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────┐
    │      commit       │  │       debug       │  │       verify      │
    │                   │  │                   │  │                   │
    │ Git commit        │  │ Systematic        │  │ Final checks      │
    │ discipline        │  │ debugging         │  │ before claiming   │
    │ (explicit adds)   │  │ process           │  │ done              │
    └───────────────────┘  └───────────────────┘  └───────────────────┘
```

## Quick Reference

| Want to... | Use |
|------------|-----|
| Create a PRD from requirements | `product-orchestrator` |
| Create an implementation plan | `write-plan` |
| Convert PRD to progress file | `prd-to-ralph` |
| Convert plan to progress file | `plan-to-ralph` |
| Run autonomous execution | `ralph-loop` with `--prompt-file` |
| Quick ad-hoc task | `ralph-loop` with inline prompt |
| Write ad-hoc prompts | `write-ralph-prompt` (this skill) |

## Key Principle

> **The execution protocol lives in the engine, not the prompt.**
>
> Progress files are declarative (WHAT to do). The ralph-loop.ts injects HOW to do it.
> This ensures consistent, reliable execution regardless of who wrote the progress file.
