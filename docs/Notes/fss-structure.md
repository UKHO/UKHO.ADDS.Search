USE [fss-vnext-e2e-db]
GO

/****** Object:  Table [dbo].[FileAttribute]    Script Date: 19/02/2026 14:52:41 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[FileAttribute](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AttributeKey] [varchar](255) NOT NULL,
	[AttributeValue] [varchar](1024) NOT NULL,
	[FileId] [int] NOT NULL,
 CONSTRAINT [PK_FileAttribute] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[FileAttribute]  WITH CHECK ADD  CONSTRAINT [FK_FileAttribute_File_Id] FOREIGN KEY([FileId])
REFERENCES [dbo].[File] ([Id])
GO

ALTER TABLE [dbo].[FileAttribute] CHECK CONSTRAINT [FK_FileAttribute_File_Id]
GO


USE [fss-vnext-e2e-db]
GO

/****** Object:  Table [dbo].[File]    Script Date: 19/02/2026 14:52:33 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[File](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FileName] [varchar](255) NOT NULL,
	[Status] [int] NOT NULL,
	[FileByteSize] [bigint] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[WrittenOn] [datetime] NULL,
	[FileLocation] [varchar](512) NULL,
	[MIMEType] [varchar](255) NULL,
	[BatchId] [uniqueidentifier] NOT NULL,
	[MD5Hash] [varchar](32) NULL,
 CONSTRAINT [PK_File] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[File]  WITH CHECK ADD  CONSTRAINT [FK_File_Batch_Id] FOREIGN KEY([BatchId])
REFERENCES [dbo].[Batch] ([Id])
GO

ALTER TABLE [dbo].[File] CHECK CONSTRAINT [FK_File_Batch_Id]
GO

USE [fss-vnext-e2e-db]
GO

/****** Object:  Table [dbo].[BusinessUnit]    Script Date: 19/02/2026 14:52:21 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BusinessUnit](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](255) NOT NULL,
	[CostCentre] [varchar](16) NOT NULL,
	[StorageAccount] [varchar](32) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[AcceleratedDownloadThresholdInBytes] [int] NOT NULL,
 CONSTRAINT [PK_BusinessUnit] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[BusinessUnit] ADD  DEFAULT ((1)) FOR [IsActive]
GO

ALTER TABLE [dbo].[BusinessUnit] ADD  DEFAULT ((-1)) FOR [AcceleratedDownloadThresholdInBytes]
GO

USE [fss-vnext-e2e-db]
GO

/****** Object:  Table [dbo].[BatchReadUser]    Script Date: 19/02/2026 14:52:10 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BatchReadUser](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserIdentifier] [varchar](64) NOT NULL,
	[BatchId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_BatchReadUser] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[BatchReadUser]  WITH CHECK ADD  CONSTRAINT [FK_BatchReadUser_Batch_Id] FOREIGN KEY([BatchId])
REFERENCES [dbo].[Batch] ([Id])
GO

ALTER TABLE [dbo].[BatchReadUser] CHECK CONSTRAINT [FK_BatchReadUser_Batch_Id]
GO


USE [fss-vnext-e2e-db]
GO

/****** Object:  Table [dbo].[BatchReadGroup]    Script Date: 19/02/2026 14:51:57 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BatchReadGroup](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GroupIdentifier] [varchar](64) NOT NULL,
	[BatchId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_BatchReadGroup] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[BatchReadGroup]  WITH CHECK ADD  CONSTRAINT [FK_BatchReadGroup_Batch_Id] FOREIGN KEY([BatchId])
REFERENCES [dbo].[Batch] ([Id])
GO

ALTER TABLE [dbo].[BatchReadGroup] CHECK CONSTRAINT [FK_BatchReadGroup_Batch_Id]
GO

USE [fss-vnext-e2e-db]
GO

/****** Object:  Table [dbo].[BatchAttribute]    Script Date: 19/02/2026 14:51:49 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BatchAttribute](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AttributeKey] [varchar](255) NOT NULL,
	[AttributeValue] [varchar](1024) NOT NULL,
	[BatchId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_BatchAttribute] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[BatchAttribute]  WITH CHECK ADD  CONSTRAINT [FK_BatchAttribute_Batch_Id] FOREIGN KEY([BatchId])
REFERENCES [dbo].[Batch] ([Id])
GO

ALTER TABLE [dbo].[BatchAttribute] CHECK CONSTRAINT [FK_BatchAttribute_Batch_Id]
GO

USE [fss-vnext-e2e-db]
GO

/****** Object:  Table [dbo].[Batch]    Script Date: 19/02/2026 14:51:39 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Batch](
	[Id] [uniqueidentifier] NOT NULL,
	[BusinessUnitId] [int] NOT NULL,
	[Status] [int] NOT NULL,
	[CreatedOn] [datetime] NOT NULL,
	[CreatedBy] [varchar](64) NOT NULL,
	[CreatedByIssuer] [varchar](128) NULL,
	[CommittedOn] [datetime] NULL,
	[RolledBackOn] [datetime] NULL,
	[ExpiryDate] [datetime] NULL,
	[ZipSizeInBytes] [bigint] NULL,
 CONSTRAINT [PK_Batch] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Batch]  WITH CHECK ADD  CONSTRAINT [FK_Batch_BusinessUnit_Id] FOREIGN KEY([BusinessUnitId])
REFERENCES [dbo].[BusinessUnit] ([Id])
GO

ALTER TABLE [dbo].[Batch] CHECK CONSTRAINT [FK_Batch_BusinessUnit_Id]
GO






