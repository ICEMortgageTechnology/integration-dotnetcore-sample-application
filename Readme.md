## Encompass Partner Connect
#### Reference Implementation - Appraisal / Verification / Data and Docs
 
## Table of Contents
1. Introduction
2. System Requirements
3. Source Code Compilation
4. Configure ngrok
5. Configure the Visual Studio Solution
6. Create and Upload the Integration Zip File
7. Testing the Integration
 
## Introduction
This document explains how to register the Reference Integration with your Encompass Partner Connect (EPC) account, and to get started developing on the Partner API.
 
## System Requirements
- Microsoft Visual Studio 2017
- A production Encompass instance
- Loan Officer Connect
- An Encompass Partner Connect account
- Encompass Partner Connect API credentials
- (Optional) A paid ngrok account
 
## Source Code Compilation
Clone the Reference Integration repository from GitHub and open it in Visual Studio 2017.  Confirm that the project builds without errors.
 
## Configure ngrok
The EPC Partner API uses webhooks to notify the Partner that an order is ready to be processed.  In order to receive a webhook notification, a website must have a REST API endpoint listening on the public Internet.  However, most developers would not want their development workstation exposed publically on the Internet in order to test their code.
 
Ngrok is a utility that creates a public-facing URL that can accept webhook notifications, and forwards these notifications to a process running on the development workstation.  This can be done without exposing the development workstation to the public Internet.
 
Ngrok will create a new public-facing URL every time it is invoked, and this URL must be registered with Encompass Partner Connect in order to test the integration.  Ngrok (paid version) can use a static URL that doesn't change every time it is invoked.
 
Navigate to [ngrok.com] (https://ngrok.com/) and download the version of ngrok appropriate for your development environment.
 
To launch ngrok, unzip the contents, open a command prompt to the ngrok folder location, and execute:
```
ngrok http -host-header="localhost:65387" 65387
```
 
Ngrok will display a proxy URL, for example https://123456f23.ngrok.io/.  This URL will be required to register the Reference Integration with Encompass Partner Connect.
 
Ngrok has a Web Console that can be used to see active requests that are being proxied to the development workstation.  The Web Console can be accessed at [http://127.0.0.1:4040/inspect/http] (http://127.0.0.1:4040/inspect/http)
 
## Configure the Visual Studio Solution
 
TODO: Plug the EPC WebhookSecret, APIHost, ClientID and ClientSecret in the config file (appsettings.json) which is shared by Ellie Mae during onboarding.
 
## Create and Upload the Integration Zip File
 
In order to register the integration with Encompass Partner Connect, create a zip file containing the following two files
- A HTML file containing the User Interface
- A JSON file containing the configuration information
 
The HTML file can be found as part of the source code cloned from GitHub.  The configuration file should contain:
```
{ 
   "Configuration":{ 
      "Category":"CategoryName_1",
      "ProductName":"ProductName",
      "ConfigurationName":"Dev",
      "PartnerRestApiURL":"https://localhost:65387/",
      "WebhookURL":"https://123456f23.ngrok.io:65387/api/webhook"
   }
}
```
 
Once the two files (HTML and JSON) are zipped, upload the zip file to the Encompass Partner Connect Portal.
 
## Testing the Integration
 
In the Visual Studio Solution, start debugging, which runs the web site in IIS Express, running on port 65387.
 
Log into Loan Officer Connect (LO Connect) and navigate to Services. Find the newly registered integration under the category specified in the JSON configuration file, and launch it.