# Introduction 
The ChromeRiver Service is a .Net Core Service used to automatically synchronize agency data in ChromeRiver. People data is collected from the IAM database, whereas entity and allocation data are collected from the on premise database. This service handles the activation and deactivation of each of these object types. For more details on ChromeRiver see the wiki section of the project on Azure DevOps. For more information of windows service using BackgroundService see https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service#rewrite-the-worker-class.  

# Getting Started
1.  Clone the repository
2.  Download all of the required packages with the dotnet CLI using the 'dotnet restore' command.
3.  Set the following required environment variables with values provided in keepass.
    - IAM_PROD_CONNECTION_STRING (keypass name: IAM Database Production Credentials)
    - NCI_LOCAL_CONNECTION_STRING (keypass name: NCI_LOCAL_CONNECTION_STRING)
        - Note: this will need to be environment based, or handled through the pipelines
    - MAIL_SERVER_PASSWORD (keypass name: Mail Server Credentials)
        

# Build and Test
Run in the appropriate environment or debug mode using Visual Studio. The service can also be run in debug mode using Visual Studio Code.  

# Contribute
The data coming from the on-premise database is provided from a view. Copies of these views are found in Db/NciCommon/DbViewsVersionControl. The views should first be edited in the project before altering the view. Doingso will ensure version controlling a streamlined peer review process. If an alteration of a view leads to any change in the data structure a new scaffolding will be required. Each time you scaffold, you will need to include all the views in order for them to be present in the newly generated DBContext file. Also you will need to persist the IConfiguration implementation for gathering the connection string. By default, the scaffold command will hard code the connection string inside the new DBContext file, however there are options to suppress this and vary whether you are using the .Net CLI or the VS PMC. For instructions and details on scaffolding see https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/?tabs=dotnet-core-cli.
