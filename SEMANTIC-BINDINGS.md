# Trajectory semantic bindings

`Service/TrajectoryProviderSemantics.cs` is the shared binding source for REST/OpenAPI and MCP. Bindings use the published SemanticCatalogue 0.16.0 concepts and its authoritative physical-quantity and SI-unit metadata. No new vocabulary terms or package version are needed for this update.

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
| Ellipse semi-major / semi-minor axis | `physical-length-extent` | SI metres, with `semi-major-axis` / `semi-minor-axis` roles; roles are not noun concepts. |
| Metadata, name, description | `resource-metadata`, `resource-name`, `resource-description` | Managed-resource discovery and description. |
| UUID arguments, properties and collection elements | `resource-identifier` | Resource/nested-object identifiers. Known relationships carry the target noun in `resourceType`. |

The catalogue deprecates `measured-depth` in favour of the broader reviewed `along-hole-depth`; the provider publishes the reviewed replacement while retaining the familiar MD terminology in descriptions. MD origins can differ across runs and sidetracks, so a WGS84 along-hole origin is not invented when the source contract does not establish it.

`valueAliasOf` declares the equivalent survey property in the same object: MD → Abscissa, TVD → Z, RiemannianNorth → X, RiemannianEast → Y. `resourceType` is a provider metadata qualifier containing an existing catalogue noun URI; it does not turn an identifier into a resource object or a datum. The MCP `x-osdc-resource-type` qualifier supplies collection scope for discovery payloads. UUID collection annotations are attached to their items.

Bindings for inherited geometric coordinates are selected using their reflected survey/global-coordinate type. An arbitrary vector's Z is not labelled as a WGS84 TVD. Catalogue roles and references are not accepted as quantity nouns through name coincidence.

The read-only station evaluation operation binds its UUID to `resource-identifier` with `resourceType=wellbore-trajectory`, and its required numerical MD key to `along-hole-depth` in SI metres. Its complete SurveyStation response carries the existing quantity/reference bindings and aliases. It uses the stored calculation method, rejects extrapolation and unfinished calculations, and creates no persisted case. No new catalogue concepts were needed.

Both the full REST schema and merged schema/client have been regenerated through the repository's existing Swagger and ModelSharedOut workflows. Routes, serialized property names, values, persistence, safety annotations and calculations are unchanged. Publishing the new service image and refreshing discovered MCP contracts are required before a running consumer sees these descriptions. These contracts provide inputs for generic traversal/interpolation; they do not themselves implement those DrillWeaver operators.
