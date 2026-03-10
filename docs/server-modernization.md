# Server Modernization

## Assessment

The legacy server is not a candidate for an in-place upgrade. It is a VB6 application with:

- Custom TCP framing and packet parsing in `Server/ReadClientData.bas`
- A large mutable global state model in `Server/modTypes.bas`
- DAO/Jet persistence in `Server/modDataBase.bas`
- A native script runtime boundary through `Server/script.dll` in `Server/modScript.bas`

That combination makes direct conversion high-risk and hard to validate. The safer path is a compatibility-first rewrite that reproduces protocol behavior incrementally.

## Rewrite strategy

1. Build a new server alongside the legacy one.
2. Start with the externally observable protocol boundary.
3. Keep compatibility with the existing launcher/client where possible.
4. Reintroduce systems in slices: login/session, static data loading, character persistence, movement/combat, then scripting.
5. Replace the legacy database with an explicit schema only after the old record layouts are understood and migration tooling exists.

## First implemented slice

The new project lives in `src/Seyerdin.ServerModernized`.

Current scope:

- Loads basic server settings from `Server/Server.ini`
- Listens on the legacy port
- Implements legacy packet framing and checksum validation
- Responds to packet `5` (`Registry Ping`) with the same raw response shape used by the existing launcher:
  - byte 0: online user count
  - bytes 1-4: client version as a big-endian 32-bit integer
- Tracks the legacy pre-login session state and handles:
  - packet `61` (`Version`)
  - packet `0` (`New Account`)
  - packet `1` (`Log on`)
  - packet `92` (`UID`)
  - packet `93` (`iniUID`)
  - packet `255` (`eek`)
- Persists modernized test accounts in `data/modernized/accounts.json` instead of the legacy `server.dat`
- Returns legacy packet `3` (`Character Data`) in the same shape the client expects, with support for the empty-character case used by newly created accounts
- Handles packet `2` (`Create New Character`) in the connected state and populates starter stats from `Server/classes.ini`
- Handles packet `3` (`Change Password`) and packet `4` (`Delete Account`) against the JSON-backed account store
- Supports developer overrides for `--port`, `--accounts`, and `--motd` so the new server can be run beside the legacy one during migration

This corresponds to the legacy behavior in `Server/ReadClientData.bas` under `Case 5 'Registry Ping'`.

## Recommended next steps

1. Add protocol fixtures for the login/version handshake packets (`61`, `1`, `0`, `29`, `92`, `93`).
2. Extract a written packet catalog from `ReadClientData.bas` before implementing more handlers.
3. Decide whether to implement a compatibility `Play` flow next or bypass it in favor of a cleaner post-login state machine.
4. Define modern domain models for inventory, guild, and map state instead of copying VB6 globals directly.
5. Introduce a one-way importer from `server.dat`.
6. Treat scripting as an adapter boundary; do not design the new server around `script.dll`.

## Target shape

Suggested architecture:

- `Protocol`: frame codec, packet contracts, session state machine
- `Application`: use cases like login, character creation, movement, chat
- `Domain`: player, guild, map, combat, item rules
- `Infrastructure`: storage, config, script adapters, logging

The current scaffold covers only the first `Protocol` and `Hosting` pieces.
