# RustStorm Wipe Webhook

A premium-style Rust plugin for automated Discord wipe announcements, RustMaps-powered map voting, and next-wipe community map selection.

Built for Oxide/uMod Rust servers.

---

# Features

## Automated Wipe Detection

* Detects fresh procedural map wipes automatically
* Posts premium Discord wipe announcements
* Supports weekly and force wipe schedules
* Countdown timers using Discord timestamps

## RustMaps Integration

* Automatic RustMaps procedural map generation
* RustMaps API support
* Configurable map generation timeout
* Automatic polling while maps generate
* Supports generated map previews and images

## Discord Map Voting

* Automatic post-wipe map voting
* Reaction-based Discord voting
* Auto-generated map options
* Configurable map size and seed ranges
* Automatic winner detection
* Winner announcements with RustMaps previews

## RustMaps Image Support

Configurable RustMaps image types:

* auto
* icons
* preview
* thumbnail
* raw

## Persistent Vote Tracking

* Survives server restarts
* Resumes active votes automatically
* Stores winning map data
* Prevents duplicate vote generation

---

# Requirements

* Rust Dedicated Server
* Oxide/uMod
* Discord Bot Token
* Discord Webhook
* RustMaps API Key

---

# Installation

1. Upload `RustStormWipeWebhook.cs` to:

```text
oxide/plugins/
```

2. Start or reload the plugin:

```text
oxide.reload RustStormWipeWebhook
```

3. Configure:

```text
oxide/config/RustStormWipeWebhook.json
```

---

# Discord Bot Permissions

Required permissions:

* View Channels
* Send Messages
* Embed Links
* Add Reactions
* Read Message History

---

# Commands

## Wipe Commands

```text
wipewebhook.test
```

Posts a test wipe announcement.

```text
wipewebhook.check
```

Checks for a fresh wipe manually.

---

## Map Vote Commands

```text
wipewebhook.mapvote.start
```

Starts a map vote manually.

```text
wipewebhook.mapvote.status
```

Shows active vote information.

```text
wipewebhook.mapvote.end
```

Ends the active vote and announces the winner.

---

# RustMaps API Setup

Get your API key from:

https://rustmaps.com/docs

Add it to config:

```json
"RustMaps API Settings": {
  "Use RustMaps API Generation Before Posting Vote": true,
  "API Key": "YOUR_API_KEY"
}
```

---

# Recommended Settings

```json
"Vote Duration Minutes": 4320,
"Map Generation Timeout Seconds": 3600,
"Map Generation Poll Interval Seconds": 60,
"Preferred RustMaps Image Type (auto, icons, preview, thumbnail, raw)": "icons"
```

---

# Example Features In Action

## Automatic Flow

```text
Fresh wipe detected
↓
Discord wipe announcement
↓
Automatic map vote starts
↓
RustMaps generates maps
↓
Players vote in Discord
↓
Winner selected automatically
↓
Winner announcement posted
```

---

# Configuration Highlights

## Automatic Vote Generation

```json
"Automatically Start New Vote After Fresh Wipe": true
```

## Auto Generated Maps

```json
"Auto Generate Map Options On Vote Start": true
```

## Map Size

```json
"Auto Generated Map Size": 4000
```

## RustMaps Image Style

```json
"Preferred RustMaps Image Type (auto, icons, preview, thumbnail, raw)": "icons"
```

---

# Performance

Designed to have very low runtime overhead.

The plugin remains mostly idle outside of:

* wipe detection
* Discord requests
* RustMaps generation polling

Recommended polling interval:

```json
"Map Generation Poll Interval Seconds": 60
```

---

# Notes

* RustMaps generation can take time depending on map size and RustMaps queue load
* The plugin supports long generation waits (up to 2 hours configurable)
* Shockbyte-compatible
* Supports procedural Rust maps

---

# Credits

Created by Milestorme

Powered by:

* RustMaps
* Discord
* Oxide/uMod
