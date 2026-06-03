# REST API Documentation

Secure Customer Access and Credential Management Platform

The REST API (`SecureAccess.Api`) exposes the platform's functionality for
automation and integration. It is documented interactively via **Swagger / OpenAPI**
at `/swagger` and described here.

- **Base URL:** `https://<api-host>/`
- **Auth:** JWT Bearer (`Authorization: Bearer <token>`).
- **Content type:** `application/json` unless noted.
- **Authorization:** endpoints are protected by granular **permission policies**
  (e.g. `credentials.view`). A 401 means not authenticated; a 403 means the token
  lacks the required permission.

---

## 1. Authentication

### POST `/api/auth/login`
Request:
```json
{ "userName": "tech1", "password": "••••••••" }
```
Response (200):
```json
{
  "token": "<jwt>",
  "expiresAtUtc": "2026-06-03T12:00:00Z",
  "user": { "id": 5, "userName": "tech1", "fullName": "...", "roles": ["Technician"], "permissions": ["credentials.request", "..."] },
  "mustChangePassword": false
}
```
Use the returned `token` as `Authorization: Bearer <token>` on subsequent calls.

### POST `/api/auth/logout`
Ends/records the current session. Requires a valid token.

### POST `/api/auth/change-password`
```json
{ "currentPassword": "old", "newPassword": "new-strong-secret" }
```

The JWT embeds the user's roles and permissions as claims and is valid for
`Jwt:AccessTokenMinutes`.

---

## 2. Credential request workflow

### POST `/api/credentials/request`
Gateway for retrieving credentials. A reason and internal ticket are **mandatory**;
the request is persisted (access request + audit) **before** any secret is returned.

```json
{
  "machineId": 12,
  "credentialId": null,
  "reasonCategory": "Incident",
  "reason": "Customer reported PLC offline",
  "internalTicket": "HD-4821",
  "customerTicket": "CUST-99"
}
```
Response: the resolved credentials for that machine (machine → client → global
override), including decrypted secret fields the caller is authorized to see.

---

## 3. Credentials

| Method & path | Permission | Description |
|---------------|------------|-------------|
| `GET /api/credentials/machine/{machineId}` | `credentials.view` | Credentials scoped to a machine. |
| `GET /api/credentials/client/{clientId}` | `credentials.view` | Client-scoped credentials. |
| `GET /api/credentials/global` | `credentials.view` | Global credentials. |
| `GET /api/credentials/{id}` | `credentials.view` | Single credential (metadata; secrets gated). |
| `POST /api/credentials` | `credentials.create` | Create a credential. |
| `PUT /api/credentials/{id}` | `credentials.edit` | Update a credential. |
| `DELETE /api/credentials/{id}` | `credentials.delete` | Delete a credential. |

### Credential types
`GET/POST/PUT/DELETE /api/credential-types[/{id}]` — manage predefined/custom types
and their field schema (`credentials.create` / related permissions).

---

## 4. Clients & machines

| Method & path | Description |
|---------------|-------------|
| `GET /api/clients` (supports search query) / `GET /api/clients/{id}` | List / get clients. |
| `POST /api/clients` / `PUT /api/clients/{id}` / `DELETE /api/clients/{id}` | CRUD. |
| `GET /api/machines` / `GET /api/machines/{id}` | List / get machines. |
| `POST /api/machines` / `PUT /api/machines/{id}` / `DELETE /api/machines/{id}` | CRUD. |

---

## 5. Templates & bulk

| Method & path | Description |
|---------------|-------------|
| `GET/POST/PUT/DELETE /api/templates[/{id}]` | Manage credential templates. |
| `POST /api/templates/apply` | Apply a template to machines in bulk. |
| `POST /api/bulk/copy/machine-to-machines` | Copy credentials machine → machines. |
| `POST /api/bulk/copy/client-to-clients` | Copy credentials client → clients. |
| `POST /api/bulk/update` | Bulk update (fill-empty or overwrite + confirmation). |

---

## 6. Users & roles

| Method & path | Permission | Description |
|---------------|------------|-------------|
| `GET /api/users` / `GET /api/users/{id}` | `users.manage` | List / get users. |
| `POST /api/users` / `PUT /api/users/{id}` | `users.manage` | Create / update user. |
| `GET /api/users/roles` | `roles.manage` | List roles. |
| `PUT /api/users/roles/{roleId}/permissions` | `roles.manage` | Replace a role's permissions. |

---

## 7. Reports, dashboard, audit, search

| Method & path | Permission | Description |
|---------------|------------|-------------|
| `GET /api/dashboard` | `dashboard.view` | Dashboard metrics. |
| `POST /api/reports/credential-access` | `reports.view` | Credential access report. |
| `POST /api/reports/technician-activity` | `reports.view` | Technician activity. |
| `POST /api/reports/customer-access` | `reports.view` | Customer access. |
| `POST /api/reports/machine-access` | `reports.view` | Machine access. |
| `POST /api/reports/credential-usage` | `reports.view` | Credential usage. |
| `POST /api/reports/reason-analysis` | `reports.view` | Reason analysis. |
| `POST /api/reports/change-history` | `reports.view` | Change history. |
| `POST /api/audit/query` | `audit.view` | Filtered, paged audit log. |
| `GET /api/audit/sessions` | `audit.view` | Session tracking. |
| `GET /api/search?q=...` | (authenticated) | Global search (non-secret fields). |

Report endpoints accept a JSON body with a date range and optional filters.

---

## 8. Import / export

| Method & path | Permission | Description |
|---------------|------------|-------------|
| `POST /api/data/import/{entity}` | `data.import` | Import `clients`/`machines` from XLSX/CSV (multipart upload). |
| `GET /api/data/export/clients` | `data.export` | Export clients (format via query, e.g. `?format=xlsx|csv|pdf`). |
| `GET /api/data/export/machines` | `data.export` | Export machines. |

---

## 9. Health

`GET /health` — liveness/readiness (includes a DB check). Returns `Healthy` /
`Unhealthy`.

---

## 10. Errors

Standard HTTP status codes:
- `400` validation error (body includes the message),
- `401` missing/invalid token,
- `403` authenticated but lacking the required permission,
- `404` not found,
- `409` conflict (e.g. duplicate code),
- `500` unexpected error (details are logged via Serilog, not leaked to clients).

For the authoritative, always-current contract, use the **Swagger UI** at `/swagger`
or the OpenAPI document it serves.
