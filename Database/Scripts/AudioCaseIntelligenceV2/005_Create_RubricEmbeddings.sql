-- JSON embedding vectors (not SQL Server native vector type)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RubricEmbeddings' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RubricEmbeddings
    (
        Id              BIGINT IDENTITY(1,1) NOT NULL,
        RubricId        INT NOT NULL,
        EmbeddingJson   NVARCHAR(MAX) NOT NULL,
        ModelName       NVARCHAR(100) NOT NULL,
        TextHash        CHAR(64) NOT NULL,
        SourceType      NVARCHAR(30) NOT NULL,
        CreatedDate     DATETIME NOT NULL CONSTRAINT DF_RubricEmbeddings_CreatedDate DEFAULT (GETUTCDATE()),
        UpdatedDate     DATETIME NULL,
        CONSTRAINT PK_RubricEmbeddings PRIMARY KEY (Id),
        CONSTRAINT FK_RubricEmbeddings_SubSection FOREIGN KEY (RubricId)
            REFERENCES dbo.SubSectionMaster (SubSectionId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_RubricEmbeddings_RubricId ON dbo.RubricEmbeddings (RubricId);
GO

CREATE NONCLUSTERED INDEX IX_RubricEmbeddings_TextHash ON dbo.RubricEmbeddings (TextHash);
GO
