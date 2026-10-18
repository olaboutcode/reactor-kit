# ReactorKit

MauiReactor controls live in `src/ReactorKit` (namespace `Reactor.Kit`, package `Reactor.Kit`). Samples live in `samples/KitTests`. Specs live in `docs/`.

Follow the Flow section when adding a component. Follow the Improvement section when changing a component that already exists. Copy the spec from `docs/sheet.md` and `docs/popup-menu.md`. Copy the code from `GlazeView`, `PopupView`, and `SheetView`.

## Flow

Do these in order. Stop after the design doc.

1. Open an issue on [olaboutcode/reactor-kit](https://github.com/olaboutcode/reactor-kit). The title names the component. Label it `New Component`. `gh` may be logged out; the git credential for github.com still works. Do not print tokens.
2. Fetch `origin`. Branch from `origin/main`, not from the branch you are on. Name it `{issue-number}-{slug}`, for example `13-add-pagesheet-and-formsheet`.
3. Check out that branch.
4. Write the design doc at `docs/<name>.md`.
5. Stop. Show the doc and wait for approval.

Do not add the component, the factory, the test page, or a README section before approval. Leave the Done when boxes unchecked. Do not open a pull request, and do not write Fixes or Closes, until asked. Do not bump `Version` or tag.

After approval, update `README.md`, the main doc, with the new component. The steps are in After approval.

## Improvement

Follow this when the component already exists. Do not add a second public type for the change.

Do these in order. Stop after the design doc.

1. Open an issue on [olaboutcode/reactor-kit](https://github.com/olaboutcode/reactor-kit). The title names the component and the improvement. The body is the plan: what changes, what stays, and what is out of scope. Label it `enhancement`. Do not label it `New Component`. `gh` may be logged out; the git credential for github.com still works. Do not print tokens.
2. Fetch `origin`. Branch from `origin/main`, not from the branch you are on. Name it `{issue-number}-{slug}`, for example `17-add-a-paged-view-to-richtextview`.
3. Check out that branch.
4. Write the design doc. Extend `docs/<name>.md` when that spec exists. Create it when it does not. Match `docs/sheet.md`: straight apostrophes, present tense, the behavior, the defaults, and the numbers.
5. Stop. Show the doc and wait for approval.

The issue is the plan. This doc is the spec. Do not change the control before approval. Leave the Done when boxes unchecked. Do not open a pull request, and do not write Fixes or Closes, until asked. Do not bump `Version` or tag.

After approval, implement the spec on that branch. Update `README.md`, the main doc, and `docs/<name>.md` so both match the change. Show the result and wait. When asked for a tweak, make that change, update `README.md` and the spec, and wait again. Do not commit during that wait.

When the implementation is approved, commit and push that branch. Do not push `main`. The message has an imperative subject, a blank line, and paragraphs. Say what was built and what was not run. Do not use Fixes or Closes. Do not merge unless asked.

## Design doc

One public component. Write the behavior, the defaults, and the numbers. Match `docs/sheet.md`: straight apostrophes, present tense, a pieces table, and one call sample.

- What it is, and which platform guide it follows.
- The native type it does not host.
- Pieces: the public node, and any factory that only sets defaults.
- Public API, defaults, and the state the page owns.
- Files, using the layout below.
- Sample: the KitTests page and what it shows.
- Out of scope.
- Done when: unchecked boxes a person can verify.

The issue is the goal. This doc is the spec.

## After approval

This section is for a new component. An improvement stays on the existing control and follows the Improvement section through commit.

Three pieces, then `README.md`.

1. One public component at `src/ReactorKit/<Name>View.cs`. One type in that file. The constructor is `internal`. Callers do not use `new`.
2. One static factory on `Reactor.Kit.Extensions` in `src/ReactorKit/Extensions.Components.cs`, next to `GlazeView`. The method name is what pages write: `GlazeView(...)`, `PopupView(...)`, `PageSheet(...)`.
3. One test page at `samples/KitTests/Components/<Name>TestPage.cs`. `HomePage` opens it with `PushModalAsync(animated: false)`. The page closes with `PopModalAsync(animated: false)` through `NavigationBar`.

4. Update `README.md`, the main doc. The section is one sentence, one example, and the supported platforms. Behavior and math stay in `docs/<name>.md`. Update that spec when the built control differs from it. A tweak that changes behavior updates `README.md` and the spec in the same change.

## Structure

A control the page lays out is a `View<T>`, like `GlazeView`, `RichTextView`, `MarkdownView`, `Object3DView`, and `PopupView`. `OnMount` creates the native element. Each setting has a value method and a `Func<T>` method that takes `IComponentWithState? state = null`.

A control that only composes other nodes is a `Component` with `[Prop]`, like `SheetView` and `PopupMenu`. The generator writes the fluent methods. `Render` returns the host. Do not hand-write those fluents.

Style variants are factories, not extra public types. `PageSheet` and `FormSheet` both return `SheetView`.

The page owns open state and selection. The control raises `OnDismiss` and the other callbacks. It does not call `SetState`.

| Piece | Path | Namespace |
|---|---|---|
| Public node | `src/ReactorKit/<Name>View.cs` | `Reactor.Kit` |
| Factory | `src/ReactorKit/Extensions.Components.cs` | `Reactor.Kit` |
| Tokens callers name | `src/ReactorKit/<Name>Tokens/` | `Reactor.Kit.<Name>Tokens` |
| Presenter and math | `src/ReactorKit/Internals/<Name>/` | `Reactor.Kit.Internals.<Name>` |
| iOS and Mac Catalyst | `src/ReactorKit/Platforms/iOS/<Name>/` | platform |
| Android | `src/ReactorKit/Platforms/Android/<Name>/` | platform |
| Windows | `src/ReactorKit/Platforms/Windows/<Name>/` | platform |
| Test page | `samples/KitTests/Components/<Name>TestPage.cs` | `KitTests.Components` |

One type per file. Internals stay internal. The iOS folder is already shared with Mac Catalyst.

Register a handler inside `UseReactorKit` only when the component needs one, as glaze and Object3D do. A presenter the view creates does not get a settings flag.

Add a `global using` for new tokens in `samples/KitTests/GlobalUsings.cs` and in the README namespaces list. KitTests already has `global using static Reactor.Kit.Extensions`.

The factory takes `params VisualNode?[] children` when the control has children, and an `Action<T?> refAction` overload when it is a `View`. The callback receives the mounted element, and null when it unmounts. Return the component so the page can chain.

Keep `samples/KitTests` on `AppShell`. Do not wrap `HomePage` in a `NavigationPage`. Do not open sample pages with `PushAsync`.

File-scoped namespaces. 4-space C#. Explicit access. A multiline body has braces. `ConfigureAwait(false)` on library awaits. Pass `StringComparison` when comparing strings. Public members get an XML summary.

## Verify

Build the library and KitTests for `net10.0` with 0 warnings. Build `net10.0-ios` and `net10.0-android` when platform code changed. Windows does not have to compile on the Mac. Check a Done when box only after that behavior has been verified. Say what you did not run.
