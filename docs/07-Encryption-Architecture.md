# Encryption Architecture

Secure Customer Access and Credential Management Platform

This document describes how secret data is protected at rest. The design goal: secret
credential values and attachments are **never stored in clear text**, while
non-secret fields remain searchable.

---

## 1. Algorithm

- **Cipher:** AES‑256 in **GCM** mode (authenticated encryption — confidentiality +
  integrity). Implemented with .NET's `System.Security.Cryptography.AesGcm`.
- **Key size:** 256-bit (32 bytes).
- **Nonce:** 12 bytes, randomly generated per encryption (`RandomNumberGenerator`).
- **Auth tag:** 16 bytes.
- **Token format:** `v1:` + Base64( `nonce(12) | tag(16) | ciphertext` ).
  The version prefix allows future algorithm rotation; values without the prefix are
  passed through unchanged (forward/backward compatible reads).

See `src/SecureAccess.Infrastructure/Security/AesGcmEncryptionService.cs`.

---

## 2. Key management

```
┌──────────────────────────────┐
│ AES-256 master key (32 bytes)│  used to encrypt/decrypt credential secrets
└──────────────┬───────────────┘
               │ protected by
               ▼
┌──────────────────────────────┐
│ ASP.NET Core Data Protection │  (DPAPI-backed on Windows)
│  key ring (App_Data/keys/dp) │
└──────────────────────────────┘
```

- On first run, the platform **generates a random 256-bit master key** and writes it
  to `Encryption:KeyFilePath` (default `App_Data/keys/credential-master.key`),
  **protected by ASP.NET Core Data Protection** (`IDataProtector`). The key file on
  disk is therefore itself encrypted — the raw master key is never persisted in clear.
- On subsequent runs, the master key is read and **unprotected** via Data Protection.
- `Encryption:SeedKeyBase64` may supply a fixed 32-byte key (Base64) for controlled
  key provisioning / multi-node consistency instead of random generation.

> Because the master key is protected by the Data Protection key ring, the key ring
> (`App_Data/keys/dp`) is as sensitive as the key itself. **Back up `App_Data` with
> the database, and restrict its NTFS ACLs to the app-pool identity.**

---

## 3. Field-level (column-level) protection

Each credential type defines fields; each field can be flagged **Is Secret**.

When a credential is saved, `CredentialDataProtector` splits the field values:

| Storage column | Contents |
|----------------|----------|
| `Credentials.PlainData` | JSON of **non-secret** fields → stored as plain text, **searchable**. |
| `Credentials.SecretData` | JSON of **secret** fields → **AES‑256‑GCM encrypted** (the `v1:` token), then stored. |

On read:
- List/search views use `PlainData` only — secrets are never materialized.
- The **request workflow** (and authorized reveal) decrypts `SecretData` on demand,
  and every reveal is audited.

Attachments are encrypted as byte streams via `EncryptBytes`/`DecryptBytes` with the
same master key and stored outside the database (under `App_Data`), with only metadata
(name, size, hash, path) in the `Attachments` table.

---

## 4. Transport & session protection (defense in depth)

- **HTTPS/TLS** enforced; HSTS in production.
- **API**: JWT bearer tokens signed with `Jwt:SigningKey` (HMAC). Tokens carry roles
  and permissions as claims and expire after `Jwt:AccessTokenMinutes`.
- **Web**: secure, HTTP-only authentication cookies with idle timeout.
- **Password storage**: user passwords are hashed with **BCrypt** (work factor), never
  reversibly encrypted.

---

## 5. Key rotation (procedure)

The `v1:` prefix is the rotation hook. To rotate the master key:

1. Back up DB + `App_Data`.
2. Provision the new key (new `SeedKeyBase64` or a managed rotation step).
3. Run a re-encryption pass: decrypt each `SecretData` with the old key, re-encrypt
   with the new key, bump the token prefix (e.g. `v2:`), and save.
4. Verify reveals on a sample, then retire the old key.

(Data Protection key-ring rotation is automatic per its configured lifetime; the AES
master key rotation above is a separate, explicit operation.)

---

## 6. What is **never** done

- Secrets are **never** written to logs (Serilog logs metadata/actions, not values).
- Secrets are **never** returned by search or list endpoints.
- Secrets are **never** exported in plaintext.
- The raw master key is **never** stored unprotected on disk.
