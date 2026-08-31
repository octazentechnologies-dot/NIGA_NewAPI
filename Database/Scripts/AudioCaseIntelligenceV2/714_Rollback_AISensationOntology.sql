/*
    Rollback Task 3 — AISensationOntology + metaphor grounding columns
*/

IF COL_LENGTH('dbo.AIMetaphorResolution', 'OntologyId') IS NOT NULL
    ALTER TABLE dbo.AIMetaphorResolution DROP COLUMN OntologyId;
GO

IF COL_LENGTH('dbo.AIMetaphorResolution', 'GroundedInOntology') IS NOT NULL
BEGIN
    DECLARE @dfGrounded SYSNAME;
    SELECT @dfGrounded = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AIMetaphorResolution')
      AND c.name = N'GroundedInOntology';
    IF @dfGrounded IS NOT NULL
        EXEC(N'ALTER TABLE dbo.AIMetaphorResolution DROP CONSTRAINT [' + @dfGrounded + N']');
    ALTER TABLE dbo.AIMetaphorResolution DROP COLUMN GroundedInOntology;
END
GO

IF COL_LENGTH('dbo.AIMetaphorResolution', 'IsMetaphor') IS NOT NULL
BEGIN
    DECLARE @dfMeta SYSNAME;
    SELECT @dfMeta = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.AIMetaphorResolution')
      AND c.name = N'IsMetaphor';
    IF @dfMeta IS NOT NULL
        EXEC(N'ALTER TABLE dbo.AIMetaphorResolution DROP CONSTRAINT [' + @dfMeta + N']');
    ALTER TABLE dbo.AIMetaphorResolution DROP COLUMN IsMetaphor;
END
GO

IF OBJECT_ID(N'dbo.AISensationOntology', N'U') IS NOT NULL
    DROP TABLE dbo.AISensationOntology;
GO
