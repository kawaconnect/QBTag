# QBTag

Windows desktop tool (WinForms, .NET Framework 4.0) that reads sales orders from
QuickBooks Desktop, turns the order lines into product tags (optionally with QR
codes) and prints them through Crystal Reports. It was written for Abanaki and
registers with QuickBooks under the application name `ABANAKI`.

## How it works

1. QBTag opens a session with the company file that is open in QuickBooks
   Desktop, through the QuickBooks SDK (QBFC12).
2. You pick one order number, a range of order numbers or a date range and
   click **Export**. The matching sales order lines are copied into a local
   Access database (`usman.mdb`).
3. The selected Crystal Reports template (`Tag Report` or
   `Tag Report with QRCodes`, or one you added) is filled from that database
   and previewed or printed.

## Requirements

- Windows 7 SP1 or later. QBTag is a 32-bit (x86) program; it runs on 64-bit
  Windows.
- QuickBooks Desktop, installed on the same computer, with the company file
  open.
- .NET Framework 4.0 or later.
- QuickBooks SDK 12.0 (QBFC12).
- Crystal Reports runtime for .NET Framework 2.0 (`CRRedist2008_x86.msi`).
- Microsoft Jet 4.0 OLE DB provider (part of Windows) for the Access database.
- QODBC driver for QuickBooks: optional, only needed for QODBC connections
  (see [QuickBooks ODBC login](#quickbooks-odbc-login-optional)).

The installer carries .NET 4.0, the QuickBooks SDK, QODBC and the Crystal
Reports runtime and installs whichever are missing.

## Install

Installers and portable zips are on this project's GitLab Releases page, with a
`SHA256SUMS.txt` for both files. Releases before 3.2.16 are on the
[GitHub Releases](https://github.com/kawaconnect/QBTag/releases) page.

### Installer (recommended)

Run `QBTag-Setup-X.Y.Z.exe` as an administrator. It asks for:

1. **Install folder** (default `C:\Program Files (x86)\QBTag`).
2. **Tag Database Location**: the path of an existing `usman.mdb`, or a folder
   for a new one (**New...**). Local and UNC paths work. The default is
   `%LocalAppData%\QBTag\usman.mdb`.
3. **Prerequisites**: QODBC and the QuickBooks SDK are offered when they are
   not installed. The QODBC setup shows its own wizard.

The database path and the database itself are set up for the Windows account
that runs the installer. If you install with a different administrator account
than the one that will use QBTag, set the path again as that user
(**Tools > Configure Databases**, or `QBTag --set-db <path>`).

Uninstalling removes the program and the stored QuickBooks ODBC login, if
one was set. The tag
database, the logs and the per-user settings are left in place.

### Upgrading

Run the new installer over the old version. The database, `Product.xml` and a
stored QuickBooks ODBC login are kept. From 3.3.1 on, per-user settings (the
database path and the list of added reports) are copied from the previous
version the first time the new version starts from the same folder; an
upgrade from 3.3.0 or older also carries them over. The installer's Tag Database Location page still sets
the path for the account that runs the installer.

### Portable zip

Unzip `QBTag-vX.Y.Z-portable.zip` anywhere and run `QBTag.exe`. Nothing is
installed for you: the QuickBooks SDK, the Crystal Reports runtime and .NET 4.0
must already be on the computer. Run `QBTag --doctor` to see what is missing.

### First start

The first time QBTag connects, QuickBooks asks whether to allow the application
`ABANAKI` to access the company file. Log in to QuickBooks as the Admin user,
allow it ("Yes, always" if QBTag should work when QuickBooks is not already
open on that file), then click Retry in QBTag.

## Usage

Start QBTag with QuickBooks open on the company file. The main window title is
"Export Data".

- **Order #**, **Order #s between** or **Use dates from**: which sales orders
  to read.
- **Export**: reads the orders from QuickBooks into the tag database and opens
  the report. **Reprint** opens the report for what is already in the database.
- **Print Labels** / **Preview Labels**: print the report, show it on screen,
  or both.
- **Delete old export data**: empties the exported orders before the new
  export. There is no confirmation.
- **Report Name**: the template to print. **Manage Reports** adds or removes
  your own `.rpt` files and sets the primary (preselected) one.
- **Tools > Configure Databases**: tag database path and the part types that
  are matched against order line items.
- **Tools > Configure QR Codes**: product number, link and QR image per
  product.
- **Edit Order Info**: add, change or delete exported order rows by hand.
- **Set Products**: the product list used during export.

## Command line

`QBTag.exe` with no arguments starts the window. With an argument it runs one
command, prints to the console and exits with 0 on success or 1 on failure.
`--name`, `-name` and `/name` are all accepted.

| Command | What it does |
|---|---|
| `--help`, `-h` | List the commands |
| `--version`, `-v` | Print the version |
| `--doctor` | Check program files, folders, database, QuickBooks connection, settings and the last logged error, and show the optional QuickBooks ODBC login. Start here when something is wrong |
| `--connect` | Test the QuickBooks SDK connection |
| `--disconnect` | Close the QuickBooks SDK connection |
| `--status` | Show install, log and database paths and which program files are present |
| `--config` | Show the QuickBooks data source, database path and the optional QuickBooks ODBC login (not set, or the user and where the password comes from) |
| `--init-db [path]` | Create the Access database (default `%LocalAppData%\QBTag\usman.mdb`); does nothing if it exists |
| `--test-db` | Open the database and list its tables with row counts |
| `--set-db <path>` | Set the database path for the current Windows user |
| `--list-orders` | List the orders in the database |
| `--list-parts` | List the configured part types |
| `--add-report <path>` | Add a Crystal Reports template (`.rpt`) to the report list |
| `--list-reports` | Show the report templates and whether their files exist |
| `--reset-config` | Reset the current user's settings to defaults (asks first) |
| `--logs` | Show the end of the newest log file |
| `--logs clear` | Delete all log files |

## Configuration

### QuickBooks ODBC login (optional)

QBTag does not need a QuickBooks password. The order screens read sales orders
through the QuickBooks SDK session, which QuickBooks itself authorises (see
[First start](#first-start)). The login described here is only used for QODBC
connections, with the connection string
`dsn=QuickBooks Data;uid=Admin;pwd=${QB_PASSWORD};` by default: QBTag fills in
the user and the `${QB_PASSWORD}` placeholder when it builds that connection.

To set it, open **Tools > Configure Databases**. The **QuickBooks ODBC login
(optional)** section shows whether a login is set; **Set...** asks for the user
and password (or "This user has no password"), **Clear** removes them. Both
take effect at once.

QBTag looks for the login in this order:

1. The `QB_USER` and `QB_PASSWORD` environment variables, if set. For scripts
   and servers, for example `setx QB_PASSWORD "..."` for the account that runs
   QBTag.
2. `C:\ProgramData\QBTag\qb-user.txt` (plain text, the user name) and
   `C:\ProgramData\QBTag\qb-password.dat` (the password, encrypted with
   Windows DPAPI for this computer, so it only works on the machine that
   wrote it). This is what **Set...** writes.
3. The older `QuickBooksPassword` setting (empty by default), and the `uid=`
   in the connection string (`Admin`).

`QBTag --config` and `QBTag --doctor` show the login as `Not set`, or the user
and where the password comes from. A missing login is not reported as a
problem.

Who can write the stored login: the installer creates `C:\ProgramData\QBTag`
so that every Windows user of the computer can change it. With the portable
zip the folder is created by whoever stores the login first, and other
non-administrator users can then read it but not replace it; save it once as
an administrator in that case.

### Tag database

An Access (Jet 4.0) file, `usman.mdb`. QBTag creates it on first start at
`%LocalAppData%\QBTag\usman.mdb` if no other path is set. To share one
database between computers, put it on a network share and point every user at
it with **Tools > Configure Databases** or `QBTag --set-db \\server\share\usman.mdb`.
The setting is stored per Windows user and takes effect at the next start.

### Reports

`tag.rpt` and `TagWithQRCodes.rpt` sit next to `QBTag.exe`. Add your own with
**Manage Reports** or `QBTag --add-report <path>`; the report stays where it
is, QBTag stores its path.

### Files and folders

| Path | Contents |
|---|---|
| `<install folder>\QBTag.exe.config` | Default settings |
| `%LocalAppData%\Abanaki\QBTag.exe_...\<version>\user.config` | Per-user settings: database path, reports. Shown by `--config`, cleared by `--reset-config`, copied from the previous version on the first start after an upgrade |
| `%LocalAppData%\QBTag\usman.mdb` | Default tag database |
| `%LocalAppData%\QBTag\Log\ErrorLog*.log` | Logs (`QBTag --logs`) |
| `C:\ProgramData\QBTag\qb-user.txt`, `qb-password.dat` | Optional QuickBooks ODBC login (only if one was set) |
| `<install folder>\Product.xml` | Product list (kept on upgrade) |

## Troubleshooting

Run `QBTag --doctor` first, then `QBTag --logs`.

| Symptom | What to do |
|---|---|
| "Cannot connect to QuickBooks" at start | Read the text under it: it names the cause. QuickBooks Desktop must be installed, running and have the company file open |
| "QBFC12 SDK is NOT installed" | Install the QuickBooks SDK 12.0 (run the QBTag installer again and tick it), then restart the computer |
| "QuickBooks denied access to QBTag" | In QuickBooks: Edit > Preferences > Integrated Applications, allow `ABANAKI` |
| "A required DLL is missing (QBXMLRP2)" | QuickBooks Desktop itself is not installed on this computer; the SDK alone is not enough |
| "No order found, the order may already be in history" | The order number does not exist as an open sales order in the company file that is open |
| "No Part Types are configured in the database" | Add part types under Tools > Configure Databases. A part type must match the beginning of the QuickBooks line item group names |
| "Report file not found" | The `.rpt` was moved or deleted. `QBTag --list-reports` shows the paths; add it again or reinstall |
| Report window fails to load | The Crystal Reports runtime is missing: run the installer again, or install `CRRedist2008_x86.msi` |
| Database errors, or `--doctor` shows "Database opens FAIL" | Check the path with `QBTag --config`, that the file is reachable and writable, then `QBTag --test-db`. `QBTag --init-db <path>` creates a new one |
| The installer's database path is not used | It was saved for the administrator account that ran the installer. Set it again as the normal user (`QBTag --set-db <path>`) |
| "The login could not be saved" (Configure Databases) | The current Windows user cannot write to `C:\ProgramData\QBTag`. Start QBTag once as an administrator and set it there, or use `QB_USER` and `QB_PASSWORD` |
| A command prints nothing in a redirected or scheduled run | QBTag is a windowed program that attaches to the console it was started from. Check the exit code, or run it from an open command prompt |

## Security notes

- The stored QuickBooks password is encrypted with DPAPI at machine scope. That
  keeps it out of backups of the file and off other computers, but any program
  or user on the same computer can decrypt it. Treat a computer with a stored
  login as trusted for that QuickBooks user, and prefer a QuickBooks user with
  only the rights QBTag needs over `Admin`.
- `QB_PASSWORD` set with `setx` is stored in plain text in that user's
  registry environment.
- QBTag makes no network connections of its own. It talks to QuickBooks on the
  same computer and to the Access database at the path you configure.
- The Access database is not encrypted and holds order numbers and item names.
  On a share, limit access with the share's permissions.
- Logs contain order numbers, item names and file paths, not the QuickBooks
  password.

## Build

Open `QBTag.sln` in Visual Studio 2022 or build from command line:

```
dotnet restore QBTag.sln
msbuild QBTag.sln -p:Configuration=Release
```

Output binaries will be in `QBTag/bin/Release/net40/`.

`dotnet build` alone cannot build the solution: two WinForms `.resx` files
contain serialized images, which only Visual Studio's (.NET Framework)
MSBuild can embed for a .NET Framework target. Use `msbuild` from Visual
Studio 2022 or the VS 2022 Build Tools.

## CI and releases (GitLab CI)

CI/CD runs on GitLab only (`.gitlab-ci.yml`); the old GitHub Actions
workflows in `.github/` no longer run. Pipelines start on `v*` tags and when
run from the GitLab web UI or API, not on ordinary pushes.

| Job | Runner | What it does |
|---|---|---|
| `test` | Linux | `ci/check-sources.ps1` (solution projects, AssemblyInfo attributes, installer sources and committed prerequisites exist); on tag builds, stamps the release version and checks that `CHANGELOG.md` has an entry for it; then compiles all C# with `dotnet build` (the two `.resx` files skipped; a compile check only, nothing is published) |
| `build-windows` | Windows laptop | Visual Studio MSBuild Release build, `QBTag.exe` version check, Inno Setup installer (each bundled prerequisite checked first), portable zip and `SHA256SUMS.txt`. On tag builds it uploads all three to the project's Generic Package Registry |
| `release` | Linux | tags / `RELEASE_VERSION` only: creates or updates the GitLab Release for the tag, with links to the uploaded files and the version's `CHANGELOG.md` entry as notes |

The installer is about 175 MB because it bundles the prerequisite
installers. That is over GitLab's default 100 MB job artifact limit, so it
is not kept as a job artifact: only tag builds keep it, in the package
registry. The job artifacts (90 days) are the portable zip and
`SHA256SUMS.txt`. Untagged runs build and check the installer but do not
keep it.

The Windows job sets up its own tools on first use under
`%ProgramData%\gitlab-runner-tools` (`ci/tools-windows.ps1`, no admin): the
.NET SDK 9.0.121, Inno Setup 6.7.3 and the .NET Framework 4.0 offline
installer, all pinned by SHA-256. It needs Visual Studio 2022 or the VS 2022
Build Tools (MSBuild 17.11 or newer, workload `ManagedDesktopBuildTools`)
already installed on the runner machine. That is a one-time admin install;
the job stops with the install command if it is missing.

To release:

1. Bump the version: add a `## [X.Y.Z] - <date>` entry to `CHANGELOG.md` and
   set `X.Y.Z.0` in the `AssemblyInfo.cs` files (`pwsh ci/set-version.ps1
   -Version X.Y.Z` does all of them).
2. Tag the commit `vX.Y.Z` (numbers only) and push the tag.

The pipeline stamps `X.Y.Z.0` into every `AssemblyInfo.cs` for that build,
checks that `QBTag.exe` carries it, and publishes `QBTag-Setup-X.Y.Z.exe`,
`QBTag-vX.Y.Z-portable.zip` and `SHA256SUMS.txt` to the GitLab Release for
the tag. If a tag pipeline failed before it uploaded anything (for example
the Windows runner was offline), run a pipeline on the tag from the web UI
with the variable `RELEASE_VERSION=vX.Y.Z`. Files that are already
published are never uploaded again (builds are not byte-identical, and the
release is mirrored to GitHub); release a new patch version instead.
Runs without a tag build `0.0.0-<commit>` with the committed assembly
versions and publish nothing.

The installer bundles `installer/prereqs/qodbc.exe`, `qbsdk120.exe` and
`CRRedist2008_x86.msi` from the repository; the `test` and `build-windows`
jobs fail if any of them is missing.

## Projects

| Project | Description |
|---|---|
| **QBTag** | Main WinForms application and command line |
| **QuickBooksDAL** | Data access layer (QuickBooks SDK, QODBC, Access) |
| **QuickBooksHandler** | Thin layer between the forms and the data access layer |
| **QuickBooksModel** | Data models (OrderInfo, Parts, SalesOrderLine, etc.) |
| **QBSalesOrder** | QuickBooks SDK connection and sales order queries |
| **QBHelpers** | QuickBooks SDK session, registration and settings helpers |
| **Logs** | Log files |
| **Interop.QBFC12Lib** | QuickBooks SDK interop types |

Much of the C# was recovered from compiled assemblies, which is why it reads
like decompiler output in places.
