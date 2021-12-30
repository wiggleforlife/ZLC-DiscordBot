# ZLC-DiscordBot

The ZLC Bot will make automated posts to this channel notifying ZLC Observers, Specialist, and Neighbors of any new ATC that have signed on.

The bot will check to see who is online once every 5min.

If there has been no change from the previous check, nothing will happen.

If there has been changes from the previous check and it has been over 15min from the previous new post, the bot will delete the previous post and make a new one; sending a notification out.

If there has been changes from the previous check and it has NOT been over 15min from the previous new post, the bot will edit the previous post and NOT make a new post; not sending a notification out.
