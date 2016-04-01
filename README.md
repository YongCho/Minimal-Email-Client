# MinimalEmailClient
MinimalEmailClient is a simple Windows based email client that is designed to handle downloading, reading, writing, and sending emails using IMAP and SMTP protocols.

# Contributor
- Yong Cho
- Yoomin Cha

# Progress (Week 03/29)
- Modified the message sync procedure to download and update the existing messages instead of downloading only new messages.
- Replaced database engine from SQLite to SQLCE due to a bug in SQLite driver that crashed XAML design mode.
- Fixed database error that was occuring when the user deleted an account from UI.
- Changed data model and model-view classes design so the views and models do not interact with each other - promotes modularity and maintainability.
- Reorganized project directory structure to accommodate different components and growing project.

# Summary of progress up to Week 3/29
- Implemented some basic IMAP functionalities which include:
    * Logging in to an IMAP server.
    * Retrieving a list of mailboxes in the user's email account.
    * Downloading and displaying email messages in each mailbox.
- Created a database that stores the downloaded mailboxes and messages information.
- Implemented a series of logic that executes at the program startup:
    * Retrieve the list of mailboxes and messages from the database (local copy).
    * Connect to the configured IMAP server and download mailbox list.
    * For each mailbox, check the server for any new messages and download them as necessary.
    * Store all newly downloaded information to the database.
- Created UI elements that collect an email account information from the user and display the mailbox list and email messages.

# Setting up the environment.
- Install Visual Studio 2015 Community. Make sure you install the components to develop C# WPF Application. This might be installed by default. If they are not installed initially, you will have an option to install them later when you open the project.
- Double-click the .sln file to open the project.
