# PR #11 clean reapply notes

This note records how the PR #11 conflict set must be reapplied against `node/hanna-lightweight-i386` without replacing the base branch wholesale.

## Base branch constraint

The clean branch must start from the current `node/hanna-lightweight-i386` branch. In this execution environment, `git fetch origin node/hanna-lightweight-i386 --depth=1` failed with `CONNECT tunnel failed, response 403`, so the branch could not be refreshed locally through Git. The repository remote was set to `https://github.com/EliezerCarrVen/Hanna.git` before the fetch attempt.

## Conflict areas from PR #11

The conflict-prone files are files that already exist in `node/hanna-lightweight-i386` and were also rewritten or introduced in the previous PR branch:

- `Hanna.NodeLightweight/.env.example`
- `Hanna.NodeLightweight/README.md`
- `Hanna.NodeLightweight/package.json`
- `Hanna.NodeLightweight/src/cli/commandRouter.js`
- `Hanna.NodeLightweight/src/index.js`
- `Hanna.NodeLightweight/src/services/clamAvService.js`
- `Hanna.NodeLightweight/src/services/doctorService.js`

A clean reapply must modify those files in place from the base branch version instead of replacing them with a generated subtree.

## Phase preservation checklist

- Phase 1 / core stabilization: keep singleton service instances in `CommandRouter`, keep stream-based JSONL reads in `fsSafe`, keep async process execution in `processRunner`, and keep `clamdscan` asynchronous in `ClamAvService`.
- Phase 2 / storage unification: keep Markdown/JSONL/config JSON storage mapping and keep `RemoteSyncService` HTTP/HTTPS-only without MongoDB drivers.
- Phase 3 / hardware capabilities: keep pure-CLI hardware services (`VoiceService`, `VisionService`) and the documented `xbindkeys` setup; do not add native Node modules or heavy npm dependencies.

## Prohibited while resolving

- Do not merge PR #11 automatically.
- Do not add MongoDB, mongoose, mongodb, bson, or native storage/audio/screen drivers.
- Do not overwrite the base branch versions of the conflict files without reviewing them.
- Do not delete existing functional code from `node/hanna-lightweight-i386`.
