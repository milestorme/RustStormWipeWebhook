using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RustStormWipeWebhook", "Milestorme", "1.3.3")]
    [Description("Detects fresh map wipes automatically and posts a premium RustStorm Discord webhook update with the next wipe countdown.")]
    public class RustStormWipeWebhook : RustPlugin
    {
        private Configuration config;
        private StoredData data;

        private const string DataFileName = "RustStormWipeWebhook";

        private class Configuration
        {
            [JsonProperty("Discord Webhook URL")]
            public string DiscordWebhookUrl = "";

            [JsonProperty("Server Name")]
            public string ServerName = "RustStorm AU";

            [JsonProperty("Server Description")]
            public string ServerDescription = "5x | Solo/Duo/Trio | Weekly";

                        [JsonProperty("Post Once On Server Initialization")]
            public bool PostOnceOnServerInitialization = true;

            [JsonProperty("Wipe Day")]
            public string WipeDay = "Friday";

            [JsonProperty("Wipe Hour 24")]
            public int WipeHour24 = 3;

            [JsonProperty("Wipe Minute")]
            public int WipeMinute = 0;

            [JsonProperty("Timezone Label")]
            public string TimezoneLabel = "GMT+8";

            [JsonProperty("Timezone Offset Hours")]
            public int TimezoneOffsetHours = 8;

            [JsonProperty("Branding")]
            public BrandingSettings Branding = new BrandingSettings();

            [JsonProperty("Discord Message Settings")]
            public MessageSettings Message = new MessageSettings();
        }

        private class BrandingSettings
        {
            [JsonProperty("Banner Image URL")]
            public string BannerImageUrl = "https://i.ibb.co/9HGbDcWT/Chat-GPT-Image-Mar-31-2026-08-24-03-PM.png";

            [JsonProperty("Thumbnail Image URL")]
            public string ThumbnailImageUrl = "https://i.ibb.co/9HGbDcWT/Chat-GPT-Image-Mar-31-2026-08-24-03-PM.png";

            [JsonProperty("Embed Color Decimal")]
            public int EmbedColorDecimal = 15882260;
        }

        private class MessageSettings
        {
            [JsonProperty("Username Override")]
            public string Username = "RustStorm Wipe Bot";

            [JsonProperty("Avatar URL")]
            public string AvatarUrl = "";

            [JsonProperty("Title")]
            public string Title = "🔥 RustStorm Wipe Updated";

            [JsonProperty("Description")]
            public string Description = "A fresh map wipe has been detected and the next RustStorm wipe countdown is now live.";

            [JsonProperty("Footer")]
            public string Footer = "RustStorm AU";

            [JsonProperty("Show @everyone On Real Wipes")]
            public bool ShowEveryoneOnRealWipes = false;

            [JsonProperty("Show @everyone On Test Messages")]
            public bool ShowEveryoneOnTestMessages = false;
        }

        private class StoredData
        {
            public string LastAnnouncedMapId = "";
            public string LastNextWipeIso = "";
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    throw new Exception("Config was empty.");
            }
            catch
            {
                PrintWarning("Config invalid, generating a new one.");
                config = new Configuration();
            }

            SanitizeConfig();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        private void Init()
        {
            LoadData();
        }

        private void OnServerInitialized()
        {
            if (!config.PostOnceOnServerInitialization)
                return;

            TryAnnounceFreshWipe("server initialization");
        }

        [ConsoleCommand("wipewebhook.test")]
        private void WipeWebhookTest(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            SendWebhook("manual test", true);
            SendReply(arg, "RustStormWipeWebhook test sent.");
        }

        [ConsoleCommand("wipewebhook.check")]
        private void WipeWebhookCheck(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            bool sent = TryAnnounceFreshWipe("manual check");
            SendReply(arg, sent
                ? "Fresh map wipe detected and webhook sent."
                : "No new map wipe detected. Current map was already announced.");
        }

        private void SanitizeConfig()
        {
            if (config == null)
                config = new Configuration();

            if (config.Branding == null)
                config.Branding = new BrandingSettings();

            if (config.Message == null)
                config.Message = new MessageSettings();

            config.WipeHour24 = Mathf.Clamp(config.WipeHour24, 0, 23);
            config.WipeMinute = Mathf.Clamp(config.WipeMinute, 0, 59);

            if (string.IsNullOrWhiteSpace(config.ServerName))
                config.ServerName = "RustStorm AU";

            if (string.IsNullOrWhiteSpace(config.ServerDescription))
                config.ServerDescription = "5x | Solo/Duo/Trio | Weekly";

                        if (string.IsNullOrWhiteSpace(config.WipeDay))
                config.WipeDay = "Friday";

            if (string.IsNullOrWhiteSpace(config.TimezoneLabel))
                config.TimezoneLabel = "GMT+8";

            if (string.IsNullOrWhiteSpace(config.Message.Title))
                config.Message.Title = "🔥 RustStorm Wipe Updated";

            if (string.IsNullOrWhiteSpace(config.Message.Description))
                config.Message.Description = "A fresh map wipe has been detected and the next RustStorm wipe countdown is now live.";

            if (string.IsNullOrWhiteSpace(config.Message.Footer))
                config.Message.Footer = "RustStorm AU";

            if (config.Branding.EmbedColorDecimal <= 0)
                config.Branding.EmbedColorDecimal = 15882260;
        }

        private void LoadData()
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(DataFileName);
            }
            catch
            {
                data = new StoredData();
            }

            if (data == null)
                data = new StoredData();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(DataFileName, data);
        }

        private bool TryAnnounceFreshWipe(string reason)
        {
            string currentMapId = GetCurrentMapId();

            if (string.IsNullOrWhiteSpace(currentMapId))
            {
                PrintWarning("Could not determine current map identity yet. Wipe webhook was skipped.");
                return false;
            }

            if (data.LastAnnouncedMapId == currentMapId)
                return false;

            if (SendWebhook(reason, false))
            {
                data.LastAnnouncedMapId = currentMapId;
                data.LastNextWipeIso = GetNextWipeLocal().ToString("o");
                SaveData();
                return true;
            }

            return false;
        }

        private string GetCurrentMapId()
        {
            try
            {
                string url = World.Url ?? string.Empty;
                string seed = World.Seed.ToString();
                string size = World.Size.ToString();

                if (!string.IsNullOrWhiteSpace(url))
                    return $"url:{url}".Trim();

                if (World.Seed > 0 && World.Size > 0)
                    return $"seed:{seed}|size:{size}";
            }
            catch
            {
            }

            return string.Empty;
        }

        private bool SendWebhook(string reason, bool isTest)
        {
            if (string.IsNullOrWhiteSpace(config.DiscordWebhookUrl))
            {
                PrintWarning("Discord Webhook URL is empty. Set it in the config first.");
                return false;
            }

            DateTimeOffset nextWipeLocal = GetNextWipeLocal();
            long unix = nextWipeLocal.ToUnixTimeSeconds();

            string content = BuildContentPrefix(isTest);
            var embed = BuildEmbed(reason, unix, isTest);

            var payload = new DiscordWebhookPayload
            {
                username = config.Message.Username,
                avatar_url = string.IsNullOrWhiteSpace(config.Message.AvatarUrl) ? null : config.Message.AvatarUrl,
                content = content,
                embeds = new List<DiscordEmbed> { embed }
            };

            string json = JsonConvert.SerializeObject(payload);

            webrequest.Enqueue(
                config.DiscordWebhookUrl,
                json,
                (code, response) =>
                {
                    if (code == 200 || code == 204)
                        Puts($"Discord wipe webhook sent successfully for map {GetCurrentMapId()}.");
                    else
                        PrintWarning($"Discord wipe webhook failed. HTTP {code}. Response: {response}");
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            );

            return true;
        }

        private string BuildContentPrefix(bool isTest)
        {
            var parts = new List<string>();

            if (isTest)
            {
                if (config.Message.ShowEveryoneOnTestMessages)
                    parts.Add("@everyone");

                parts.Add("🧪 Test message");
            }
            else if (config.Message.ShowEveryoneOnRealWipes)
            {
                parts.Add("@everyone");
            }

            return string.Join(" ", parts.ToArray());
        }


        private bool IsForceWipe(DateTimeOffset wipeTime)
        {
            return wipeTime.Day <= 7 && wipeTime.DayOfWeek == DayOfWeek.Friday;
        }

        private DiscordEmbed BuildEmbed(string reason, long unix, bool isTest)
        {
            string headline = isTest ? "🧪 Test message" : "🌩 Fresh map wipe detected";
            string schedule = $"{config.WipeDay} • {config.WipeHour24:00}:{config.WipeMinute:00} {config.TimezoneLabel}";
            string nextWipeCompact = $"<t:{unix}:R>";
            string nextWipeFull = $"<t:{unix}:F>";
            string wipeType = isTest ? "Test Trigger" : (IsForceWipe(GetNextWipeLocal()) ? "Force Wipe (Facepunch)" : "Weekly Wipe");

            return new DiscordEmbed
            {
                title = isTest ? "🧪 RustStorm Wipe Test" : (IsForceWipe(GetNextWipeLocal()) ? "🔥 RustStorm Force Wipe" : "🔥 RustStorm Weekly Wipe"),
                description =
                    $"**{config.ServerName}** | {config.ServerDescription}\n\n" +
                    $"{headline}\n" +
                    $"⏱ **Next Wipe:** {nextWipeCompact}\n" +
                    $"📅 **Wipe Time:** {nextWipeFull}\n\n" +
                    $"⚡ Fresh start. No delays. No surprises.",
                color = config.Branding.EmbedColorDecimal,
                fields = new List<DiscordField>
                {
                    new DiscordField
                    {
                        name = "⚔️ Wipe Type",
                        value = wipeType,
                        inline = true
                    },
                    new DiscordField
                    {
                        name = "🗓 Schedule",
                        value = schedule,
                        inline = true
                    },
                },
                footer = new DiscordFooter
                {
                    text = isTest
                        ? $"{config.Message.Footer} • Test Webhook"
                        : $"{config.Message.Footer} • Wipe Webhook"
                },
                timestamp = DateTime.UtcNow.ToString("o"),
                image = string.IsNullOrWhiteSpace(config.Branding.BannerImageUrl) ? null : new DiscordImage { url = config.Branding.BannerImageUrl },
                thumbnail = string.IsNullOrWhiteSpace(config.Branding.ThumbnailImageUrl) ? null : new DiscordThumbnail { url = config.Branding.ThumbnailImageUrl }
            };
        }

        private DateTimeOffset GetNextWipeLocal()
        {
            var offset = TimeSpan.FromHours(config.TimezoneOffsetHours);
            var nowLocal = DateTimeOffset.UtcNow.ToOffset(offset);

            DayOfWeek wipeDay;
            if (!Enum.TryParse(config.WipeDay, true, out wipeDay))
                wipeDay = DayOfWeek.Friday;

            int daysUntil = ((int)wipeDay - (int)nowLocal.DayOfWeek + 7) % 7;

            var next = new DateTimeOffset(
                nowLocal.Year,
                nowLocal.Month,
                nowLocal.Day,
                config.WipeHour24,
                config.WipeMinute,
                0,
                offset
            ).AddDays(daysUntil);

            if (daysUntil == 0 && nowLocal >= next)
                next = next.AddDays(7);

            return next;
        }

        private class DiscordWebhookPayload
        {
            public string username;
            public string avatar_url;
            public string content;
            public List<DiscordEmbed> embeds;
        }

        private class DiscordEmbed
        {
            public string title;
            public string description;
            public int color;
            public List<DiscordField> fields;
            public DiscordFooter footer;
            public string timestamp;
            public DiscordImage image;
            public DiscordThumbnail thumbnail;
        }

        private class DiscordField
        {
            public string name;
            public string value;
            public bool inline;
        }

        private class DiscordFooter
        {
            public string text;
        }

        private class DiscordImage
        {
            public string url;
        }

        private class DiscordThumbnail
        {
            public string url;
        }
    }
}
