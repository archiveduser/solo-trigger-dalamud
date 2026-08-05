# Solo Trigger

Solo Trigger is a Dalamud plugin that monitors nearby players and runs configured commands when the selected player count crosses a threshold.

This plugin was developed by Codex.

## Installation

Add the following Dalamud plugin repository:

```text
https://github.com/archiveduser/sync/raw/refs/heads/main/dalamud-plugins.json
```

Then search for and install `Solo Trigger`.

## Features

- Create and persist multiple independent trigger profiles.
- Choose between all players, non-AFK players, and variants that treat the count as zero while the local player is within 125 horizontal yalms of a major aetheryte.
- Exclude the local player and UI preview characters from player counts.
- Run a start command when the selected count is less than or equal to the configured threshold.
- Run an end command when the selected count is greater than the configured threshold.
- Execute registered Dalamud plugin commands and native FFXIV text commands.
- Serialize commands from multiple profiles to avoid dispatching several commands in the same framework update.
- Open a dedicated monitoring window for each profile.

## Usage

Open the configuration window:

```text
/solotrigger
```

Open a profile window directly by name:

```text
/solotrigger PROFILE_NAME
```

Open the live diagnostics window:

```text
/solotriggerdebug
```

The diagnostics window displays the nearby player count, the non-AFK nearby player count, and the horizontal distance to the nearest currently loaded major aetheryte.

Each profile contains:

- A unique name
- A player-count mode, including optional major-aetheryte proximity exclusion
- A trigger count, which defaults to `0`
- An optional start command
- An optional end command

Click `Start` in a profile window to begin monitoring that profile. Runtime state is independent for every profile and is not restored after the plugin or game restarts.

To delete a profile, hold `Shift` while clicking its delete button.

## Trigger Behavior

For a threshold of `0`:

- `0` matching players runs the start command.
- `1` or more matching players runs the end command.

Commands are only queued when the condition changes, so they are not repeatedly executed on every update.

Examples:

```text
/echo No players nearby<se.1>
/echo A player is nearby<se.2>
/gatherbuddy auto on
/gatherbuddy auto off
```

Macro-only control commands such as `/wait` are not supported as command sequences. Each command field represents one command.

## Building

Solo Trigger targets Dalamud API 15 and .NET 10.

```powershell
dotnet restore --locked-mode
dotnet build --configuration Debug --no-restore
```

Local builds use the Dalamud development files from XIVLauncherCN when available. The release workflow downloads Dalamud automatically and publishes the generated plugin archive.

## Automated Releases

Pushes to `main` or `master` run the release workflow. It builds the plugin, creates or updates a GitHub release, and dispatches a `dalamud-plugin-release` event to `archiveduser/sync`, which regenerates the subscription repository.

The source repository must define the following GitHub Actions secret:

```text
SYNC_REPO_DISPATCH_TOKEN
```

The token must have permission to send repository dispatch events to `archiveduser/sync` (Contents: write).
