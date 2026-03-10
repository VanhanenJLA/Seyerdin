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

This corresponds to the legacy behavior in `Server/ReadClientData.bas` under `Case 5 'Registry Ping'`.

## Recommended next steps

1. Add protocol fixtures for the login/version handshake packets (`61`, `1`, `0`, `29`, `92`, `93`).
2. Extract a written packet catalog from `ReadClientData.bas` before implementing more handlers.
3. Define modern domain models for account, character, inventory, guild, and map state instead of copying VB6 globals directly.
4. Introduce a persistence abstraction and a one-way importer from `server.dat`.
5. Treat scripting as an adapter boundary; do not design the new server around `script.dll`.

## Target shape

Suggested architecture:

- `Protocol`: frame codec, packet contracts, session state machine
- `Application`: use cases like login, character creation, movement, chat
- `Domain`: player, guild, map, combat, item rules
- `Infrastructure`: storage, config, script adapters, logging

The current scaffold covers only the first `Protocol` and `Hosting` pieces.
