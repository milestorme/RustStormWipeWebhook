# 🌩 RustStorm Wipe Webhook

A premium Oxide plugin that automatically detects Rust map wipes and posts a clean, branded Discord webhook announcement with a live countdown.

---

## 🔥 Features

* ✅ **Automatic wipe detection**

  * Detects new maps using `World.Url` (primary) or fallback identity
  * No manual input required

* ⏱ **Live Discord countdown**

  * Uses Discord timestamps (`<t:...>`)
  * Shows both relative and exact wipe time

* 🧠 **Force vs Weekly wipe detection**

  * First Friday of the month → **Force Wipe (Facepunch)**
  * All other Fridays → **Weekly Wipe**

* 🎨 **Fully branded embed**

  * Banner image
  * Thumbnail
  * Custom webhook avatar
  * RustStorm color styling

* 🧼 **Clean, minimal layout**

  * No clutter (no debug info, no filler sections)
  * Focused on key info only

* 🔁 **Posts once per wipe**

  * Prevents duplicate spam on server restarts

* 📣 **Optional @everyone ping**

---

## ⚙️ Installation

1. Drop the plugin into your server:

```
/oxide/plugins/RustStormWipeWebhook.cs
```

2. Start or reload server:

```
oxide.reload RustStormWipeWebhook
```

3. Edit config file:

```
/oxide/config/RustStormWipeWebhook.json
```

---

## 🔧 Configuration

### Required

```json
"Discord Webhook URL": "https://discord.com/api/webhooks/..."
```

---

### Branding

```json
"Branding": {
  "Banner Image URL": "https://your-banner.png",
  "Thumbnail Image URL": "https://your-thumbnail.png",
  "Embed Color Decimal": 15882260
}
```

---

### Webhook Identity

```json
"Username Override": "RustStorm Wipe Bot",
"Avatar URL": "https://your-logo.png"
```

---

### Wipe Schedule

```json
"Wipe Day": "Friday",
"Wipe Hour 24": 3,
"Wipe Minute": 0,
"Timezone Label": "GMT+8",
"Timezone Offset Hours": 8
```

---

### Behavior

```json
"Post Once On Server Initialization": true,
"Show @everyone On Real Wipes": true,
"Show @everyone On Test Messages": false
```

---

## 🧪 Commands

### Test webhook

```
wipewebhook.test
```

### Force wipe check

```
wipewebhook.check
```

---

## 📩 Example Output

**Title:**

```
🔥 RustStorm Force Wipe
```

**Content:**

* Server name + type
* Next wipe countdown (live)
* Exact wipe time
* Wipe type (Force / Weekly)
* Schedule
* Banner image

---

## 🧠 How It Works

* Tracks current map using:

  * `World.Url` (preferred)
  * fallback: seed + size internally
* Stores last announced map in:

```
/oxide/data/RustStormWipeWebhook.json
```

* Only posts when a **new map is detected**

---

## 🚀 Notes

* No seed/technical info is shown to players (clean UX)
* Designed for **Discord-first servers**
* Safe across restarts and reloads

---

## 🔥 Recommended Setup

* Use a **square logo (256x256)** for Avatar
* Use your **banner (wide)** for embed image
* Keep embed clean — less is more

---

## 💡 Future Ideas

* Role ping instead of @everyone
* Different embed color for force wipe
* Auto “JUST WIPED — JOIN NOW” message

---

## 🌩 RustStorm

> Fresh wipe. No delays. No surprises.
