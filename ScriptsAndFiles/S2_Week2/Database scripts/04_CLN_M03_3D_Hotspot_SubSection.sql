/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 04_CLN_M03_3D_Hotspot_SubSection.sql
Purpose      : CLN-20.02 hotspot → subsection mapping. Masters already exist;
               add SubSectionId and backfill where HotspotName matches SubSectionName.
Use          : Run after 03.
Prerequisites: dbo.ThreeDBodyPartSectionHotspot, dbo.SubSectionMaster.
Idempotent   : Yes.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.ThreeDBodyPartSectionHotspot', N'U') IS NULL
BEGIN
    RAISERROR('dbo.ThreeDBodyPartSectionHotspot is missing. Stop.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.ThreeDBodyPartSectionHotspot', N'SubSectionId') IS NULL
    ALTER TABLE dbo.ThreeDBodyPartSectionHotspot ADD SubSectionId INT NULL;
GO

IF OBJECT_ID(N'dbo.ThreeDBodyPartSectionHotspot', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.SubSectionMaster', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ThreeDBodyPartSectionHotspot', N'SubSectionId') IS NOT NULL
BEGIN
    UPDATE h
    SET h.SubSectionId = s.SubSectionID
    FROM dbo.ThreeDBodyPartSectionHotspot h
    INNER JOIN dbo.SubSectionMaster s
        ON LTRIM(RTRIM(h.HotspotName)) = LTRIM(RTRIM(s.SubSectionName))
       AND ISNULL(s.DeleteStatus, 0) = 0
    WHERE h.SubSectionId IS NULL
      AND ISNULL(h.DeleteStatus, 0) = 0;
END
GO

IF OBJECT_ID(N'dbo.ThreeDBodyPartSectionHotspot', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ThreeDBodyPartSectionHotspot', N'SubSectionId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_3DHotspot_SubSectionId_S2' AND object_id = OBJECT_ID(N'dbo.ThreeDBodyPartSectionHotspot'))
    CREATE INDEX IX_3DHotspot_SubSectionId_S2 ON dbo.ThreeDBodyPartSectionHotspot (SubSectionId);
GO

PRINT '04_CLN_M03_3D_Hotspot_SubSection.sql complete';
GO
