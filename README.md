# MinimalEmailClient
MinimalEmailClient is a simple Windows based email client that is designed to handle downloading, reading, writing, and sending emails using IMAP and SMTP protocols.

# Contributor
- Yong Cho
- Yoo Min Cha

# Progress (Week 03/29)
- Modified the message sync procedure to re-download and update the existing messages instead of downloading only new messages.
- Replaced database engine from SQLite to SQLCE due to a bug in SQLite driver that crashed XAML design mode.
- Fixed database error that was occuring when the user deleted an account from UI.
- Changed data model and model-view classes design so the views and models do not interact with each other - promotes modularity and maintainability.
- Reorganized project directory structure to accommodate different components and growing project.
- Greeting and Authorization of user to local SMTP server has been authenticated.
- Completed delivery notification to SMTP server and upon receiving acknowledgement, message may be sent out and retrieved by recipient server(s).
- Recipient client can now retrieve text-based emails that have been stored into their mailbox.

# Summary of progress up to Week 3/29
- Implemented some basic IMAP functionalities which include:
    * Logging in to an IMAP server with the credentials provided by the user.
    * Retrieving a list of mailboxes in the user's email account.
    * Downloading and displaying email messages in each mailbox.
- Created a database that stores the downloaded mailboxes and messages information.
- Implemented a series of logic that executes at the program startup:
    * Retrieve the list of mailboxes and messages from the database (local copy).
    * Connect to the configured IMAP server and download the mailbox list (server copy). Update the local mailbox list if there are differences.
    * For each mailbox, check the server for any newly arrived messages and download them as necessary.
    * Store all downloaded messages to the database.
- Implemented some basic SMTP functionalities which include:
    * Logging in to an SMTP server with the credentials provided by the user.
    * Authenticating Secure Socket Layer Connection with local server using email account credentials.
    * Preparing local SMTP server to accept upload of message from user to recipient mailbox.

# Setting up the environment.
- Install Visual Studio 2015 Community. Make sure you install the components to develop C# WPF Application. This might be installed by default. If they are not installed initially, you will have an option to install them later when you open the project.
- Double-click the .sln file to open the project.
