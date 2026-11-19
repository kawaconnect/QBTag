# Changelog

All notable changes to QBTag are recorded here. Versions match the `vX.Y.Z`
git tags; releases before 3.2.16 were published on GitHub and have no
entries here.

## [3.3.1] (unreleased)

### Changed
- The QuickBooks login is no longer asked for. The order screens read
  QuickBooks through the QuickBooks SDK session, which QuickBooks itself
  authorises and which never used this login, so the 3.3.0 login setup had no
  effect on them. Removed: the installer's QuickBooks Login page (and the
  temporary file it used), the prompt on first start, the offer to re-enter
  the password after a failed order lookup, and the `--set-qb-password` and
  `--clear-qb-password` commands.
- The login is now an optional setting for QODBC connections only: Tools >
  Configure Databases has a "QuickBooks ODBC login (optional)" section to set
  or clear the user and password. It is stored as before (DPAPI encrypted in
  `C:\ProgramData\QBTag`), and the `QB_USER` and `QB_PASSWORD` environment
  variables still take precedence. A login stored by 3.3.0 keeps working.
- `--config` and `--doctor` show the ODBC login as optional (not set, or
  which user and where the password comes from) and no longer count a missing
  password as a problem.
- Uninstalling still removes the stored login from `C:\ProgramData\QBTag`.
- README: QuickBooks login, commands, files and troubleshooting updated to
  match; requirements, install, usage, command reference, troubleshooting and
  security notes added.

### Fixed
- Per-user settings (database path, added reports) are carried over when
  QBTag is upgraded. Before, each new version started from the defaults
  because .NET keeps them in a folder per version. `--reset-config` does not
  bring the old values back.
- The old `QuickBooksPassword` setting no longer defaults to a placeholder
  value ("Usman"); it is empty, and the placeholder is dropped from settings
  carried over from older versions.
- Edit Order Info: the "Are you sure" questions before updating, inserting or
  deleting an order now have a Cancel button. Before, the only button was OK,
  so the change could not be called off. The window title is "Edit Order Info".
- Configure QR Codes: a QR code or product number containing an apostrophe no
  longer makes the "already exists" check fail (the value is passed as a
  parameter). The QODBC and part type queries escape quotes in the same way.

## [3.3.0] - 2026-11-19

### Added
- QuickBooks login setup: the installer has a QuickBooks Login page (user and
  password, or "no password"); the app asks for the login on start when the
  connection needs one that is not set up, and again if QuickBooks rejects it;
  `--set-qb-password` (interactive, or `--user`, `--file`, `--none`) and
  `--clear-qb-password` on the command line. The password is stored encrypted
  for the computer (DPAPI) in `C:\ProgramData\QBTag`; `QB_USER` and
  `QB_PASSWORD` environment variables still take precedence.
- `--doctor` and `--config` show the QuickBooks user and where the password
  comes from.

### Fixed
- The `${QB_PASSWORD}` placeholder in the QuickBooks connection string is now
  filled in. Before, the order screens sent it to QODBC as literal text.

## [3.2.18] - 2026-10-28

### Fixed
- Products screen: saving, checking or deleting a product whose name contains
  an apostrophe no longer fails, and a product name can no longer change
  which rows a delete or update touches. The product queries now pass the
  name as a parameter instead of pasting it into the SQL.

## [3.2.17] - 2026-09-25

### Changed
- CI: pipelines run on merge requests (tests/Linux builds; Windows/macOS
  manual; nothing published).

## [3.2.16] - 2026-09-24

### Added
- A `test` stage runs on every GitLab pipeline before anything is built: in
  the `mcr.microsoft.com/dotnet/sdk:9.0` image it runs `ci/check-sources.ps1`,
  which checks the project and installer sources (the Windows build and
  installer jobs only start after it passes).

### Changed
- Builds and releases run on GitLab CI (`.gitlab-ci.yml`); the GitHub
  Actions workflows no longer run. Tag pipelines build with Visual Studio's
  MSBuild on the Windows runner, check `QBTag.exe --version`, build the Inno
  Setup installer and portable zip, and publish them with a `SHA256SUMS.txt`
  on this project's GitLab Releases page.
- The setup (about 175 MB with its bundled prerequisites) is uploaded to the
  GitLab package registry instead of being kept as a job artifact, which is
  too large for GitLab's default limit. The build checks each bundled
  prerequisite is present before building the installer, and release notes
  come from this changelog.
- Helper scripts in `ci/` install the pinned build tools on the Windows
  runner, stamp the release version, check the sources and publish the files.
- The README explains how to build (Visual Studio's MSBuild only), what each
  pipeline job does, what the Windows runner needs and how to cut a release.
- The committed assembly versions are now 3.2.16.0 (they had stayed at
  3.0.3.0; release builds were already stamped from the tag).

No application changes.
