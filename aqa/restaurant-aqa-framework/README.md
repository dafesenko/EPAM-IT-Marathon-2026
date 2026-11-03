# Restaurant AQA framework

End-to-end tests for the Green & Tasty restaurant application: Reqnroll (Gherkin) + TUnit + Playwright for .NET, with the Screenplay pattern.

Guidance for extending the suite (writing Gherkin, steps, Screenplay, test data, working with an AI assistant) is in [CLAUDE.md](CLAUDE.md).

## Run the tests

```bash
cp .env.example .env        # then fill in your own values (see "Configuration")
dotnet build
dotnet run --no-build -- --report-trx                          # everything
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=smoke]"   # by tag
```

Everything a run produces lands in `TestResults/`: the TUnit report and TRX, the Reqnroll report in `reqnroll/`, and Playwright traces in `traces/` (open one with `npx playwright show-trace <file>.zip`).

Some scenarios are expected to fail (`@known-defect`, bugs in the application) or are skipped (`@wip`, no admin account configured). That is intentional.

## Quality gates

| Where | What runs |
|---|---|
| Git hooks (local) | `pre-commit`: `dotnet format` check and Gherkin lint on staged files · `commit-msg`: Conventional Commits · `pre-push`: build with analyzers |
| `aqa-quality.yml` | on every push and pull request: no generated files tracked, format, build (warnings are errors), Gherkin lint, test discovery |
| `aqa-tests.yml` | the scenarios themselves, nightly and on demand (Actions → AQA Tests → Run workflow) |

Enable the hooks once per clone, from the repository root:

```bash
dotnet tool restore --tool-manifest aqa/restaurant-aqa-framework/.config/dotnet-tools.json
git config core.hooksPath aqa/restaurant-aqa-framework/.husky
```

The hooks only act on commits and pushes that touch `aqa/restaurant-aqa-framework`. Gherkin lint needs Node.js.

## Configuration

There is no shared environment and no committed `.env`: copy `.env.example` to `.env` and fill it in with the data of **your own** deployment (see "Testing your own deployment"). `.env` is git-ignored; never commit it.

Settings come from `.env` or real environment variables (variables win):

| Setting | Meaning |
|---|---|
| `UI_URL` | where the frontend is served |
| `API_URL` | base URL of the backend API, including the stage (for example `…/develop`) |
| `WAITER_USERNAME`, `WAITER_PASSWORD` | a waiter account that already exists in the environment under test |
| `TESTTABLE_ID`, `TESTTABLE_WAITER_*` | a table and the waiter assigned to it (only for scenarios that need them) |
| `ADMIN_USERNAME`, `ADMIN_PASSWORD` | an admin account; without it the admin scenarios are skipped |

The full list, with defaults, is in `.env.example`.

## Testing your own deployment

### If you deployed the backend / frontend / DevOps homework

1. `cp .env.example .env` and set `UI_URL` and `API_URL` to your deployment (API URL includes the stage, for example `…/develop`).
2. Waiters cannot be created by signing up, so create a waiter (and, for the admin scenarios, an admin) in *your* environment and put its credentials in `WAITER_*` / `ADMIN_*`. Optionally add a table and its assigned waiter (`TESTTABLE_*`).
3. Run `dotnet build` and a small slice first (`--treenode-filter "/*/*/*/*[Category=smoke]"`).

### If you have not deployed your own homework

