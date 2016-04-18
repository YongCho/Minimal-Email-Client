# MinimalEmailClient
MinimalEmailClient is a simple Windows based email client that is designed to handle downloading, reading, writing, and sending emails using IMAP and SMTP protocols. <br /><br />
[UML Class Diagram](https://www.draw.io/#G0B4iaHoetmJUpMzhJY2VYc29uWkk)

# Contributor
- Yong Cho
- Yoo Min Cha

# Progress (Week 4/19)
- TODO: Implement replying and forwarding functionality.
- In-Progress: Conduct reliability test on all functionalities implemented so far.

# Progress (Week 04/12)
- We conducted research on possible ways to get notified on important IMAP server changes such as arrival of new messages, deletion of a message and message being marked as read/unread, so that we can automatically update our UI when such events occur. We found several IMAP extensions that could be used to achieve this:
    - [IMAP4-IDLE](https://tools.ietf.org/html/rfc2177)
    - [IMAP4-NOTIFY](https://tools.ietf.org/html/rfc5465)
    - [Push-IMAP](https://en.wikipedia.org/wiki/Push-IMAP)
- The problem was that different IMAP servers support different extensions and no single extension is supported by all the popular commercial IMAP servers. We decided not to implement any of the above extensions and instead implemented traditional 'periodic polling' method to achieve the similar result. This method has the disadvantage of wasting some network resources but works on all servers.
- Finished implementing file attachment to outgoing email.
- Allow sender to send to multiple recipients.
- TODO: Implement reply and forwarding email capabilities

# Progress (Week 04/05)
- Implemented handling of attached files in a received message. The program now scans the message's MIME body and extracts/displays all attached files (both binary and text attachments) when the user opens an email message. 
- Implemented UI to allow opening the attachment directly using the system's default associated application for the file type, or save them to the file system.
- Implemented UI to select files from the file system in order to attach them to an outgoing message.
- Researched possible ways to construct outgoing MIME message in order to allow file attachment. We have decided to use [MimeKit](https://github.com/jstedfast/MimeKit). It seems to be a little easier to use than [NI.Email.Mime](http://nugetmusthaves.com/Package/NI.Email.Mime) which is currently being used by the receiver side to parse incoming MIME message. We will continue using NI.Email.Mime on the receiver side for now since we have finished implementing it already and tested it functional. The sender side will use MimeKit.
- In-Progress: Implement mechanism to embed binary/text attachment to an outgoing email.

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
