/* ============================================================================
   Secure Customer Access and Credential Management Platform
   05 - Test / Demo Data (idempotent)
   ----------------------------------------------------------------------------
   Loads sample Clients and Machines for evaluation and UAT.

   NOTE on credentials:
     Credential secret values are encrypted at the column level (AES-256) by the
     application using its Data Protection key ring. They therefore CANNOT be
     inserted as plaintext via SQL. Create demo credentials through the web UI
     (Credentials page) or the REST API so they are encrypted correctly.

   Status enum: 1 = Active, 2 = Inactive, 3 = Suspended, 4 = Archived
   Run against the SecureAccess database AFTER the schema has been created.
   ============================================================================ */

SET NOCOUNT ON;

/* ---------- Clients ---------- */
MERGE INTO [Clients] AS tgt
USING (VALUES
    (N'Contoso Manufacturing', N'CONTOSO',  N'123 Industrial Way, Detroit, MI',  N'Alice Reyes',  N'+1-313-555-0100', N'it@contoso.example',   1, 0),
    (N'Northwind Logistics',   N'NORTHW',   N'500 Harbor Blvd, Seattle, WA',      N'Bob Tanaka',   N'+1-206-555-0142', N'ops@northwind.example',1, 0),
    (N'Fabrikam Health',       N'FABRIK',   N'9 Clinic Road, Austin, TX',         N'Dr. Chen',     N'+1-512-555-0188', N'admin@fabrikam.example',1, 1)  -- restricted client
) AS src (Name, CustomerCode, Address, ContactPerson, PhoneNumber, Email, Status, IsRestricted)
ON tgt.CustomerCode = src.CustomerCode
WHEN NOT MATCHED THEN
    INSERT (Name, CustomerCode, Address, ContactPerson, PhoneNumber, Email, Status, IsRestricted, CreatedAtUtc)
    VALUES (src.Name, src.CustomerCode, src.Address, src.ContactPerson, src.PhoneNumber, src.Email, src.Status, src.IsRestricted, SYSUTCDATETIME());

/* ---------- Machines ---------- */
;WITH c AS (SELECT Id, CustomerCode FROM [Clients])
MERGE INTO [Machines] AS tgt
USING (
    SELECT m.Name, m.SerialNumber, m.Model, m.Location, m.Status, c.Id AS ClientId
    FROM (VALUES
        (N'CONTOSO', N'CTX-PLC-01',  N'SN-CTX-1001', N'Siemens S7-1500',   N'Plant Floor A', 1),
        (N'CONTOSO', N'CTX-HMI-01',  N'SN-CTX-2002', N'WinCC Panel',       N'Plant Floor A', 1),
        (N'NORTHW',  N'NW-SRV-01',   N'SN-NW-3003',  N'Dell PowerEdge',    N'Server Room',   1),
        (N'NORTHW',  N'NW-WS-07',    N'SN-NW-4004',  N'HP EliteDesk',      N'Dispatch',      1),
        (N'FABRIK',  N'FB-IMG-01',   N'SN-FB-5005',  N'GE Imaging Console',N'Radiology',     1)
    ) AS m(CustomerCode, Name, SerialNumber, Model, Location, Status)
    JOIN c ON c.CustomerCode = m.CustomerCode
) AS src (Name, SerialNumber, Model, Location, Status, ClientId)
ON tgt.SerialNumber = src.SerialNumber
WHEN NOT MATCHED THEN
    INSERT (ClientId, Name, SerialNumber, Model, Location, Status, CreatedAtUtc)
    VALUES (src.ClientId, src.Name, src.SerialNumber, src.Model, src.Location, src.Status, SYSUTCDATETIME());

PRINT 'Test data load complete.';
SELECT (SELECT COUNT(*) FROM [Clients]) AS Clients, (SELECT COUNT(*) FROM [Machines]) AS Machines;
