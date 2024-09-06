TornBot for Discord
Overview
TornBot is a powerful Discord bot tailored for Torn.com players, providing crucial functionalities such as war reports, authentication, verification, and progress tracking through intuitive slash commands.

Features
War Reports and Payments - Get real-time updates and automated payment distributions for faction wars directly in Discord.
Authentication - Securely link and authenticate your Torn.com account with your Discord profile.
Verification - Verify user accounts to ensure interactions are secured and authorized.
Progress Tracking - Monitor and report on your game progress including stats, income levels, and other significant metrics.
Requirements
Node.js 16.x or higher
Discord Bot Token
Torn API Key

Setup
Clone the Repository:

bash
Copy code
git clone https://github.com/yourgithub/tornbot-discord.git
cd tornbot-discord
Install Dependencies:

Copy code
npm install
Configure Environment: Create a .env file in the root directory and add your Discord Bot Token and Torn API Key:

makefile
Copy code
DISCORD_BOT_TOKEN=your-discord-bot-token
TORN_API_KEY=your-torn-api-key
Deploy Commands: Set up the slash commands for deployment:

Copy code
node deploy-commands.js
Start the Bot: Launch your bot:

Copy code
node index.js
Usage
Use the following slash commands to interact with the bot on your Discord server:

/war_report - Display the latest war report and payment information.
/authenticate - Link your Torn.com account to your Discord user.
/verify - Verify your identity with your linked Torn.com account.
/track_progress - Check your or your faction's progress in Torn.com.
Contribution
Contributions to the TornBot are always welcome. Please read the CONTRIBUTING.md for how to contribute effectively.

Support
If you encounter any issues or need assistance, please open an issue on the GitHub repository or contact support via Discord.
