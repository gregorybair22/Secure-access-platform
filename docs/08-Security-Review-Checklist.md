# Security Review Checklist

Secure Customer Access and Credential Management Platform

Complete this checklist before go-live and re-run it periodically (at least quarterly
and after any major change). Mark each item Pass / Fail / N/A with notes.

---

## 1. Authentication & accounts
- [ ] Mandatory login enforced on all pages/endpoints (no anonymous access except
      the login page and `/health`).
- [ ] Seeded admin password (`Seed:AdminPassword`) changed from default before launch.
- [ ] Passwords hashed with BCrypt (no reversible storage).
- [ ] Failed-login lockout active; thresholds reviewed.
- [ ] "Must change password" enforced for new/reset accounts.
- [ ] No shared accounts; each user is individually named.

## 2. Authorization
- [ ] Role-based + permission-based checks enforced **server-side** on every action
      (UI hiding is not relied upon for security).
- [ ] Administrator / Technician / Auditor permission sets reviewed (least privilege).
- [ ] Restricted clients hide reveals from non-administrators.
- [ ] API returns 403 (not 200) when a permission is missing.

## 3. Cryptography
- [ ] Secret credential fields stored AES-256-GCM encrypted (`v1:` tokens) in
      `SecretData`; non-secret fields only in `PlainData`.
- [ ] AES master key generated/stored **protected by Data Protection**, never in clear.
- [ ] `Encryption:KeyFilePath` and `App_Data/keys` ACL-restricted to the app identity.
- [ ] Attachments stored encrypted; only metadata in the database.
- [ ] `Jwt:SigningKey` is a long (≥ 32 chars) random secret, unique per environment.
- [ ] No secrets in source control or committed appsettings.

## 4. Transport & network
- [ ] HTTPS enforced; HTTP redirects to HTTPS; HSTS enabled in production.
- [ ] TLS certificate valid, trusted, and not expiring soon.
- [ ] IP allow-list (`Security:AllowedIpRanges`) configured if required.
- [ ] CORS (`Cors:AllowedOrigins`) restricted to known origins (no `*` in prod).
- [ ] Blazor WebSocket endpoint reachable; session affinity configured if load-balanced.

## 5. Sessions
- [ ] Auth cookies are Secure + HttpOnly; SameSite reviewed.
- [ ] Idle session timeout (`Session:TimeoutMinutes`) configured.
- [ ] Logout ends and records the session.
- [ ] JWT lifetime (`Jwt:AccessTokenMinutes`) appropriate.

## 6. Auditing & logging
- [ ] Audit, change-history, and login logs are append-only and populated.
- [ ] Credential reveals always create an access request + audit entry (reason +
      internal ticket mandatory; verified it cannot be bypassed).
- [ ] 5-year retention strategy in place (archival, never hard-delete).
- [ ] Secrets never appear in logs.
- [ ] Serilog sinks/locations secured; log files ACL-restricted.

## 7. Data protection & input handling
- [ ] Server-side validation on all create/update endpoints.
- [ ] EF Core parameterized queries (no string-concatenated SQL).
- [ ] Import files validated (type/size) before processing.
- [ ] Error responses do not leak stack traces/secrets (`DetailedErrors` off in prod).

## 8. Backup & recovery
- [ ] Nightly DB backup scheduled and verified (`RESTORE VERIFYONLY`).
- [ ] `App_Data` (attachments + Data Protection keys) backed up with the DB.
- [ ] Restore tested on a non-production host (reveal a secret + download an attachment).
- [ ] Off-site/long-term retention satisfies the 5-year requirement.

## 9. Deployment & hardening
- [ ] App pools run under low-privilege dedicated identities (gMSA preferred).
- [ ] DB account has least privilege (no `sysadmin`; `db_ddladmin` only if app migrates).
- [ ] OS, .NET Hosting Bundle, and SQL Server patched.
- [ ] Swagger UI exposure reviewed (restrict/disable on public-facing API if required).
- [ ] Unused ports/sites/features disabled on the server.

## 10. Ownership & licensing
- [ ] 100% of source code owned by the company (no proprietary paid components).
- [ ] Third-party libraries are free/open (ClosedXML, CsvHelper, QuestPDF Community,
      Serilog, BCrypt.Net) and their licenses recorded.

---

**Reviewer:** ____________________  **Date:** ____________  **Result:** ☐ Pass ☐ Fail

Findings / remediation:
```
(record findings here)
```
