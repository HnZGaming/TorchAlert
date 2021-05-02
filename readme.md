# Torch Alert

Rust's "Raid Alert" but in Space Engineers (and Discord).

Discord bot will send DM to offline players whenever their grids are in danger. 

![user alerts](docs/user_alerts.png)

## Setup (Discord bot)

1. Make an application & a bot on [Discord Developer Portal](https://discord.com/developers/applications)
1. Get the bot token (used later)
1. Enable the bot's presence intent & server members intent
1. Invite the bot to your Discord server
1. Give it a role & show it on the right pane

## Bot Icon

![bot icon](docs/icon.png)

woo woo

## Setup (Admin)

1. Grab the binary in [Torch plugin repository](https://torchapi.net/plugins/item/5a486edf-d677-4c5d-a4d7-9015dd9fb20b)
1. Install the plugin like usual
1. Assign the bot token (from earlier) to the plugin config; **no restart required**
1. Do some config tweaking if you want to

## Setup (User)

![user setup](docs/user_setup.png)

1. In game, type `!alert link`, or as an admin `!alert link <your steam id>`
1. Remember the link ID (1-5 digits number) that the plugin will spit out in response to the command
1. Mention or DM the bot with the link ID

## Troubleshooting (Admin):

* `!alert configs` to view and/or edit all configs
* `!alert commands` to view all commands

## Troubleshooting (User)

* To validate your Discord/Steam link, type `!alert check` in game or say `check` to the bot on Discord.
* To link again (for whatever reason) you can simply `!alert link` again.
* To turn on/off the alert for a player, say "mute" or "unmute" to the bot on Discord, or `!mute` or `!unmute` in game.

## Bug Fix & Feature Requests

Ping @ryo in #plugins on Torch Discord

## Contribution

I love your PR
