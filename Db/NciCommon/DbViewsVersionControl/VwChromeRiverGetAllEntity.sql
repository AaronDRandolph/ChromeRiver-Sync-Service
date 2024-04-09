    --Charter School Entity Feed - Activity
	SELECT DISTINCT 
		SUBSTRING(A.ACCOUNTNUMBER,22,18) 
		EntityCode, 
		'Activity'EntitytypeCode, 
		'1' SortOrder, 
		--Entity Name made up of 3 parts with hyphens in between: PIC code, Site ID and Activity
		SUBSTRING(A.ACCOUNTNUMBER,22,18) + ' : ' + (SELECT TE.DESCRIPTION
		FROM [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTSEGMENTS] AC INNER JOIN
			[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7SEGMENTS] SEG ON AC.GL7SEGMENTSID = SEG.GL7SEGMENTSID INNER JOIN
			[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[TABLEENTRIES] TE ON AC.TABLEENTRIESID = TE.TABLEENTRIESID INNER JOIN
			[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[CODETABLES] C ON TE.CODETABLESID = C.CODETABLESID 
		WHERE (A.GL7ACCOUNTSID = AC.GL7ACCOUNTSID ) AND (C.NAME = 'INTENT')) + ' - ' + --PIC Code

		(SELECT TE.DESCRIPTION
		FROM [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTSEGMENTS] AC INNER JOIN
			[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7SEGMENTS] SEG ON AC.GL7SEGMENTSID = SEG.GL7SEGMENTSID INNER JOIN
			[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[TABLEENTRIES] TE ON AC.TABLEENTRIESID = TE.TABLEENTRIESID INNER JOIN
			[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[CODETABLES] C ON TE.CODETABLESID = C.CODETABLESID 
		WHERE (A.GL7ACCOUNTSID = AC.GL7ACCOUNTSID ) AND (C.NAME = 'SITE ID')) + ' - ' + --Site ID

		(SELECT TE.DESCRIPTION
		FROM [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTSEGMENTS] AC INNER JOIN
			[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7SEGMENTS] SEG ON AC.GL7SEGMENTSID = SEG.GL7SEGMENTSID INNER JOIN
			[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[TABLEENTRIES] TE ON AC.TABLEENTRIESID = TE.TABLEENTRIESID INNER JOIN
			[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[CODETABLES] C ON TE.CODETABLESID = C.CODETABLESID 
		WHERE (A.GL7ACCOUNTSID = AC.GL7ACCOUNTSID ) AND (C.NAME = 'ACTIVITY')) EntityName
		,'CS' Extradata1
		, CASE 
			WHEN status = 1 THEN 1
			ELSE 0
		END AS Active
	FROM [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTS] A INNER JOIN
		 [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTCODES]ACC ON A.GL7ACCOUNTCODESID = ACC.GL7ACCOUNTCODESID
	WHERE ACC.ACCOUNTCODE > 6220 AND (A.status = 1 OR (A.status <> 1 AND A.dateChanged >= DATEADD(DAY, -30, GETDATE())))

UNION   
      --BakerRipley Entity Feed - Cost Center
    SELECT DISTINCT ProjectId EntityCode,'CostCenter'EntityTypeCode,'1' SortOrder,ProjectId + ' : ' + Description EntityName,  
    (	SELECT	CASE WHEN SUBSTRING(pate.DESCRIPTION,1,3) in ('910','920','930','940','938','936') THEN 'DEV'
                                    WHEN SUBSTRING(pate.DESCRIPTION,1,3) in ('891','890','893','000','512','540','810','820','830','833','835','915','948','949','951','952','955','956','960','965','999','894','897','840','841','845','898','700','710','896','899','895','750','650') THEN 'AD'
                                    WHEN SUBSTRING(pate.DESCRIPTION,1,3) in ('580','586','525','560','585') THEN 'EI'
                                    WHEN SUBSTRING(pate.DESCRIPTION,1,3) in ('892') THEN 'PSPE'
                                    WHEN SUBSTRING(pate.DESCRIPTION,1,3) in ('325') THEN 'CS'
                                    WHEN SUBSTRING(pate.DESCRIPTION,1,3) in ('400','440','510','511','587','590','591','592','598','953','530','599') THEN 'CI'
                                    WHEN SUBSTRING(pate.DESCRIPTION,1,3) in ('300','301','325','310','311','312','320','321','323','328','330','340','350') THEN 'HS'
                                    WHEN SUBSTRING(pate.DESCRIPTION,1,3) in ('600','605','620','625','626','627','660','670','680','685') THEN 'HWI'
                                    WHEN SUBSTRING(pate.DESCRIPTION,1,3) in ('272','273','274','275','276','277','278','279','210','211','213','215','216','218','219','225','230','231','232','233','234','235','236','245','250','251','252','266','267','268','282','292','295','296','297','298','299','550','902','551') THEN 'RI'
                                    WHEN SUBSTRING(pate.DESCRIPTION,1,3) in ('100','110','115','120','125','130','135','150','151','160','170','165','280','281','284','286') THEN 'WFI'
                                        ELSE (	SELECT	CASE pate.DESCRIPTION 
                                                            WHEN 'Supporting Services' THEN 'AD' 
                                                            WHEN 'Public Sector Solutions' THEN 'RI'
                                                            WHEN 'Choices in Education' THEN 'HS'
                                                            WHEN 'Senior Services' THEN 'HWI'
                                                            WHEN 'Community Based Initiatives' THEN 'CI'
                                                            WHEN 'Shared Costs' THEN 'AD'
                                                            WHEN 'Strategy' THEN 'PSPE'
                                                            WHEN 'Charter Schools' 
                                                                THEN 'CS'
                                                                ELSE 'REVIEW' 
                                                        END
                                                FROM    [BR-CL-SQL3\MSSQLSERVER3].[NEIGHBORHOOD_CENTERS_INC].[dbo].[GL7PROJECTATTRIBUTES] AS pa WITH (nolock) INNER JOIN
                                                        [BR-CL-SQL3\MSSQLSERVER3].[NEIGHBORHOOD_CENTERS_INC].[dbo].[TABLEENTRIES] AS pate WITH (nolock) ON pa.TABLEENTRIESID = pate.TABLEENTRIESID INNER JOIN
                                                        [BR-CL-SQL3\MSSQLSERVER3].[NEIGHBORHOOD_CENTERS_INC].[dbo].[CODETABLES] AS c WITH (nolock) ON pate.CODETABLESID = c.CODETABLESID
                                                WHERE	(p.GL7PROJECTSID = pa.PARENTID) AND (c.NAME = 'Division')) 
                                    END
                        FROM    [BR-CL-SQL3\MSSQLSERVER3].[NEIGHBORHOOD_CENTERS_INC].[dbo].[GL7PROJECTATTRIBUTES] AS pa WITH (nolock) INNER JOIN
                                [BR-CL-SQL3\MSSQLSERVER3].[NEIGHBORHOOD_CENTERS_INC].[dbo].[TABLEENTRIES] AS pate WITH (nolock) ON pa.TABLEENTRIESID = pate.TABLEENTRIESID INNER JOIN
                                [BR-CL-SQL3\MSSQLSERVER3].[NEIGHBORHOOD_CENTERS_INC].[dbo].[CODETABLES] AS c WITH (nolock) ON pate.CODETABLESID = c.CODETABLESID
                        WHERE   (p.GL7PROJECTSID = pa.PARENTID) AND (c.NAME = 'Program')) ExtraData1,
                        CASE 
							WHEN activeflag = 1 THEN activeflag
							ELSE 0
						END AS Active
    FROM [BR-CL-SQL3\MSSQLSERVER3].[NEIGHBORHOOD_CENTERS_INC].[dbo].[GL7PROJECTS] AS p WITH (nolock)
    WHERE (activeflag = 1 OR (activeflag <> 1 AND dateChanged >= DATEADD(DAY, -30, GETDATE()))) AND
        projectid <> '951-00-100' AND 
        projectid NOT LIKE '%-100%' AND 
        projectid NOT LIKE '%-90%' AND 
        projectid NOT LIKE '%000%' AND 
        projectid NOT LIKE '%120-%' AND 
        projectid NOT LIKE '%999-%' AND
        projectid NOT LIKE '%-999%' 

UNION
    --Charter School Entity Feed - Organization
    SELECT DISTINCT 
        SUBSTRING(A.ACCOUNTNUMBER,16,3) EntityCode, 
        'Organization'EntitytypeCode, 
        '1' SortOrder, 
        SUBSTRING(A.ACCOUNTNUMBER,16,3) + ' : ' + (SELECT TE.DESCRIPTION
        FROM [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTSEGMENTS] AC with (nolock) INNER JOIN
                [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7SEGMENTS] SEG with (nolock) ON AC.GL7SEGMENTSID = SEG.GL7SEGMENTSID INNER JOIN
                [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[TABLEENTRIES] TE with (nolock) ON AC.TABLEENTRIESID = TE.TABLEENTRIESID INNER JOIN
                [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[CODETABLES] C with (nolock) ON TE.CODETABLESID = C.CODETABLESID 
        WHERE (A.GL7ACCOUNTSID = AC.GL7ACCOUNTSID ) AND (C.NAME = 'ORGANIZATION')) EntityName,
        '' ExtraData1,
        CASE 
			WHEN A.status  = 1 THEN 1 
			ELSE 0
		END AS Active
    FROM [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTS] A INNER JOIN
            [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTCODES]ACC ON A.GL7ACCOUNTCODESID = ACC.GL7ACCOUNTCODESID
    WHERE ACC.ACCOUNTCODE > 6220 AND (A.status = 1 OR (A.status <> 1 AND A.dateChanged >= DATEADD(DAY, -30, GETDATE())))

UNION
    -- Divisons
    SELECT DISTINCT CASE WHEN name IN ('Economic Initiatives','Financial Initiatives','Fab Lab','Entrepreneur Connection','Verizon','Adult Education','Verizon Remote Initiative') THEN 'EI' 
                        WHEN name IN ('Prog Strategy Plan Eval','Strategy','Program Planning & Eval') THEN 'PSPE' 
                        WHEN name IN ('Charter School','Charter Schools','Choices in Education') THEN 'CS' 
                        WHEN name IN ('CACFP','Early Childhood Education','Early Head Start','Head Start','ECDC','Child Partnership','Ft Bend Start Up','Rescue Plan','Fort Bend HS','Fort Bend Carryover','Harris County Early Head Start','Fort Bend EHS','CARES','Harris County Head Start') THEN 'HS'
                        WHEN name IN ('Community Initiatives','Comm Based Initiatives','Community Centers','Immigration','Youth Programs','Leadership','BNTW','Summer Youth - OtJ Train','Youth Services') THEN 'CI'
                        WHEN name IN ('Workforce Initiatives','Public Sector Solutions','Career Centers','FAPO','WFS Coastal Bend','WFS East Texas','WFS Rural Capital','PSS Shared Cost','Support Srvc for Vet Fams','ASPIRE','WFS','Health Career Pathway Par','Work Base Learning','Coastal Bend WorkSource','Rural Capital Workforce Board','East Texas','Capital Area','Early REACH','Support Services for Veteran Families','SSVF Shallow','Housing Navigation') THEN 'WFI'
                        WHEN name IN ('Regional Initiatives','CEAP Energy Assistance','Disaster Relief','VITA Program','Energy Efficiency','Harvey','Weatherization','Energy Aid','NTC HC CTC') THEN 'RI'
                        WHEN name IN ('Hlth & Wllns Initiatives','Home Care','Senior Meals','Volunteer Services','Senior Services','Senior Health Promotion','Case Management','Houston Dementia Alliance') THEN 'HWI'
                        WHEN name IN ('Development','Marketing','Turkey Trot','Marketing & Communication','Hearty of Gold','Major Gifts','Events','BakerRipley Experience') THEN 'DEV'
                        WHEN name IN ('Good 2 Go','Procurement & Contracts','Peope & Culture','Supporting Services','Loc Specific Shared Cost','Compliance & QA','Administration','Accounting & Finance','Executive Department','BPDI','Compliance Contract Admin','Executive Admin','Facilities','People & Culture','Finance & Accounting','Human Resources','Information Technology','Procurement','Credit Union','Family Dev Credential Pgm','Adult Day Center','Agency Sponsored Init') THEN 'AD'  
    END EntityCode,
    'Entity Type Code' = 'Division',
    'SortOrder' = '2',
    CASE	WHEN name IN ('Economic Initiatives','Financial Initiatives','Fab Lab','Entrepreneur Connection','Verizon','Adult Education','Verizon Remote Initiative') THEN 'Economic Initiatives' 
            WHEN name IN ('Prog Strategy Plan Eval','Strategy','Program Planning & Eval') THEN 'Prog Strategy Plan Eval' 
            WHEN name IN ('Charter School','Charter Schools','Choices in Education') THEN 'Charter Schools' 
            WHEN name IN ('CACFP','Early Childhood Education','Early Head Start','Head Start','ECDC','Child Partnership','Ft Bend Start Up','Rescue Plan','Fort Bend HS','Fort Bend Carryover','Harris County Early Head Start','Fort Bend EHS','CARES','Harris County Head Start') THEN 'Early Childhood Education'
            WHEN name IN ('Community Initiatives','Comm Based Initiatives','Community Centers','Immigration','Youth Programs','Leadership','BNTW','Summer Youth - OtJ Train') THEN 'Community Initiatives'
            WHEN name IN ('Workforce Initiatives','Public Sector Solutions','Career Centers','FAPO','WFS Coastal Bend','WFS East Texas','WFS Rural Capital','PSS Shared Cost','Support Srvc for Vet Fams','ASPIRE','WFS','Health Career Pathway Par','Work Base Learning','Coastal Bend WorkSource','Rural Capital Workforce Board','East Texas','Capital Area','Early REACH','Support Services for Veteran Families','SSVF Shallow','Housing Navigation') THEN 'Workforce Initiatives'
            WHEN name IN ('Regional Initiatives','CEAP Energy Assistance','Disaster Relief','VITA Program','Energy Efficiency','Harvey','Weatherization','Energy Aid','NTC HC CTC') THEN 'Regional Initiatives'
            WHEN name IN ('Hlth & Wllns Initiatives','Home Care','Senior Meals','Volunteer Services','Senior Services','Senior Health Promotion','Case Management','Houston Dementia Alliance') THEN 'Hlth & Wllns Initiatives'
            WHEN name IN ('Development','Marketing','Turkey Trot','Marketing & Communication','Hearty of Gold','Major Gifts','Events','BakerRipley Experience') THEN 'Development'
            WHEN name IN ('Good 2 Go','Procurement & Contracts','Peope & Culture','Supporting Services','Loc Specific Shared Cost','Compliance & QA','Administration','Accounting & Finance','Executive Department','BPDI','Compliance Contract Admin','Executive Admin','Facilities','People & Culture','Finance & Accounting','Human Resources','Information Technology','Procurement','Credit Union','Family Dev Credential Pgm','Adult Day Center','Agency Sponsored Init') THEN 'Administration'  
    END EntityName, 
    '' ExtraData1,
    1 AS Active
    FROM [NCI_COMMON].[dbo].[Department]
    WHERE name NOT IN ('z','Other')

UNION
    -- Departments
    SELECT DISTINCT D.name EntityCode
        ,'Department' EntityTypeCode
        ,'2' SortOrder
        ,D.name EntityName
        ,'' Extradata1
		,CASE 
			WHEN active  = 1 THEN 1 
			ELSE 0
		END AS Active
    FROM [NCI_COMMON].[dbo].[Department] D
    WHERE active = 1 OR (active <> 1 AND lastModDt >= DATEADD(DAY, -30, GETDATE()))

UNION
    --BakerRipley FundingSource--
    SELECT DISTINCT substring([DESCRIPTION],1,4) EntityCode
        ,'FundingSource' EntityTypeCode
        ,'1' SortOrder
        ,[DESCRIPTION] EntityName
        ,'' Extradata1
        , CASE 
            WHEN Active = '-1' THEN 1
            ELSE 0
        END AS Active
    FROM [BR-CL-SQL3\MSSQLSERVER3].[NEIGHBORHOOD_CENTERS_INC].[dbo].[TABLEENTRIES]
    WHERE CODETABLESID ='128' and active ='-1' OR (active = 0 AND INTEGRATION_DATE_CHANGED >= dateadd(day, -30, getdate())) -- NOTE THERE ARE FUNDING SOURCES THAT ARE DEACTIVATED, HOWEVER NO DATE IS TIED TO THEM (ALL INTEGRATION_DATE_CHANGED ARE NULL AND IT IS THE ONLY DATE COLUMN), IT APPEARS AUTOMATIC DECAVITATION WILL NOT HAPPEN FOR FUNDING SOURCES 

UNION
    --GL Account--
    SELECT DISTINCT MSA.coaCode EntityCode
        ,'GLAcct' EntityTypeCode
        ,'1' SortOrder
        ,MSA.CoaCode + ' : ' + upper(MSA.CoaTitle) EntityName
        ,'' ExtraData1
        ,CASE 
            WHEN MSA.CoaStatus = 'A' THEN 1
            ELSE 0 
        END AS Active
    FROM [NCI_COMMON].[dbo].[MIPCoaSegmentsAccount] AS MSA 
        INNER JOIN [NCI_COMMON].[dbo].[MIPCoaSegment] AS MC ON MSA.coaSegmentID = MC.coaSegmentID 
    WHERE MC.title = 'GL' AND MSA.coaStatus = 'A' OR (MSA.coaStatus <> 'A' AND MSA.LastModDt >= DATEADD(DAY, -30, GETDATE())) 

UNION
    --Office--
    SELECT DISTINCT MSA.coaCode EntityCode
        ,'Office' EntityTypeCode
        ,'1' SortOrder
        ,MSA.coaCode + ' : ' + upper(MSA.coaTitle) EntityName
        , '' ExtraData1
        ,CASE 
            WHEN MSA.CoaStatus = 'A' THEN 1
            ELSE 0 
        END AS Active
    FROM [NCI_COMMON].[dbo].[MIPCoaSegmentsAccount] AS MSA WITH (NOLOCK) 
        INNER JOIN [NCI_COMMON].[dbo].[MIPCoaSegment] AS MC WITH (NOLOCK) ON MSA.coaSegmentID = MC.coaSegmentID 
    WHERE MC.title = 'Career Office' AND MSA.coaStatus = 'A' OR (MSA.coaStatus <> 'A' AND MSA.lastModDt >= DATEADD(DAY, -30, GETDATE()))