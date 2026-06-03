# Administrator Manual

Secure Customer Access and Credential Management Platform

Audience: platform administrators responsible for users, roles, clients, credential
types, and oversight (audit, reports, dashboard).

---

## 1. Roles & permissions

Three roles are seeded; permissions are granular and assignable per role under
**Roles**.

| Role | Purpose | Typical permissions |
|------|---------|---------------------|
| **Administrator** | Full control | All permissions, including user/role management. |
| **Technician** | Day-to-day support | Request/view credentials, view clients/machines, run their own activity. |
| **Auditor** | Read-only oversight | View audit logs, sessions, reports, dashboard. No credential reveal. |

Permissions are grouped by area (clients, machines, credentials, users, roles,
reports, audit, dashboard, bulk, templates, data). Examples:
`credentials.view`, `credentials.create`, `credentials.request`,
`users.manage`, `roles.manage`, `audit.view`, `reports.view`, `bulk.operations`,
`data.export`.

> Navigation and actions in the UI are permission-aware: users only see what they
> are allowed to use, and the server re-checks every action.

### Managing roles
1. Go to **Roles**.
2. Select a role; toggle the permission checkboxes.
3. Save. Changes apply on the user's next request/login.

---

## 2. Users

Under **Users** you can:
- Create a user (username, full name, email, status, assigned roles).
- Edit a user; set status to **Active / Inactive / Suspended**.
- New users can be flagged to **must change password** at first login.

Account protection:
- Failed logins increment a counter; after the threshold the account is
  **locked out** for a cooldown window.
- All login attempts (success/failure) are written to the **Login Logs**.

> The seeded admin account password (`Seed:AdminPassword`) must be changed on first
> login. Do not share administrator accounts; create individual named accounts.

---

## 3. Clients & machines

- **Clients** — name, customer code, address, contact, phone, email, notes, status,
  and an **Is Restricted** flag. Restricted clients hide credential reveals from
  non-administrators.
- **Machines** — belong to a client; name, serial number, model, location, status,
  installation date, notes.

Deleting a client cascades to its machines and machine-level credentials; any
client-level credentials are removed first automatically.

---

## 4. Credential types (predefined + custom)

Under **Credential Types** you define the *shape* of credentials:

- Predefined types are seeded (e.g. RustDesk, AnyDesk, RDP, VPN, SSH, Web, etc.).
- Create **unlimited custom types**. Each type has an ordered list of **fields**:
  - `Key`, `Label`, `Type` (text/password/number/url/multiline/…)
  - **Is Secret** — encrypted at rest, hidden until explicitly revealed.
  - **Is Required** — enforced on save.
  - **Is Searchable** — included in global search (only for non-secret fields).

Secret fields are stored encrypted (AES-256); non-secret fields are stored in a
searchable plain column. See [07-Encryption-Architecture.md](07-Encryption-Architecture.md).

---

## 5. Credentials & inheritance

Credentials have a **scope**:
- **Global** — apply to all machines/clients.
- **Client** — apply to all machines of a client.
- **Machine** — apply to one machine.

Resolution order is **Machine → Client → Global** per credential type (a more
specific credential overrides a more general one of the same type).

**Templates** let you define a set of credentials once and apply them in bulk to many
machines (**Templates** page). **Bulk** operations let you copy credentials between
machines/clients and bulk-update with *fill-empty* or *overwrite* (with confirmation).

---

## 6. Oversight

- **Dashboard** — activity, customer, security, and audit metrics at a glance.
- **Reports** — seven built-in reports (Credential Access, Technician Activity,
  Customer Access, Machine Access, Credential Usage, Reason Analysis, Change History)
  with date filters; exportable.
- **Audit** — append-only audit log with filtering and pagination.
- **Sessions** — per-session login/logout, duration, and resources accessed.

Audit, change-history, and login data are **append-only** and retained for **5 years**
(archive flag, never hard-deleted). They remain searchable after archival.

---

## 7. Import / export

Under **Import / Export**:
- **Export** clients/machines to **XLSX, CSV, or PDF**.
- **Import** clients/machines from **XLSX or CSV**.

Exports of secret credential values are not produced in plaintext.

---

## 8. Security administration

- **IP allow-list**: set `Security:AllowedIpRanges` (CIDR) to restrict access by
  network. Empty = allow all.
- **Sessions**: idle timeout via `Session:TimeoutMinutes` (Web).
- **CORS**: restrict API origins via `Cors:AllowedOrigins`.
- **Backups**: ensure DB **and** `App_Data` are backed up together (key ring!).
  See [06-Backup-and-Restore.md](06-Backup-and-Restore.md).

Review the [Security Review Checklist](08-Security-Review-Checklist.md) before go-live
and periodically thereafter.
