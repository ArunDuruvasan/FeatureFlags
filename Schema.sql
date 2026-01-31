USE [FeatureFlagDB]
GO
/****** Object:  Table [dbo].[FeatureFlags]    Script Date: 31-01-2026 14:43:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FeatureFlags](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Description] [nvarchar](255) NULL,
	[DefaultState] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FeatureOverrides]    Script Date: 31-01-2026 14:43:54 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FeatureOverrides](
	[Id] [uniqueidentifier] NOT NULL,
	[FeatureId] [uniqueidentifier] NULL,
	[OverrideType] [nvarchar](20) NOT NULL,
	[OverrideKey] [nvarchar](100) NOT NULL,
	[State] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[FeatureFlags] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[FeatureOverrides] ADD  DEFAULT (newid()) FOR [Id]
GO
ALTER TABLE [dbo].[FeatureOverrides]  WITH CHECK ADD FOREIGN KEY([FeatureId])
REFERENCES [dbo].[FeatureFlags] ([Id])
GO
