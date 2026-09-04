# Development Guidelines

## Purpose

Build a small, complete, explainable game through reviewable changes. Protect the playable core, project reproducibility, and delivery evidence throughout development.

## Sources of Truth

Use this order when instructions conflict:

1. The approved `Docs/GDD.md` for game behavior, scope, platform, and presentation decisions.
2. Verified assignment and delivery requirements recorded in the project documentation.
3. These development guidelines.
4. An explicit team decision recorded in the GDD changelog.

Do not invent a missing design decision. Record the question and request a decision before making a costly dependent change.

## Before Changing the Project

- State the player-visible outcome and acceptance criteria.
- Inspect the relevant scene, prefab, scripts, serialized fields, and current Console state.
- Identify the smallest change that can prove the behavior.
- Confirm that the change is inside the current MVP or approved polish scope.
- Commit or otherwise preserve the current working state before using a tool that edits the live Unity project.

## Implementation Rules

- Keep each class and component focused on one responsibility.
- Prefer explicit serialized references and clear ownership over hidden searches or unnecessary global state.
- Keep gameplay rules independent from UI and presentation where practical.
- Make tuning values intentional and editable without changing code.
- Preserve the accepted Unity version and package set unless a reviewed decision changes them.
- Save every edited scene and prefab before reviewing or committing.
- Treat Play Mode changes as temporary until they are deliberately applied and saved.
- Keep matching Unity `.meta` files with their assets.
- Place imported third-party material in a dedicated folder and record its source and license.
- Never add credentials, signing files, machine-local paths, generated builds, caches, or downloaded development tools to source control.

## Verification Rules

- Resolve compile errors and investigate new Console warnings.
- Play the affected behavior instead of assuming that compilation proves it works.
- Test the happy path, failure path, restart, and repeated-session behavior when relevant.
- Try to escape intended boundaries and exercise invalid or rapid input.
- Test every supported aspect ratio, resolution, input method, and target device affected by the change.
- Review the complete Git diff, including serialized files and `.meta` changes.
- Never describe a feature as verified without recording what was actually run or observed.

## Git Workflow

- Use a short-lived branch for each coherent feature or fix.
- Commit small working steps with clear, specific messages.
- Stage only files that belong to the change.
- Keep the history chronological and understandable without chat transcripts.
- Do not combine generated project setup, unrelated cleanup, and gameplay behavior in one commit.
- Keep the shared branch playable and integrate completed work frequently.

## Assisted Development

- Read the current GDD before proposing or implementing behavior.
- Work on one bounded outcome at a time.
- Explain changed responsibilities, serialized dependencies, failure modes, and verification steps.
- Do not create requirements, APIs, packages, assets, or test results that were not verified.
- Treat game feel, readability, and usability as human playtest decisions.
- When connected to the Unity Editor, approve only an expected client, avoid editing during compilation or Play Mode, and review every resulting diff.
- Keep agent configuration, generated tool directories, transcripts, and integration state outside the submitted repository.

## Mobile Checks When Applicable

- Make touch targets large, visible, and reachable without covering important information.
- Use anchors, `Scale With Screen Size`, and safe-area handling.
- Verify orientation and representative aspect ratios before polishing layout.
- Test on a real device before release.
- Confirm the correct scene list, package identity, architecture, signing setup, quality level, and target frame rate.

## Definition of Done

A change is complete only when:

- Its acceptance criteria pass in the relevant environment.
- The project remains playable and free of new unexplained errors.
- Scenes, prefabs, assets, and serialized references are saved and reproducible.
- The diff is reviewed and contains no unrelated or local-only files.
- The implementation can be explained without relying on the assistant conversation.
- Any design change is recorded in the GDD changelog.
