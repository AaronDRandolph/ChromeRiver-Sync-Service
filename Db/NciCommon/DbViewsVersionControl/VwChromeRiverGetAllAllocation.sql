SELECT DISTINCT  
        --Allocation Name is made up of three parts with hyphens in between: fund code, function and expenditure account
        (SELECT TE.DESCRIPTION
        FROM [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTSEGMENTS] AC INNER JOIN
        [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7SEGMENTS] SEG ON AC.GL7SEGMENTSID = SEG.GL7SEGMENTSID INNER JOIN
        [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[TABLEENTRIES] TE ON AC.TABLEENTRIESID = TE.TABLEENTRIESID INNER JOIN
        [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[CODETABLES] C ON TE.CODETABLESID = C.CODETABLESID 
        WHERE (A.GL7ACCOUNTSID = AC.GL7ACCOUNTSID ) AND (C.NAME = 'FUND CODE')) + ' - ' +  --fund code

        (SELECT TE.DESCRIPTION
        FROM [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTSEGMENTS] AC INNER JOIN
        [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7SEGMENTS] SEG ON AC.GL7SEGMENTSID = SEG.GL7SEGMENTSID INNER JOIN
        [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[TABLEENTRIES] TE ON AC.TABLEENTRIESID = TE.TABLEENTRIESID INNER JOIN
        [BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[CODETABLES] C ON TE.CODETABLESID = C.CODETABLESID 
        WHERE (A.GL7ACCOUNTSID = AC.GL7ACCOUNTSID ) AND (C.NAME = 'FUNCTION')) + ' - ' +    --function

        ACC.description AllocationName, --Expenditure Account
        SUBSTRING (A.ACCOUNTNUMBER,4,11) AllocationNumber
        ,':' ClientName
        ,'CS' ClientNumber
        ,'USD' Currency
        ,'Activity' OnSelect1EntityTypeCode
        ,'Organization' OnSelect2EntityTypeCode
        ,'CS' Type 
        , CASE 
            WHEN A.status = 1 THEN ''
            ELSE CONCAT(CONVERT (nvarchar,a.dateChanged,23),'T00:00:00Z')
			  END AS CloseDate
	FROM	[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTS] A INNER JOIN
			[BR-CL-SQL3\MSSQLSERVER3].[PROMISE_COMMUNITY_SCHOOL].[dbo].[GL7ACCOUNTCODES] ACC ON A.GL7ACCOUNTCODESID = ACC.GL7ACCOUNTCODESID
	WHERE	ACC.ACCOUNTCODE > 6220 AND (A.status = 1 OR (A.status <> 1 AND A.dateChanged >= DATEADD(DAY, -30, GETDATE())))

UNION

    -- BakerRipley Allocation Feed
    SELECT DISTINCT A.[DESCRIPTION] AS AllocationName
        ,[ACCOUNTNUMBER] AllocationNumber
        ,':' ClientName
        ,'BR' ClientNumber
        ,'USD' Currency
        ,'CostCenter' OnSelect1EntityTypeCode
        ,'FundingSource' OnSelect2EntityTypeCode
        ,'BR' Type  
        , CASE 
            WHEN A.status = 1 THEN ''
            ELSE CONCAT(CONVERT (nvarchar,A.dateChanged,23),'T00:00:00Z')
            END AS CloseDate
    FROM [BR-CL-SQL3\MSSQLSERVER3].[NEIGHBORHOOD_CENTERS_INC].[dbo].[GL7ACCOUNTS] A INNER JOIN
            [BR-CL-SQL3\MSSQLSERVER3].[NEIGHBORHOOD_CENTERS_INC].[dbo].[GL7ACCOUNTCODES] ACC ON A.GL7ACCOUNTCODESID = ACC.GL7ACCOUNTCODESID

    WHERE (A.status = 1 OR (A.status <> 1 AND A.dateChanged >= DATEADD(DAY, -30, GETDATE()))) 
    and ACCOUNTNUMBER like '11-%' 
    and (ACC.ACCOUNTCODE in ('1345','1450','1630','2550','2555','7420','7450','7580','8450','8451','8680','8888') 
    or (ACC.ACCOUNTCODE between 1640 and 1680) 
    or (ACC.ACCOUNTCODE between 7020 and 7045) 
    or (ACC.ACCOUNTCODE between 7519 and 8351) 
    or (ACC.ACCOUNTCODE between 8353 and 8390) 
    or  (ACC.ACCOUNTCODE between 8410 and 8434) 
    or  (ACC.ACCOUNTCODE between 8460 and 8479) 
    or (ACC.ACCOUNTCODE between 8510 and 8615) 
    or (ACC.ACCOUNTCODE between 8630 and 8663) 
    or (ACC.ACCOUNTCODE between 9110 and 9220) or (ACC.ACCOUNTCODE between 9110 and 9220))

UNION 

    -- MIP Allocation Feed
    SELECT DISTINCT	Upper(MSA.CoaTitle) AS AllocationName,
                    MSA.CoaCode AS AllocationNumber,
                    ': ' AS ClientName,
                    'MIP' AS ClientNumber,
                    'USD' AS Currency,
                    '' AS OnSelect1EntityTypeCode,
                    '' AS OnSelect2EntityTypeCode,
                    'MIP' AS Type,
                    CASE 
                        WHEN MSA.CoaStatus = 'A' THEN ''
                        ELSE CONCAT(CONVERT (nvarchar,MSA.LastModDt,23),'T00:00:00Z') 
                    END AS CloseDate

    FROM     [NCI_COMMON].[dbo].[MIPCoaSegmentsAccount] AS MSA WITH (NOLOCK)
                INNER JOIN [NCI_COMMON].[dbo].[MIPCoaSegment] AS MC WITH (NOLOCK)
                ON MSA.CoaSegmentID = MC.CoaSegmentID
    WHERE    MC.Title = 'Customer'
                AND MSA.CoaCode LIKE 'SA%'
                AND (MSA.CoaStatus = 'A' OR (MSA.CoaStatus <> 'A' AND MSA.LastModDt >= DATEADD(DAY, -30, GETDATE())))
            
UNION

    SELECT DISTINCT	Upper(MSA.CoaTitle) AS AllocationName,
                    MSA.CoaCode AS AllocationNumber,
                    ': ' AS ClientName,
                    'MIP' AS ClientNumber,
                    'USD' AS Currency,
                    'GLAcct' AS OnSelect1EntityTypeCode,
                    'Office' AS OnSelect2EntityTypeCode,
                    'MIP' AS Type,
                    CASE 
                        WHEN MSA.CoaStatus = 'A' THEN ''
                        ELSE CONCAT(CONVERT (NVARCHAR,MSA.LastModDt,23),'T00:00:00Z') 
                    END AS CloseDate
    FROM	[NCI_COMMON].[dbo].[MIPCoaSegmentsAccount] AS MSA WITH (NOLOCK)
            INNER JOIN [NCI_COMMON].[dbo].[MIPCoaSegment] AS MC WITH (NOLOCK) ON MSA.CoaSegmentID = MC.CoaSegmentID
    WHERE    MC.Title = 'Fund' AND (MSA.CoaStatus = 'A' OR (MSA.CoaStatus <> 'A' AND MSA.LastModDt >= DATEADD(DAY, -30, GETDATE())))