If you deployed the original application, test that: set `UI_URL` / `API_URL` as above. Otherwise point the framework at a comparable public site (Booking, Airbnb, OpenTable, …) and write all the layers for it yourself: Gherkin features, step definitions, Screenplay tasks and questions, page objects with locators, and API clients where the site has a usable API. Use only flows you are allowed to automate (no purchases, no real bookings, respect the site's terms), and set `UI_URL` to that site.

1. **Generate a skeleton with Playwright codegen** (see "Presetup instructions"): record one flow, for example search → open a result → check details. Save the generated code to a scratch file, it is only raw material.
2. **Split it across the layers**: locators and small actions go to `Ui/` page objects, the flow becomes a task in `Screenplay/Tasks/`, the read-backs become questions in `Screenplay/Questions/`, the business wording goes to `Features/` and thin glue to `StepDefinitions/`.
3. **Replace weak locators.** Codegen often emits positional or text-fragile selectors. Prefer role + accessible name, label, test id, then CSS (see [CLAUDE.md](CLAUDE.md)); check them in the Playwright inspector.
4. Tag scenarios `@ui` / `@api`, keep them independent, and run `npx gherkin-lint -c .gherkin-lintrc "Features/*.feature"`.

### CI on your own public repository

Publish your work in your own public Git repository (the link is the deliverable). Workflows are disabled in a fresh fork until you enable them. Add the same values under Settings → Secrets and variables → Actions:

- **Variables:** `AQA_UI_URL`, `AQA_API_URL`, `AQA_TESTTABLE_ID`
- **Secrets:** `AQA_WAITER_USERNAME`, `AQA_WAITER_PASSWORD`, `AQA_ADMIN_USERNAME`, `AQA_ADMIN_PASSWORD`, `AQA_TESTTABLE_WAITER_USERNAME`, `AQA_TESTTABLE_WAITER_PASSWORD`

`aqa-quality.yml` runs on every push and pull request; `aqa-tests.yml` (nightly and on demand) tests whatever those variables point at. Never put credentials in the repository, in `.env.example` or in test code.

## Presetup instructions

This framework contains PlaywriteInstaller script so you don't need to setup it manually.
For full BDD highlighting in your IDE you may need to install Reqnroll extension https://docs.reqnroll.net/latest/ide-integrations/index.html
To start the Playwright code generator open Powershell in the framework directory and enter the line:
```bash
bin/Debug/net10.0/playwright.ps1 codegen "<your UI_URL or the public site you test>"
```
Build first so `playwright.ps1` exists. The URL is optional.

## Group pre-order, level 1 (homework "Напрям 1")

Approach: the same BDD + Screenplay setup as the rest of the suite. The feature is `Features/GroupPreOrder.feature`, tagged `@api` (no browser, the level-1 checks are about what the owner gets back from the API). The UI scenarios for the same level come later.

Run only this level:

```bash
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=level-1]"
```

It needs `LOCATION_WAITER_USERNAME` and `LOCATION_WAITER_PASSWORD` in `.env`: a Waiter account that is put on each test location, since a booking is refused on a location without a waiter. A waiter serves one location at a time, so the feature is tagged `@sequential` and its scenarios do not run in parallel.

### Behaviour the homework leaves open

- **The owner is always a participant.** The owner's own session exists from the moment the table is booked, so a group that nobody has joined lists only the owner, named `Owner`, with a total of 0. The homework text expects `participants: []` for an empty group; the backend does not produce that, and the suite follows the backend.
- **A guest is asked to sign in, not refused.** A guest holds a session token rather than a customer's Cognito token, so the request is unauthenticated (401). A signed-in customer who is not the owner, and the waiter, are refused (403).
- **Totals include unconfirmed carts.** The participants list shows every guest's cart total, with `confirmed` as a separate flag. The group total is the sum of all participants, as in the example in `docs/ApiRoutes.md`.
- **A group that does not exist** answers 404, which the suite reads as "could not be found".

### The owner's page, level 1 (UI)

`Features/GroupPreOrderOwnerPage.feature` (`@ui`) signs the owner in through the login form, opens `/reservations/{id}/group-order`, and checks the participant cards: whose card it is, whether the order is confirmed, and the subtotal. The page titles the owner's card "My Pre-Order" and each guest's card "Guest: <name>". The cards are matched by title, because the page does not promise an order.

**Known defect:** the owner cannot open the page of a group whose every seat is taken. The page first reads the group's invitation (`GET /guest/invites/{token}`), and the backend refuses that read once the guest limit is reached, so the page shows an error instead of the cards. The scenario "The owner sees the cards of a group whose every seat is taken" is tagged `@known-defect` and stays red until this is fixed. The other UI scenarios book one seat more than the guests they invite, so they are not affected.

**Not covered yet:** the page shows a "Grand Total" that counts only submitted carts. The homework asks for the total over all participants at this level; that total is checked through the API scenarios, not in the UI.

## Group pre-order, level 2 (submitting the pre-order)

`Features/GroupPreOrderSubmit.feature` (`@api @level-2 @sequential`). The owner starts their own pre-order (a pre-order needs at least one dish), the guests confirm their carts, and the owner submits the group. Checked: only confirmed carts are passed on, the same dish from several guests is merged, the group becomes `Submitted`, the order becomes `SubmittedByCustomer`, and the waiter sees the owner's dishes plus the confirmed guest dishes. Refusals: no confirmed cart, no pre-order, group does not exist, not the owner, and a second submission.

Run only this level:

```bash
dotnet run --no-build -- --treenode-filter "/*/*/*/*[Category=level-2]"
```

**Known defect:** the owner cannot add a dish to their own part of the group. The owner's session has no token, and the backend answers the attempt with a 500 instead of adding the dish. Scenario "The owner adds a dish to their own group cart" is `@known-defect`.

**Not testable through the API:** the homework says the submission must be refused when the confirmed carts contain no dishes. The backend does not allow a guest to confirm an empty cart, so that state cannot be reached, and no scenario checks it.

### The owner's page, level 2 (submitting from the page)

`Features/GroupPreOrderSubmitPage.feature` (`@ui`): the owner opens the group page and presses "Submit Pre-Order", then confirms in the dialog. Checked: the badge changes to "Sent to Restaurant" and the button is gone; the button is disabled while no guest has confirmed a cart; cancelling the dialog sends nothing.

The UI scenarios book one seat more than the guests they invite, for the same reason as level 1: the owner's page cannot be opened for a group whose every seat is taken (see the known defect above).
