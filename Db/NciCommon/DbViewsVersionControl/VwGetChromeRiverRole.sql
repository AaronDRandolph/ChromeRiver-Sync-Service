SELECT DISTINCT a.EmployeeID, 
	CASE 
		WHEN a.jobTitle LIKE '%Accounts Payable%' AND c.name = 'workforce' then 'APReview-FAPO'
		WHEN a.JobTitle LIKE 'Manager%' AND c.name = 'Accounting & Finance' then 'APApprover'
		WHEN a.JobTitle LIKE 'Supervisor%' AND c.name = 'Accounting & Finance' then 'APApprover'
		WHEN a.JobTitle LIKE '%Accounts Payable' AND c.name = 'Accounting & Finance' then 'APReview'
		WHEN a.jobTitle LIKE '%Unmet Needs%' then 'DRSReview'
		WHEN a.jobTitle LIKE 'Program Manager' then 'DRSReview'
		WHEN a.jobTitle LIKE 'Manager%' AND c.name = 'workforce' then 'APReview-FAPO'
	ELSE 'NULL' End AP_Role
FROM [NCI_COMMON].[dbo].[Employee] a
	INNER JOIN [NCI_COMMON].[dbo].[Person] b ON a.personId =b.personId
	INNER JOIN [NCI_COMMON].[dbo].[Department] c ON a.departmentId =c.departmentId
WHERE  a.codeIdEmploymentStatus IN ('410', '411') AND c.name IN ('workforce','Accounting & Finance','Regional Initiatives') AND (a.jobTitle LIKE'%Accounts Payable%' OR a.jobTitle LIKE'%Unmet%' OR a.jobTitle LIKE 'Manager, Program Accounts' OR a.jobTitle LIKE 'Manager, Accounts Payable')
ORDER BY AP_Role OFFSET 0 ROWS;