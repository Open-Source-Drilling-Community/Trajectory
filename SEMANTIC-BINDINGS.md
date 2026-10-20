# Trajectory semantic bindings

`Service/TrajectoryProviderSemantics.cs` is the shared binding source for REST/OpenAPI and MCP. Bindings use SemanticCatalogue 0.23.0 concepts, generic operation profiles, calculation states, and authoritative physical-quantity and SI-unit metadata.

| Values | Catalogue noun | Reference / meaning |
| --- | --- | --- |
| Survey `MD`, `Abscissa`, section `StartAbscissa` | `along-hole-depth` | SI metres along the path from the applicable trajectory/run's declared MD origin. Not TVD; a vertical datum offset is not an MD-origin conversion. |
| Survey `Z`, `TVD` | `true-vertical-depth` | SI metres positive downward relative to WGS84 in persisted Trajectory survey data; physical quantity `DepthDrilling`. |
| `X`, `RiemannianNorth` | `riemannian-north` | SI metres along a WGS84 meridian from the equator. |
| `Y`, `RiemannianEast` | `riemannian-east` | SI metres along the latitude parallel from Greenwich. |
| Survey latitude / longitude | `geodetic-latitude` / `geodetic-longitude` | WGS84 radians. |
| Canonical survey inclination / azimuth | `wellbore-inclination` / `wellbore-azimuth` | WGS84 downward normal / clockwise geodetic true north, radians. |
| `MDStep`, `InterpolationStep` | `trajectory-interpolation-interval` | SI metres of along-hole sample spacing; an interval, not a vertical position. |
| `DLS`, `BUR`, `TUR` | `wellbore-curvature`, `build-rate`, `turn-rate` | SI radians per metre. |
| Curve curvature minimum / junction / maximum | `wellbore-curvature` with `minimum-value`, `junction-value`, or `maximum-value` role | SI radians per metre over the declared steering-curve domain. Lead-in continuation is excluded. |
| Ellipse semi-major / semi-minor axis | `physical-length-extent` | SI metres, with `semi-major-axis` / `semi-minor-axis` roles; roles are not noun concepts. |
| Metadata, name, description | `resource-metadata`, `resource-name`, `resource-description` | Managed-resource discovery and description. |
| UUID arguments, properties and collection elements | `resource-identifier` | Resource/nested-object identifiers. Known relationships carry the target noun in `resourceType`. |
| Octree scan reference trajectory | `resource-identifier` with `reference-resource` role | The trajectory whose uncertainty-volume index is searched. |
| Octree candidates and global anti-collision comparisons | `resource-identifier` with `comparison-resource` role | Candidate comparison trajectories. Octree results are conservative broad-phase candidates and may contain false positives. |
| Octree scan job and global anti-collision case IDs | `resource-identifier` with their calculation resource type | Correlation identities joining submission, status, result and deletion operations without relying on property or tool names. |
| Policy evaluation state and worst classification | `anti-collision-policy-evaluation-state` / `anti-collision-safety-classification` | Closed provider values map to reviewed `Matched`, `NoMatch`, `Indeterminate`, `Normal`, `Alert` and `Alarm` semantic concepts. |

The catalogue deprecates `measured-depth` in favour of the broader reviewed `along-hole-depth`; the provider publishes the reviewed replacement while retaining the familiar MD terminology in descriptions. MD origins can differ across runs and sidetracks, so a WGS84 along-hole origin is not invented when the source contract does not establish it.

`valueAliasOf` declares the equivalent survey property in the same object: MD → Abscissa, TVD → Z, RiemannianNorth → X, RiemannianEast → Y. `resourceType` is a provider metadata qualifier containing an existing catalogue noun URI; it does not turn an identifier into a resource object or a datum. The MCP `x-osdc-resource-type` qualifier supplies collection scope for discovery payloads. UUID collection annotations are attached to their items.

Bindings for inherited geometric coordinates are selected using their reflected survey/global-coordinate type. An arbitrary vector's Z is not labelled as a WGS84 TVD. Catalogue roles and references are not accepted as quantity nouns through name coincidence.

The read-only station evaluation operation binds its UUID to `resource-identifier` with `resourceType=wellbore-trajectory`, and its required numerical MD key to `along-hole-depth` in SI metres. Its complete SurveyStation response carries the existing quantity/reference bindings and aliases. It uses the stored calculation method, rejects extrapolation and unfinished calculations, and creates no persisted case. No new catalogue concepts were needed.

Extrapolation and target-landing results bind `CurveCurvatureCharacteristics` as one server-derived structured result. The solved-section role enum maps to reviewed fixed-extension, lead-in, upstream-steering, downstream-steering, and well-path section concepts. Circular-arc curvature and its start toolface are explicitly bound alongside the existing BT and CTC controls. These declarations let a generic consumer select the steering summary without confusing it with curvature on an excluded lead-in section.

Octree candidate discovery declares a complete queued-calculation lifecycle: submission, lightweight status retrieval, result retrieval and deletion. Global anti-collision declares its corresponding calculation lifecycle. Their compatible typed roles allow a consumer to feed the candidate trajectory identifiers from the first result into the comparison-resource input of the second calculation. Supplying `RequestedPolicyAssignmentID` is optional for separation-factor calculation, but its metadata declares that it is required when the requested result includes policy evaluation, evaluation state or safety classification. The confidence-factor input publishes its provider default and admissible range.

Both the full REST schema and merged schema/client have been regenerated through the repository's existing Swagger and ModelSharedOut workflows. Routes, serialized property names, values, persistence, safety annotations and calculations are unchanged. Publishing the new service image and refreshing discovered MCP contracts are required before a running consumer sees these descriptions. These contracts provide inputs for generic traversal/interpolation; they do not themselves implement those DrillWeaver operators.
