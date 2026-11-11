# Deletion-intent review

## Explicit modify/delete resolutions

The resolver reviewed the complete campaign changes and incoming deletion history before staging these
three deletions. The before/base/campaign blobs are retained under `evidence/resolver-inputs/blobs/`;
the incoming absent entries are explicit in `overlaps.json`.

| Path | Incoming provenance | Campaign intent and disposition |
| --- | --- | --- |
| `src/Ctap2/examples/FidoTool/Cli/Prompts/FidoDeviceSelector.cs` | `f1ee781dee565fd3d7c5116a707f14960cf6e588`, shared active CLI selection | Campaign added a warning that this alternative shared-base selector was inactive. Accept deletion alongside `DeviceSelectorBase`; active `DeviceSelector.cs` retains transport/AID qualifications and uses the new shared selector. |
| `src/OpenPgp/examples/OpenPgpTool/Cli/Prompts/OpenPgpDeviceSelector.cs` | `f1ee781dee565fd3d7c5116a707f14960cf6e588`, shared active CLI selection | Campaign identified the separate static selector as the active command path. Accept deletion; that surviving adapter now delegates to the concrete shared selector. |
| `src/Tests.TestProject/Controllers/DiTestController.cs` | `e64eecdca1b867c039f2ff077a3bcbb6b094e1dc`, removal of Tests.TestProject | Campaign corrected the temporary-session/connection scope, request cancellation and legacy response-name explanation. The incoming commit removes the entire sample project and its solution entry. Accept deletion; retain the corrected historical example in Git and captured campaign blobs. Current discovery/Management examples carry the supported scoped pattern. |

These are intentional architecture/sample removals, not missing conflict content to resurrect. Every
other incoming deletion and both detected renames are listed in the complete incoming delta. Their
source history remains reachable through the pending merge's pinned incoming parent. No historical
audit source or frozen example project is deleted or modernized.
