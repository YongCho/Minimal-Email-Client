# MinimalEmailClient
MinimalEmailClient is a simple Windows based email client that is designed to handle downloading, reading, writing, and sending emails using IMAP and SMTP protocols.

<img src="https://drive.google.com/uc?id=0B4iaHoetmJUpZnlQMk1KRFRKSkk" />

# Contributor
- Yong Cho
- Yoomin Cha

# Progress (Week 03/29)
- Yong
    * Modified the message sync procedure to download and update the existing messages instead of downloading only new messages.
    * Fixed error during an account deletion.
    * Refactored data model and model-view classes so the views and models do not interact with each other - promotes modularity and maintainability.
    * Reorganized project directory structure to accommodate different components and growing project.

# Setting up the environment.
- Install Visual Studio 2015 Community. Make sure you install the components to develop C# WPF Application. This might be installed by default. If they are not installed initially, you will have an option to install them later when you open the project.
- Double-click the .sln file to open the project.
