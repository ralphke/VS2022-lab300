<p align="center">
<img src="img/banner.jpg" alt="decorative banner" width="1200"/>
</p>

# LAB300 - Hands-on with GitHub Copilot in Visual Studio 2022

This lab will guide you through using GitHub Copilot's various features in Visual Studio 2022. You'll start with a partially completed TinyShop application and use GitHub Copilot to complete missing features and enhance the application.

## Prerequisites

- Visual Studio 2022 with GitHub Copilot extension installed
- starting Visual Studio 2022 >= 17.13, GitHub Copilot is integrated with the VS Shell
- .NET 9 SDK
- GitHub account with Copilot subscription (including Free)
- make sure your nuget packages match the requiements by running the following commnads in the T.\src folder
- dotnet nuget locals all --clear
- dotnet restore
- This will make sure that your donet environment matches the project settings
- Next you need your browser to trust the development certificates by executing the following command
- dotnet dev-certs https --trust
- now all should be setup to work as expected on your computer
- To clean-up the environment, you can run the following command
- dotnet dev-certs https --clean
- This will remove all developer certificates from your machine


## Lab Overview

The TinyShop application consists of two main projects:
- A backend API built with .NET Minimal APIs
- A frontend Blazor Server application

You'll use GitHub Copilot's various features to enhance and complete this application.

## Lab Parts

0. [Setup](lab/setup.md)
1. [Exploring the Codebase with GitHub Copilot Chat](lab/part0-exploring-codebase.md)
2. [Code Completion with Ghost Text](lab/part1-code-completion.md)
3. [Enhancing UI with Inline Chat](lab/part2-enhancing-ui.md)
4. [Referencing Code Files in Chat](lab/part3-referencing-files.md)
5. [Using Custom Instructions](lab/part4-custom-instructions.md)
6. [Implementing Features with Copilot Agent](lab/part5-implementing-features.md)
7. [Using Copilot Vision](lab/part6-copilot-vision.md)
8. [Debugging with Copilot](lab/part7-debugging-with-copilot.md)
9. [Commit Summary Descriptions](lab/part8-commit-summary-descriptions.md)

**Key Takeaway**: These tools can significantly boost your productivity as a developer by automating repetitive tasks, generating boilerplate code, and helping you implement complex features more quickly.

## Session Resources 

| Resources          | Links                             | Description        |
|:-------------------|:----------------------------------|:-------------------|
| Build session page | https://build.microsoft.com/sessions/LAB300 | Event session page with downloadable recording, slides, resources, and speaker bio |
|Microsoft Learn|https://aka.ms/AAI_DevAppGitHubCop_Plan|Official Collection or Plan with skilling resources to learn at your own pace|
