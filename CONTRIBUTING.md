# Contributing to Ada
Ada is an open source project and contributions are more than welcome, so thank you for your interest.

---

### Checklist before creating a Pull Request
Submit only relevant commits. We don't mind many commits in a pull request, but they must be relevant as explained below.

- __Use a feature branch__ The pull request should be created from a feature branch, and not from the develop branch
- __No one-commit-to-rule-them-all__ Large commits that changes too many things at the same time are very hard to review. Split large commits into smaller. See this [StackOverflow question](http://stackoverflow.com/questions/6217156/break-a-previous-commit-into-multiple-commits) for information on how to do this.
- __Tests__ Add relevant tests and make sure the existing ones still pass. Run them with `dotnet test`.
- __No Warnings__ The build sets `TreatWarningsAsErrors`, so any warning fails the build. Package vulnerability advisories (`NU1901`-`NU1904`) are errors too.

#### Title and Description for the Pull Request ####
Give the PR a descriptive title and in the description field describe what you have done in general terms and why. This will help the reviewers greatly, and provide a history for the future.

Especially if you modify something existing, be very clear! Have you changed any algorithms, or did you just intend to reorder the code? Justify why the changes are needed.


---

## Code guidelines

Formatting and analyzer conventions are enforced by the repository's `.editorconfig`. Run `dotnet format` before opening a pull request.
