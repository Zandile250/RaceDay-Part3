# Part 2 notes: how the API follows the Part 1 plan

The database schema follows `raceday-database.sql` and `erd.pdf` (Roles, Users, Events,
Categories, Enrolments, Results, with the same column names, types, keys and unique
constraints). The routes follow `api-endpoint-plan.md`.

## Where Part 2 differs from Part 1, and why

| Item | Part 1 plan | Part 2 implementation | Reason |
|---|---|---|---|
| Authentication | JWT bearer token | Cookie-based session (user id and role stored in the session) | The Part 2 brief requires session management |
| Login response | `{ token, expiresAt, role }` | User details and role (the session cookie is the credential) | Follows from the change above |
| Event distance and type | Not in the Events table | `DistanceKm` and `EventType` (Run, Walk, Cycle) columns added to Events | The Part 2 brief says each event must capture distance and event type |
| Logout | Not planned | `POST /api/auth/logout` | Needed to end a session |
| Organiser registration | Open choice of role | Organiser role needs an invite code | Stops people giving themselves the Organiser role |
| Deleting events, categories and enrolments | Not specified | Blocked with 409 if enrolments or results depend on them | Matches the foreign keys, which have no cascade |
| Full categories | `MaxParticipants` column | Enrolling in a full category returns 409 | Uses the planned column |
