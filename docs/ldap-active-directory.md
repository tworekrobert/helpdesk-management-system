# LDAP / Active Directory Integration Concept

This project currently uses seeded local accounts and JWT authentication so reviewers can run it with one command. In a company deployment, the authentication boundary can be moved to LDAP or Active Directory without changing the ticket and asset workflows.

## Goals

- Authenticate users against the company directory instead of storing local passwords.
- Keep local shadow users for ticket ownership, audit logs, assignments and reporting.
- Map directory groups to application roles: `Employee`, `Technician`, `Admin`.
- Preserve JWT-based API authorization after successful external authentication.

## Proposed Flow

1. User submits email and password to `/api/auth/login`.
2. API validates the credentials against LDAP/AD over LDAPS.
3. API reads directory attributes such as display name, email, department and group membership.
4. API creates or updates the local `users` row.
5. API maps groups to roles and issues the same JWT used by the current frontend.

## Group Mapping

| Directory group | Application role |
| --- | --- |
| `IT-Helpdesk-Employees` | `Employee` |
| `IT-Helpdesk-Technicians` | `Technician` |
| `IT-Helpdesk-Admins` | `Admin` |

The local role should be derived from directory groups during login or synchronization. Manual role editing can remain available only for demo/local mode.

## Local Shadow User

The local `users` table should remain the source for domain relationships:

- `CreatedByUserId` on tickets
- `AssignedTechnicianId` on tickets
- `AssignedToUserId` on assets
- audit log actor ids
- report grouping and dashboard metrics

LDAP/AD should not replace these relational links. It should only provide identity verification and profile synchronization.

## Security Notes

- Use LDAPS or StartTLS only.
- Do not store LDAP passwords.
- Keep JWT expiration short and continue validating issuer, audience and signing key.
- Log authentication failures without writing submitted passwords.
- Prefer group allowlists over broad directory access.

## Implementation Shape

A production-ready version can add:

- `IExternalDirectoryAuthenticator`
- `LdapDirectoryAuthenticator`
- configuration for LDAP host, base DN, bind DN and group mappings
- a feature flag such as `Authentication:Mode = Local | Ldap`
- integration tests with a disposable LDAP container or contract tests around the authenticator

This keeps the portfolio demo simple while showing how the system would fit into a real internal IT environment.
