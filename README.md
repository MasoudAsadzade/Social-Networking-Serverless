# Social Networking Serverless Kata

This project implements a serverless-based social networking application u  sing Signalr, Asp.NetCore as Backend, and KnouckoutJs as Front, and Azure as Serverless Host.

Package, Deploy and Run the application - Azure CLI
========
 Go to  <https://azure.microsoft.com/en-us/free/>  to sign up to get $200 (for free) to explore any Azure service.

* Install the Azure CLI using PowerShell as administrator
```
Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'; rm .\AzureCLI.msi
```
 Reopen PowerShell to use the Azure CLI.
* Run the login command
```
az login
```
It will open and load an Azure sign-in page.
Sign in with your account credentials in the browser.

 * Get the current *master* version and navigate to the following directory: 
``` 
git clone https://github.com/MasoudAsadzade/Social-Networking-Serverless

cd Social-Networking-Serverless
``` 
* Run the following commands to install the required packages, run database migrations, and start the application on your local as your will.
``` 
dotnet tool install -g dotnet-ef
dotnet ef database update
dotnet run
``` 
Navigate to http://localhost:5000 in a browser.

# Create a SQL Database in Azure
 * Create a resource group
``` 
az group create --name myResourceGroup --location "West Europe"
``` 
* Create a SQL Database logical server
``` 
az sql server create --name <server-name> --resource-group myResourceGroup --location "West Europe" --admin-user <db-username> --admin-password <db-password>
``` 
* Configure a server firewall rule
``` 
az sql server firewall-rule create --resource-group myResourceGroup --server <server-name> --name AllowAzureIps --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
``` 
allow access from your local computer by replacing <your-ip-address> 
``` 
az sql server firewall-rule create --name AllowLocalClient --server <server-name> --resource-group myResourceGroup --start-ip-address=<your-ip-address> --end-ip-address=<your-ip-address>
``` 
* Create a database
``` 
az sql db show-connection-string --client ado.net --server <server-name> --name coreDB
``` 
In the command output, replace <username>, and <password> with the database administrator credentials you used earlier.
This is the connection string for your .NET Core app. 

* Configure app to connect to production database: In your local repository, open appsettings.json and find the following code:
``` 
"AzureDbSNS": "Data Source=sndbsqlserver.database.windows.net;Initial Catalog=SNDBSqlTest;User ID=*;Password=*;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"
``` 
and replace with your azure sql database connection string.

* Run database migrations to the production database. 
From the repository root, run the following commands. 
``` 
dotnet ef migrations add InitialCreate
dotnet ef database update
``` 
Now database migrations is run on the production database

# Deploy the Application to Azure
* Configure local git deployment
``` 
az webapp deployment user set --user-name <username> --password <password>
``` 
* Create a new App Service Plan, where <SKUCODE> sku may be F1 or FREE, etc
``` 
az appservice plan create --name myAppServicePlan --resource-group myResourceGroup --sku FREE --is-linux
``` 
* Create a web app in the myAppServicePlan App Service plan
``` 
az webapp create --resource-group myResourceGroup --plan myAppServicePlan --name <app-name> --runtime "DOTNETCORE|3.1" --deployment-local-git
``` 

* Set connection strings for your Azure app
``` 
az webapp config connection-string set --resource-group myResourceGroup --name <app-name> --settings MyDbConnection="<connection-string>" --connection-string-type SQLAzure
``` 
* Push to Azure from Git
``` 
git remote add azure <deploymentLocalGitUrl-from-create-step>
git push azure master
``` 
* Browse to the Azure app
``` 
http://<app-name>.azurewebsites.net
``` 
# Clean up resources
 If you don't expect to need these resources in the future, delete the resource group by running the following command in the Cloud Shell:
``` 
az group delete --name myResourceGroup
``` 
