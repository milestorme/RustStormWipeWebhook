using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RustStorm Wipe Webhook", "Milestorme", "1.6.7")]
    [Description("Detects fresh map wipes, posts Discord wipe updates, and supports RustMaps map voting with generated RustMaps images.")]
    public class RustStormWipeWebhook : RustPlugin
    {
        private Configuration config;
        private StoredData data;
        private bool mapVoteStartInProgress;

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

            [JsonProperty("Map Info Settings")]
            public MapInfoSettings MapInfo = new MapInfoSettings();

            [JsonProperty("Map Vote Settings")]
            public MapVoteSettings MapVote = new MapVoteSettings();

            [JsonProperty("RustMaps API Settings")]
            public RustMapsApiSettings RustMapsApi = new RustMapsApiSettings();
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

        private class MapInfoSettings
        {
            [JsonProperty("Include Map Size And Seed")]
            public bool IncludeMapSizeAndSeed = true;

            [JsonProperty("Include RustMaps Link")]
            public bool IncludeRustMapsLink = true;

            [JsonProperty("RustMaps Base URL")]
            public string RustMapsBaseUrl = "https://rustmaps.com/map";
        }

        private class RustMapsApiSettings
        {
            [JsonProperty("Use RustMaps API Generation Before Posting Vote")]
            public bool UseApiGenerationBeforePostingVote = false;

            [JsonProperty("API Key")]
            public string ApiKey = "";

            [JsonProperty("API Base URL")]
            public string ApiBaseUrl = "https://api.rustmaps.com";

            [JsonProperty("Procedural Map Generate Endpoint Path")]
            public string ProceduralGenerateEndpointPath = "/v4/maps";

            [JsonProperty("Generate Request Method (POST or GET)")]
            public string GenerateRequestMethod = "POST";

            [JsonProperty("Procedural Map Lookup Endpoint Path")]
            public string ProceduralLookupEndpointPath = "/v4/maps/{size}/{seed}";

            [JsonProperty("Map Generation Poll Interval Seconds")]
            public int MapGenerationPollIntervalSeconds = 60;

            [JsonProperty("Require RustMaps Generated Link Before Posting Vote")]
            public bool RequireGeneratedLinkBeforePostingVote = true;

            [JsonProperty("Map Generation Timeout Seconds")]
            public int MapGenerationTimeoutSeconds = 3600;

            [JsonProperty("Fallback To Normal RustMaps Link If API Fails Or Times Out")]
            public bool FallbackToNormalRustMapsLink = true;

            [JsonProperty("Use Generated RustMaps Image In Vote Embed")]
            public bool UseGeneratedRustMapsImageInVoteEmbed = true;

            [JsonProperty("Preferred RustMaps Image Type (auto, icons, preview, thumbnail, raw)")]
            public string PreferredRustMapsImageType = "icons";
        }

        private class MapVoteSettings
        {
            [JsonProperty("Enabled")]
            public bool Enabled = true;

            [JsonProperty("Discord Bot Token (required for reaction counting)")]
            public string DiscordBotToken = "";

            [JsonProperty("Discord Channel ID")]
            public string DiscordChannelId = "";

            [JsonProperty("Vote Duration Minutes")]
            public int VoteDurationMinutes = 1440;

            [JsonProperty("Mention Everyone When Vote Starts")]
            public bool MentionEveryoneWhenVoteStarts = false;

            [JsonProperty("Map Vote Title")]
            public string VoteTitle = "🗺 RustStorm Next Map Vote";

            [JsonProperty("Map Vote Description")]
            public string VoteDescription = "Vote for the next wipe map by reacting below. The winning seed will be announced when voting ends.";

            [JsonProperty("Automatically Announce Winner When Vote Ends")]
            public bool AutoAnnounceWinnerWhenVoteEnds = true;

            [JsonProperty("Automatically Start New Vote After Fresh Wipe")]
            public bool AutomaticallyStartNewVoteAfterFreshWipe = true;

            [JsonProperty("Delay Before Auto Starting Vote Minutes")]
            public int DelayBeforeAutoStartingVoteMinutes = 15;

            [JsonProperty("Skip Auto Start If A Vote Is Already Active")]
            public bool SkipAutoStartIfAVoteIsAlreadyActive = true;

            [JsonProperty("Auto Generate Map Options On Vote Start")]
            public bool AutoGenerateMapOptionsOnVoteStart = true;

            [JsonProperty("Auto Generated Map Option Count")]
            public int AutoGeneratedMapOptionCount = 3;

            [JsonProperty("Auto Generated Map Size")]
            public int AutoGeneratedMapSize = 4500;

            [JsonProperty("Auto Generated Seed Minimum")]
            public int AutoGeneratedSeedMinimum = 1;

            [JsonProperty("Auto Generated Seed Maximum")]
            public int AutoGeneratedSeedMaximum = 2147483647;

            [JsonProperty("Avoid Current Server Seed When Auto Generating")]
            public bool AvoidCurrentServerSeedWhenAutoGenerating = true;

            [JsonProperty("Map Options", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<MapVoteOption> Options = new List<MapVoteOption>();
        }

        private class MapVoteOption
        {
            [JsonProperty("Name")]
            public string Name = "Map Option";

            [JsonProperty("Size")]
            public int Size = 4500;

            [JsonProperty("Seed")]
            public int Seed = 1;

            [JsonProperty("Image URL Override")]
            public string ImageUrlOverride = "";

            [JsonProperty("Generated RustMaps URL")]
            public string GeneratedRustMapsUrl = "";

            [JsonProperty("Generated RustMaps Image URL")]
            public string GeneratedRustMapsImageUrl = "";
        }

        private class StoredData
        {
            public string LastAnnouncedMapId = "";
            public string LastNextWipeIso = "";
            public ActiveMapVote ActiveVote = null;
            public MapVoteResult LastVoteWinner = null;
        }

        private class ActiveMapVote
        {
            public string ChannelId = "";
            public string MessageId = "";
            public string EndsAtIso = "";
            public List<MapVoteOption> Options = new List<MapVoteOption>();
        }

        private class MapVoteResult
        {
            public string Name = "";
            public int Size;
            public int Seed;
            public int Votes;
            public string RustMapsUrl = "";
            public string RustMapsImageUrl = "";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command.",
                ["TestSent"] = "RustStorm Wipe Webhook test sent.",
                ["FreshWipeDetected"] = "Fresh map wipe detected and webhook sent.",
                ["NoFreshWipeDetected"] = "No new map wipe detected. The current map was already announced.",
                ["MapVoteDisabled"] = "Map voting is disabled in the config.",
                ["MapVoteNoOptions"] = "No valid map vote options are configured.",
                ["MapVoteMissingBot"] = "Map vote requires Discord Bot Token and Discord Channel ID in the config.",
                ["MapVoteStarted"] = "Map vote started in Discord.",
                ["MapVoteStartFailed"] = "Map vote failed to start. Check server console for the Discord error.",
                ["MapVoteNoActive"] = "There is no active map vote.",
                ["MapVoteEnded"] = "Map vote ended and winner was announced.",
                ["MapVoteEndRequested"] = "Map vote result check requested. Watch console/Discord for the result."
            }, this);
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
            ScheduleExistingMapVoteEnd();

            if (!config.PostOnceOnServerInitialization)
                return;

            TryAnnounceFreshWipe("server initialization");
        }

        [ConsoleCommand("wipewebhook.test")]
        private void WipeWebhookTest(ConsoleSystem.Arg arg)
        {
            if (!HasCommandAccess(arg))
            {
                Reply(arg, "NoPermission");
                return;
            }

            SendWebhook("manual test", true);
            Reply(arg, "TestSent");
        }

        [ConsoleCommand("wipewebhook.check")]
        private void WipeWebhookCheck(ConsoleSystem.Arg arg)
        {
            if (!HasCommandAccess(arg))
            {
                Reply(arg, "NoPermission");
                return;
            }

            bool sent = TryAnnounceFreshWipe("manual check");
            Reply(arg, sent ? "FreshWipeDetected" : "NoFreshWipeDetected");
        }

        [ConsoleCommand("wipewebhook.mapvote.start")]
        private void MapVoteStartCommand(ConsoleSystem.Arg arg)
        {
            if (!HasCommandAccess(arg))
            {
                Reply(arg, "NoPermission");
                return;
            }

            if (!config.MapVote.Enabled)
            {
                Reply(arg, "MapVoteDisabled");
                return;
            }

            if (!HasDiscordBotSettings())
            {
                Reply(arg, "MapVoteMissingBot");
                return;
            }

            List<MapVoteOption> options = GetValidVoteOptions();
            if (options.Count == 0)
            {
                Reply(arg, "MapVoteNoOptions");
                return;
            }

            StartMapVote(options, success => Reply(arg, success ? "MapVoteStarted" : "MapVoteStartFailed"));
        }

        [ConsoleCommand("wipewebhook.mapvote.end")]
        private void MapVoteEndCommand(ConsoleSystem.Arg arg)
        {
            if (!HasCommandAccess(arg))
            {
                Reply(arg, "NoPermission");
                return;
            }

            if (data.ActiveVote == null || string.IsNullOrWhiteSpace(data.ActiveVote.MessageId))
            {
                Reply(arg, "MapVoteNoActive");
                return;
            }

            EndMapVote(true);
            Reply(arg, "MapVoteEndRequested");
        }

        [ConsoleCommand("wipewebhook.mapvote.status")]
        private void MapVoteStatusCommand(ConsoleSystem.Arg arg)
        {
            if (!HasCommandAccess(arg))
            {
                Reply(arg, "NoPermission");
                return;
            }

            if (data.ActiveVote == null || string.IsNullOrWhiteSpace(data.ActiveVote.MessageId))
            {
                Reply(arg, "MapVoteNoActive");
                return;
            }

            ReplyRaw(arg, $"Active map vote message: {data.ActiveVote.MessageId}. Ends: {data.ActiveVote.EndsAtIso}");
        }

        private bool HasCommandAccess(ConsoleSystem.Arg arg)
        {
            if (arg == null)
                return false;

            if (arg.Connection == null)
                return true;

            return arg.IsAdmin;
        }

        private void Reply(ConsoleSystem.Arg arg, string messageKey)
        {
            SendReply(arg, GetMessage(messageKey, arg));
        }

        private string GetMessage(string messageKey, ConsoleSystem.Arg arg = null)
        {
            string userId = arg?.Connection?.userid.ToString() ?? null;
            return lang.GetMessage(messageKey, this, userId);
        }

        private void SanitizeConfig()
        {
            if (config == null)
                config = new Configuration();

            if (config.Branding == null)
                config.Branding = new BrandingSettings();

            if (config.Message == null)
                config.Message = new MessageSettings();

            if (config.MapInfo == null)
                config.MapInfo = new MapInfoSettings();

            if (config.MapVote == null)
                config.MapVote = new MapVoteSettings();

            if (config.RustMapsApi == null)
                config.RustMapsApi = new RustMapsApiSettings();

            if (config.MapVote.Options == null)
                config.MapVote.Options = new List<MapVoteOption>();

            CleanMapVoteOptions();

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

            if (string.IsNullOrWhiteSpace(config.MapInfo.RustMapsBaseUrl))
                config.MapInfo.RustMapsBaseUrl = "https://rustmaps.com/map";

            config.MapVote.VoteDurationMinutes = Mathf.Clamp(config.MapVote.VoteDurationMinutes, 1, 10080);
            config.MapVote.DelayBeforeAutoStartingVoteMinutes = Mathf.Clamp(config.MapVote.DelayBeforeAutoStartingVoteMinutes, 0, 1440);
            config.MapVote.AutoGeneratedMapOptionCount = Mathf.Clamp(config.MapVote.AutoGeneratedMapOptionCount, 1, VoteEmojis.Length);
            config.MapVote.AutoGeneratedMapSize = Mathf.Clamp(config.MapVote.AutoGeneratedMapSize, 1000, 6000);
            config.MapVote.AutoGeneratedSeedMinimum = Mathf.Clamp(config.MapVote.AutoGeneratedSeedMinimum, 1, 2147483647);
            config.MapVote.AutoGeneratedSeedMaximum = Mathf.Clamp(config.MapVote.AutoGeneratedSeedMaximum, 1, 2147483647);
            if (config.MapVote.AutoGeneratedSeedMaximum < config.MapVote.AutoGeneratedSeedMinimum)
                config.MapVote.AutoGeneratedSeedMaximum = config.MapVote.AutoGeneratedSeedMinimum;
            if (string.IsNullOrWhiteSpace(config.MapVote.VoteTitle))
                config.MapVote.VoteTitle = "🗺 RustStorm Next Map Vote";
            if (string.IsNullOrWhiteSpace(config.MapVote.VoteDescription))
                config.MapVote.VoteDescription = "Vote for the next wipe map by reacting below. The winning seed will be announced when voting ends.";

            if (string.IsNullOrWhiteSpace(config.RustMapsApi.ApiBaseUrl))
                config.RustMapsApi.ApiBaseUrl = "https://api.rustmaps.com";

            // v1.5.2 migration: old builds accidentally used the lookup endpoint as the generation endpoint.
            // The lookup endpoint returns HTTP 404 "Map not found" for new seeds, so move default configs to POST /v4/maps.
            if (string.Equals((config.RustMapsApi.ProceduralGenerateEndpointPath ?? string.Empty).Trim(), "/v4/maps/{size}/{seed}", StringComparison.OrdinalIgnoreCase))
                config.RustMapsApi.ProceduralGenerateEndpointPath = "/v4/maps";
            if (string.Equals((config.RustMapsApi.GenerateRequestMethod ?? string.Empty).Trim(), "GET", StringComparison.OrdinalIgnoreCase))
                config.RustMapsApi.GenerateRequestMethod = "POST";

            if (string.IsNullOrWhiteSpace(config.RustMapsApi.ProceduralGenerateEndpointPath))
                config.RustMapsApi.ProceduralGenerateEndpointPath = "/v4/maps";
            if (string.IsNullOrWhiteSpace(config.RustMapsApi.GenerateRequestMethod))
                config.RustMapsApi.GenerateRequestMethod = "POST";
            if (string.IsNullOrWhiteSpace(config.RustMapsApi.ProceduralLookupEndpointPath))
                config.RustMapsApi.ProceduralLookupEndpointPath = "/v4/maps/{size}/{seed}";
            config.RustMapsApi.MapGenerationPollIntervalSeconds = Mathf.Clamp(config.RustMapsApi.MapGenerationPollIntervalSeconds, 15, 600);
            config.RustMapsApi.MapGenerationTimeoutSeconds = Mathf.Clamp(config.RustMapsApi.MapGenerationTimeoutSeconds, 30, 7200);

            string preferredImageType = (config.RustMapsApi.PreferredRustMapsImageType ?? "icons").Trim().ToLowerInvariant();
            if (preferredImageType != "auto" && preferredImageType != "icons" && preferredImageType != "preview" && preferredImageType != "thumbnail" && preferredImageType != "raw")
                preferredImageType = "icons";
            config.RustMapsApi.PreferredRustMapsImageType = preferredImageType;
        }

        private void CleanMapVoteOptions()
        {
            if (config?.MapVote?.Options == null)
                return;

            var cleaned = new List<MapVoteOption>();
            var seen = new HashSet<string>();

            foreach (var option in config.MapVote.Options)
            {
                if (option == null || option.Size <= 0 || option.Seed <= 0)
                    continue;

                string key = option.Size + "_" + option.Seed;
                if (seen.Contains(key))
                    continue;

                seen.Add(key);
                cleaned.Add(option);
            }

            config.MapVote.Options = cleaned;
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
                QueueAutoMapVoteAfterFreshWipe();
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
                    {
                        Puts($"Discord wipe webhook sent successfully for map {GetCurrentMapId()}.");
                        SendCurrentMapLinkWebhookMessage();
                    }
                    else
                        PrintWarning($"Discord wipe webhook failed. HTTP {code}. Response: {response}");
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            );

            return true;
        }

        private void SendCurrentMapLinkWebhookMessage()
        {
            if (string.IsNullOrWhiteSpace(config.DiscordWebhookUrl) || !config.MapInfo.IncludeRustMapsLink || World.Seed <= 0 || World.Size <= 0)
                return;

            string mapUrl = BuildRustMapsUrl(World.Size.ToString(), World.Seed.ToString());
            if (string.IsNullOrWhiteSpace(mapUrl))
                return;

            var payload = new DiscordWebhookPayload
            {
                username = config.Message.Username,
                avatar_url = string.IsNullOrWhiteSpace(config.Message.AvatarUrl) ? null : config.Message.AvatarUrl,
                content = $"🗺 **New map:** {mapUrl}",
                embeds = null
            };

            webrequest.Enqueue(
                config.DiscordWebhookUrl,
                JsonConvert.SerializeObject(payload),
                (code, response) =>
                {
                    if (code == 200 || code == 204)
                        Puts($"Discord new map link posted: {mapUrl}");
                    else
                        PrintWarning($"Discord new map link post failed. HTTP {code}. Response: {response}");
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                new Dictionary<string, string> { ["Content-Type"] = "application/json" }
            );
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

        private DateTimeOffset GetCurrentWipeLocal()
        {
            var offset = TimeSpan.FromHours(config.TimezoneOffsetHours);
            var nowLocal = DateTimeOffset.UtcNow.ToOffset(offset);

            DayOfWeek wipeDay;
            if (!Enum.TryParse(config.WipeDay, true, out wipeDay))
                wipeDay = DayOfWeek.Friday;

            int daysSince = ((int)nowLocal.DayOfWeek - (int)wipeDay + 7) % 7;

            var current = new DateTimeOffset(
                nowLocal.Year,
                nowLocal.Month,
                nowLocal.Day,
                config.WipeHour24,
                config.WipeMinute,
                0,
                offset
            ).AddDays(-daysSince);

            if (daysSince == 0 && nowLocal < current)
                current = current.AddDays(-7);

            return current;
        }

        private DiscordEmbed BuildEmbed(string reason, long unix, bool isTest)
        {
            string headline = isTest ? "🧪 Test message" : "🌩 Fresh map wipe detected";
            string schedule = $"{config.WipeDay} • {config.WipeHour24:00}:{config.WipeMinute:00} {config.TimezoneLabel}";
            string nextWipeCompact = $"<t:{unix}:R>";
            string nextWipeFull = $"<t:{unix}:F>";
            var currentWipe = GetCurrentWipeLocal();
            string wipeType = isTest ? "Test Trigger" : (IsForceWipe(currentWipe) ? "Force Wipe (Facepunch)" : "Weekly Wipe");
            string embedTitle = isTest ? "🧪 RustStorm Wipe Test" : (IsForceWipe(currentWipe) ? "🔥 RustStorm Force Wipe" : "🔥 RustStorm Weekly Wipe");
            string mapInfoBlock = BuildMapInfoBlock();

            return new DiscordEmbed
            {
                title = embedTitle,
                description =
                    $"**{config.ServerName}** | {config.ServerDescription}\n\n" +
                    $"{headline}\n" +
                    $"⏱ **Next Wipe:** {nextWipeCompact}\n" +
                    $"📅 **Wipe Time:** {nextWipeFull}\n\n" +
                    $"⚡ Fresh start. No delays. No surprises." +
                    mapInfoBlock,
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

        private string BuildMapInfoBlock()
        {
            if ((!config.MapInfo.IncludeMapSizeAndSeed && !config.MapInfo.IncludeRustMapsLink) || World.Seed <= 0 || World.Size <= 0)
                return string.Empty;

            string size = World.Size.ToString();
            string seed = World.Seed.ToString();

            var lines = new List<string>();

            if (config.MapInfo.IncludeMapSizeAndSeed)
            {
                lines.Add($"🗺 **Map Size:** {size}");
                lines.Add($"🌱 **Map Seed:** {seed}");
            }

            if (config.MapInfo.IncludeRustMapsLink)
            {
                string url = BuildRustMapsUrl(size, seed);
                if (!string.IsNullOrWhiteSpace(url))
                    lines.Add($"🔗 **Map:** {url}");
            }

            if (lines.Count == 0)
                return string.Empty;

            return "\n\n" + string.Join("\n", lines.ToArray());
        }


        private void QueueAutoMapVoteAfterFreshWipe()
        {
            if (config?.MapVote == null || !config.MapVote.Enabled || !config.MapVote.AutomaticallyStartNewVoteAfterFreshWipe)
                return;

            if (config.MapVote.SkipAutoStartIfAVoteIsAlreadyActive && data?.ActiveVote != null && !string.IsNullOrWhiteSpace(data.ActiveVote.MessageId))
            {
                Puts("Fresh wipe detected, but auto map vote start was skipped because a vote is already active.");
                return;
            }

            if (!HasDiscordBotSettings())
            {
                PrintWarning("Fresh wipe detected, but auto map vote start was skipped because Discord Bot Token or Channel ID is missing.");
                return;
            }

            int delayMinutes = Mathf.Clamp(config.MapVote.DelayBeforeAutoStartingVoteMinutes, 0, 1440);
            Puts($"Fresh wipe detected. Auto map vote will start in {delayMinutes} minute(s).");
            timer.Once(delayMinutes * 60f, () =>
            {
                if (!config.MapVote.Enabled)
                    return;

                if (config.MapVote.SkipAutoStartIfAVoteIsAlreadyActive && data?.ActiveVote != null && !string.IsNullOrWhiteSpace(data.ActiveVote.MessageId))
                    return;

                List<MapVoteOption> options = GetValidVoteOptions();
                if (options.Count == 0)
                {
                    PrintWarning("Auto map vote could not start because no valid/generated map options were available.");
                    return;
                }

                StartMapVote(options, success =>
                {
                    if (success)
                        Puts("Auto map vote started after fresh wipe.");
                    else
                        PrintWarning("Auto map vote failed to start after fresh wipe.");
                });
            });
        }

        private void ScheduleExistingMapVoteEnd()
        {
            if (data == null || data.ActiveVote == null || string.IsNullOrWhiteSpace(data.ActiveVote.EndsAtIso))
                return;

            DateTimeOffset endsAt;
            if (!DateTimeOffset.TryParse(data.ActiveVote.EndsAtIso, out endsAt))
                return;

            double seconds = (endsAt - DateTimeOffset.UtcNow).TotalSeconds;
            if (seconds <= 0)
            {
                timer.Once(10f, () => EndMapVote(false));
                return;
            }

            timer.Once((float)Math.Min(seconds, int.MaxValue), () => EndMapVote(false));
        }

        private bool HasDiscordBotSettings()
        {
            return !string.IsNullOrWhiteSpace(config.MapVote.DiscordBotToken) && !string.IsNullOrWhiteSpace(config.MapVote.DiscordChannelId);
        }

        private List<MapVoteOption> GetValidVoteOptions()
        {
            if (config.MapVote != null && config.MapVote.AutoGenerateMapOptionsOnVoteStart)
                return GenerateMapVoteOptions();

            var result = new List<MapVoteOption>();
            if (config.MapVote == null || config.MapVote.Options == null)
                return result;

            for (int i = 0; i < config.MapVote.Options.Count && i < VoteEmojis.Length; i++)
            {
                MapVoteOption option = config.MapVote.Options[i];
                if (option == null || option.Size <= 0 || option.Seed <= 0)
                    continue;

                if (string.IsNullOrWhiteSpace(option.Name))
                    option.Name = $"Map Option {i + 1}";

                result.Add(CloneMapVoteOption(option));
            }

            return result;
        }

        private List<MapVoteOption> GenerateMapVoteOptions()
        {
            var result = new List<MapVoteOption>();
            int count = Mathf.Clamp(config.MapVote.AutoGeneratedMapOptionCount, 1, VoteEmojis.Length);
            int size = Mathf.Clamp(config.MapVote.AutoGeneratedMapSize, 1000, 6000);
            int minSeed = Mathf.Clamp(config.MapVote.AutoGeneratedSeedMinimum, 1, 2147483647);
            int maxSeed = Mathf.Clamp(config.MapVote.AutoGeneratedSeedMaximum, minSeed, 2147483647);
            int currentSeed = 0;

            try
            {
                if (World.Seed > 0 && World.Seed <= 2147483647u)
                    currentSeed = (int)World.Seed;
            }
            catch { }

            var usedSeeds = new HashSet<int>();
            var random = new System.Random();
            int attempts = 0;
            int maxAttempts = count * 50;

            while (result.Count < count && attempts < maxAttempts)
            {
                attempts++;
                int seed = random.Next(minSeed, maxSeed == int.MaxValue ? int.MaxValue : maxSeed + 1);

                if (usedSeeds.Contains(seed))
                    continue;

                if (config.MapVote.AvoidCurrentServerSeedWhenAutoGenerating && currentSeed > 0 && seed == currentSeed)
                    continue;

                usedSeeds.Add(seed);
                result.Add(new MapVoteOption
                {
                    Name = $"Map Option {result.Count + 1}",
                    Size = size,
                    Seed = seed
                });
            }

            return result;
        }

        private MapVoteOption CloneMapVoteOption(MapVoteOption source)
        {
            return new MapVoteOption
            {
                Name = source.Name,
                Size = source.Size,
                Seed = source.Seed,
                ImageUrlOverride = source.ImageUrlOverride,
                GeneratedRustMapsUrl = source.GeneratedRustMapsUrl,
                GeneratedRustMapsImageUrl = source.GeneratedRustMapsImageUrl
            };
        }

        private static readonly string[] VoteEmojis =
        {
            "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟"
        };

        private void ReplyRaw(ConsoleSystem.Arg arg, string message)
        {
            SendReply(arg, message);
        }

        private void StartMapVote(List<MapVoteOption> options, Action<bool> callback)
        {
            if (mapVoteStartInProgress)
            {
                PrintWarning("Map vote start ignored because another map vote start/generation is already in progress.");
                callback(false);
                return;
            }

            if (config.MapVote.SkipAutoStartIfAVoteIsAlreadyActive && data?.ActiveVote != null && !string.IsNullOrWhiteSpace(data.ActiveVote.MessageId))
            {
                PrintWarning("Map vote start ignored because a vote is already active.");
                callback(false);
                return;
            }

            mapVoteStartInProgress = true;
            PrepareRustMapsGeneratedLinks(options, preparedOptions => PostMapVote(preparedOptions, success =>
            {
                mapVoteStartInProgress = false;
                callback(success);
            }));
        }

        private void PostMapVote(List<MapVoteOption> options, Action<bool> callback)
        {
            DateTimeOffset endsAt = DateTimeOffset.UtcNow.AddMinutes(config.MapVote.VoteDurationMinutes);
            long endUnix = endsAt.ToUnixTimeSeconds();

            var embeds = new List<DiscordEmbed>
            {
                new DiscordEmbed
                {
                    title = config.MapVote.VoteTitle,
                    description = $"{config.MapVote.VoteDescription}\n\n⏱ **Voting Ends:** <t:{endUnix}:R> • <t:{endUnix}:F>",
                    color = config.Branding.EmbedColorDecimal,
                    footer = new DiscordFooter { text = $"{config.Message.Footer} • Map Vote" },
                    timestamp = DateTime.UtcNow.ToString("o"),
                    thumbnail = string.IsNullOrWhiteSpace(config.Branding.ThumbnailImageUrl) ? null : new DiscordThumbnail { url = config.Branding.ThumbnailImageUrl }
                }
            };

            for (int i = 0; i < options.Count; i++)
            {
                MapVoteOption option = options[i];
                string url = !string.IsNullOrWhiteSpace(option.GeneratedRustMapsUrl)
                    ? option.GeneratedRustMapsUrl
                    : BuildRustMapsUrl(option.Size.ToString(), option.Seed.ToString());

                embeds.Add(new DiscordEmbed
                {
                    title = $"{VoteEmojis[i]} {option.Name}",
                    description = $"**Size:** `{option.Size}` • **Seed:** `{option.Seed}`\n{url}",
                    color = config.Branding.EmbedColorDecimal,
                    image = GetVoteOptionImage(option),
                    footer = new DiscordFooter { text = "React with the matching emoji to vote for this map" }
                });
            }

            var payload = new DiscordBotMessagePayload
            {
                content = config.MapVote.MentionEveryoneWhenVoteStarts ? "@everyone" : "",
                embeds = embeds
            };

            string urlPost = $"https://discord.com/api/v10/channels/{config.MapVote.DiscordChannelId}/messages";
            SendDiscordBotRequest(urlPost, JsonConvert.SerializeObject(payload), Oxide.Core.Libraries.RequestMethod.POST, (code, response) =>
            {
                if (code < 200 || code >= 300)
                {
                    PrintWarning($"Failed to create Discord map vote. HTTP {code}. Response: {response}");
                    callback(false);
                    return;
                }

                string messageId = ExtractJsonString(response, "id");
                if (string.IsNullOrWhiteSpace(messageId))
                {
                    PrintWarning($"Discord map vote was posted but message id was not found. Response: {response}");
                    callback(false);
                    return;
                }

                data.ActiveVote = new ActiveMapVote
                {
                    ChannelId = config.MapVote.DiscordChannelId,
                    MessageId = messageId,
                    EndsAtIso = endsAt.ToString("o"),
                    Options = options
                };
                SaveData();

                AddVoteReactions(0);
                timer.Once(config.MapVote.VoteDurationMinutes * 60f, () => EndMapVote(false));
                callback(true);
            });
        }

        private void PrepareRustMapsGeneratedLinks(List<MapVoteOption> options, Action<List<MapVoteOption>> callback)
        {
            if (options == null || options.Count == 0)
            {
                callback(options ?? new List<MapVoteOption>());
                return;
            }

            if (config.RustMapsApi == null || !config.RustMapsApi.UseApiGenerationBeforePostingVote)
            {
                callback(options);
                return;
            }

            if (string.IsNullOrWhiteSpace(config.RustMapsApi.ApiKey))
            {
                PrintWarning("RustMaps API generation is enabled, but API Key is empty. Posting normal RustMaps links instead.");
                callback(options);
                return;
            }

            int pending = options.Count;
            bool completed = false;
            int timeoutSeconds = Mathf.Clamp(config.RustMapsApi.MapGenerationTimeoutSeconds, 30, 7200);

            Puts($"RustMaps API generation enabled. Waiting up to {timeoutSeconds} second(s) for {options.Count} map option(s).");

            timer.Once(timeoutSeconds, () =>
            {
                if (completed)
                    return;

                completed = true;
                PrintWarning($"RustMaps API map generation timed out after {timeoutSeconds} second(s). Posting vote with any generated links received so far.");
                callback(options);
            });

            for (int i = 0; i < options.Count; i++)
                RequestRustMapsGeneration(options[i], () =>
                {
                    pending--;
                    if (pending > 0 || completed)
                        return;

                    completed = true;
                    callback(options);
                });
        }

        private void RequestRustMapsGeneration(MapVoteOption option, Action done)
        {
            string endpoint = BuildRustMapsApiEndpoint(option.Size, option.Seed, true);
            string methodText = (config.RustMapsApi.GenerateRequestMethod ?? "POST").Trim().ToUpperInvariant();
            var method = methodText == "POST" ? Oxide.Core.Libraries.RequestMethod.POST : Oxide.Core.Libraries.RequestMethod.GET;
            string body = method == Oxide.Core.Libraries.RequestMethod.POST
                ? JsonConvert.SerializeObject(new Dictionary<string, object> { ["size"] = option.Size, ["seed"] = option.Seed })
                : string.Empty;

            var headers = BuildRustMapsHeaders(method == Oxide.Core.Libraries.RequestMethod.POST);

            webrequest.Enqueue(endpoint, body, (code, response) =>
            {
                if (code >= 200 && code < 300)
                {
                    string generatedUrl = ExtractRustMapsUrlFromResponse(response);
                    if (!string.IsNullOrWhiteSpace(generatedUrl))
                    {
                        option.GeneratedRustMapsUrl = generatedUrl;
                        option.GeneratedRustMapsImageUrl = ExtractRustMapsImageUrlFromResponse(response);
                        if (!string.IsNullOrWhiteSpace(option.GeneratedRustMapsImageUrl))
                            Puts($"RustMaps image prepared for {option.Size}_{option.Seed}: {option.GeneratedRustMapsImageUrl}");
                        Puts($"RustMaps prepared {option.Size}_{option.Seed}: {option.GeneratedRustMapsUrl}");
                        done();
                        return;
                    }

                    if (!config.RustMapsApi.RequireGeneratedLinkBeforePostingVote)
                    {
                        option.GeneratedRustMapsUrl = BuildRustMapsUrl(option.Size.ToString(), option.Seed.ToString());
                        Puts($"RustMaps accepted generation request for {option.Size}_{option.Seed}; posting fallback link because waiting is disabled.");
                        done();
                        return;
                    }

                    Puts($"RustMaps accepted generation request for {option.Size}_{option.Seed}. Waiting for generated map to become available...");
                    PollRustMapsGeneratedMap(option, done);
                    return;
                }

                PrintWarning($"RustMaps API generation failed for {option.Size}_{option.Seed}. HTTP {code}. Response: {response}");
                if (config.RustMapsApi.FallbackToNormalRustMapsLink && !config.RustMapsApi.RequireGeneratedLinkBeforePostingVote)
                    option.GeneratedRustMapsUrl = BuildRustMapsUrl(option.Size.ToString(), option.Seed.ToString());
                done();
            }, this, method, headers);
        }

        private void PollRustMapsGeneratedMap(MapVoteOption option, Action done)
        {
            int pollInterval = Mathf.Clamp(config.RustMapsApi.MapGenerationPollIntervalSeconds, 15, 600);
            string endpoint = BuildRustMapsApiEndpoint(option.Size, option.Seed, false);
            var headers = BuildRustMapsHeaders(false);

            timer.Once(pollInterval, () =>
            {
                webrequest.Enqueue(endpoint, string.Empty, (code, response) =>
                {
                    if (code >= 200 && code < 300)
                    {
                        string generatedUrl = ExtractRustMapsUrlFromResponse(response);
                        if (string.IsNullOrWhiteSpace(generatedUrl))
                            generatedUrl = BuildRustMapsUrl(option.Size.ToString(), option.Seed.ToString());

                        option.GeneratedRustMapsUrl = generatedUrl;
                        option.GeneratedRustMapsImageUrl = ExtractRustMapsImageUrlFromResponse(response);
                        if (!string.IsNullOrWhiteSpace(option.GeneratedRustMapsImageUrl))
                            Puts($"RustMaps image ready for {option.Size}_{option.Seed}: {option.GeneratedRustMapsImageUrl}");
                        Puts($"RustMaps generated map is ready for {option.Size}_{option.Seed}: {option.GeneratedRustMapsUrl}");
                        done();
                        return;
                    }

                    if (code == 404 || code == 409)
                    {
                        string waitReason = code == 409 ? "is still generating" : "is not ready yet";
                        Puts($"RustMaps map {option.Size}_{option.Seed} {waitReason}. Checking again in {pollInterval} second(s).");
                        PollRustMapsGeneratedMap(option, done);
                        return;
                    }

                    PrintWarning($"RustMaps lookup failed while waiting for {option.Size}_{option.Seed}. HTTP {code}. Response: {response}");
                    if (config.RustMapsApi.FallbackToNormalRustMapsLink && !config.RustMapsApi.RequireGeneratedLinkBeforePostingVote)
                    {
                        option.GeneratedRustMapsUrl = BuildRustMapsUrl(option.Size.ToString(), option.Seed.ToString());
                        done();
                        return;
                    }

                    // When generated links are required, do not release the vote early on transient API errors.
                    // Keep polling until the map is ready or the global generation timeout fires.
                    PrintWarning($"RustMaps map {option.Size}_{option.Seed} was not ready due to HTTP {code}; generated links are required, so polling will continue.");
                    PollRustMapsGeneratedMap(option, done);
                }, this, Oxide.Core.Libraries.RequestMethod.GET, headers);
            });
        }

        private Dictionary<string, string> BuildRustMapsHeaders(bool includeContentType)
        {
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {config.RustMapsApi.ApiKey}",
                ["X-API-Key"] = config.RustMapsApi.ApiKey,
                ["Accept"] = "application/json",
                ["User-Agent"] = "RustStormWipeWebhook/1.6.2"
            };

            if (includeContentType)
                headers["Content-Type"] = "application/json";

            return headers;
        }

        private string BuildRustMapsApiEndpoint(int size, int seed, bool generationEndpoint)
        {
            string baseUrl = (config.RustMapsApi.ApiBaseUrl ?? "https://api.rustmaps.com").Trim().TrimEnd('/');
            string path = generationEndpoint
                ? (config.RustMapsApi.ProceduralGenerateEndpointPath ?? "/v4/maps").Trim()
                : (config.RustMapsApi.ProceduralLookupEndpointPath ?? "/v4/maps/{size}/{seed}").Trim();
            if (!path.StartsWith("/"))
                path = "/" + path;

            path = path.Replace("{size}", size.ToString()).Replace("{seed}", seed.ToString());
            return baseUrl + path;
        }

        private string ExtractRustMapsUrlFromResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            try
            {
                JToken root = JToken.Parse(response);
                string[] fields =
                {
                    "url", "mapUrl", "map_url", "rustmapsUrl", "rustmaps_url", "link",
                    "data.url", "data.mapUrl", "data.map_url", "data.rustmapsUrl", "data.rustmaps_url", "data.link"
                };

                for (int i = 0; i < fields.Length; i++)
                {
                    JToken token = root.SelectToken(fields[i]);
                    string value = token?.ToString();
                    if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        return value;
                }
            }
            catch (Exception ex)
            {
                PrintWarning($"Failed to parse RustMaps API response: {ex.Message}");
            }

            return string.Empty;
        }

        private class ImageUrlCandidate
        {
            public string Url = "";
            public string Path = "";
        }

        private string ExtractRustMapsImageUrlFromResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            try
            {
                JToken root = JToken.Parse(response);
                var candidates = new List<ImageUrlCandidate>();

                // Collect every image-looking URL first, then choose the best one.
                // Older builds returned the first image URL found, which often selected map_raw_normalized.png.
                // This version prefers RustMaps icon/monument/label renders when the API response includes them.
                CollectImageUrlsRecursive(root, "", candidates);

                string selected = ChoosePreferredRustMapsImageUrl(candidates);
                if (!string.IsNullOrWhiteSpace(selected))
                    return selected;
            }
            catch (Exception ex)
            {
                PrintWarning($"Failed to parse RustMaps image URL from API response: {ex.Message}");
            }

            return string.Empty;
        }

        private void CollectImageUrlsRecursive(JToken token, string path, List<ImageUrlCandidate> candidates)
        {
            if (token == null || candidates == null)
                return;

            if (token.Type == JTokenType.String)
            {
                string value = token.ToString();
                if (LooksLikeImageUrl(value))
                {
                    candidates.Add(new ImageUrlCandidate
                    {
                        Url = value,
                        Path = path ?? string.Empty
                    });
                }
                return;
            }

            JObject obj = token as JObject;
            if (obj != null)
            {
                foreach (JProperty property in obj.Properties())
                {
                    string childPath = string.IsNullOrWhiteSpace(path) ? property.Name : path + "." + property.Name;
                    CollectImageUrlsRecursive(property.Value, childPath, candidates);
                }
                return;
            }

            JArray array = token as JArray;
            if (array != null)
            {
                for (int i = 0; i < array.Count; i++)
                    CollectImageUrlsRecursive(array[i], path + "[" + i + "]", candidates);
                return;
            }

            foreach (JToken child in token.Children())
                CollectImageUrlsRecursive(child, path, candidates);
        }

        private string ChoosePreferredRustMapsImageUrl(List<ImageUrlCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return string.Empty;

            string preferred = config?.RustMapsApi?.PreferredRustMapsImageType ?? "icons";
            int bestScore = int.MinValue;
            string bestUrl = string.Empty;

            foreach (ImageUrlCandidate candidate in candidates)
            {
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.Url))
                    continue;

                int score = ScoreRustMapsImageCandidate(candidate.Url, candidate.Path, preferred);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestUrl = candidate.Url;
                }
            }

            return bestUrl;
        }

        private int ScoreRustMapsImageCandidate(string url, string path, string preferred)
        {
            string combined = ((path ?? string.Empty) + " " + (url ?? string.Empty)).ToLowerInvariant();
            string pref = (preferred ?? "icons").Trim().ToLowerInvariant();
            if (pref != "auto" && pref != "icons" && pref != "preview" && pref != "thumbnail" && pref != "raw")
                pref = "icons";

            int score = 0;

            if (combined.Contains(".png") || combined.Contains(".jpg") || combined.Contains(".jpeg") || combined.Contains(".webp"))
                score += 50;

            bool isIcons = combined.Contains("icon") || combined.Contains("icons") || combined.Contains("monument") || combined.Contains("marker") || combined.Contains("markers") || combined.Contains("label") || combined.Contains("labeled") || combined.Contains("labelled");
            bool isPreview = combined.Contains("preview") || combined.Contains("render") || combined.Contains("normal");
            bool isThumbnail = combined.Contains("thumbnail") || combined.Contains("thumb");
            bool isRaw = combined.Contains("raw") || combined.Contains("map_raw");
            bool isMap = combined.Contains("map");

            if (pref == "icons")
            {
                if (isIcons) score += 1200;
                if (isPreview) score += 500;
                if (isThumbnail) score += 300;
                if (isRaw) score -= 250;
            }
            else if (pref == "preview")
            {
                if (isPreview) score += 1200;
                if (isIcons) score += 700;
                if (isThumbnail) score += 300;
                if (isRaw) score -= 150;
            }
            else if (pref == "thumbnail")
            {
                if (isThumbnail) score += 1200;
                if (isIcons) score += 600;
                if (isPreview) score += 500;
                if (isRaw) score -= 100;
            }
            else if (pref == "raw")
            {
                if (isRaw) score += 1200;
                if (combined.Contains("normalized")) score += 500;
                if (isPreview) score += 200;
                if (isIcons) score -= 150;
            }
            else // auto
            {
                // Best looking Discord order: icons/monument map, then preview, then thumbnail, then raw terrain.
                if (isIcons) score += 1200;
                if (isPreview) score += 800;
                if (isThumbnail) score += 400;
                if (isRaw) score -= 150;
            }

            if (combined.Contains("normalized"))
                score += pref == "raw" ? 300 : 80;
            if (isMap)
                score += 100;

            return score;
        }

        private bool LooksLikeImageUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return false;

            string lower = value.ToLowerInvariant();
            return lower.Contains(".png") || lower.Contains(".jpg") || lower.Contains(".jpeg") || lower.Contains(".webp") || lower.Contains("image") || lower.Contains("thumbnail") || lower.Contains("preview") || lower.Contains("icon");
        }

        private DiscordImage GetVoteOptionImage(MapVoteOption option)
        {
            if (option == null)
                return null;

            if (!string.IsNullOrWhiteSpace(option.ImageUrlOverride))
                return new DiscordImage { url = option.ImageUrlOverride };

            if (config.RustMapsApi != null && config.RustMapsApi.UseGeneratedRustMapsImageInVoteEmbed && !string.IsNullOrWhiteSpace(option.GeneratedRustMapsImageUrl))
                return new DiscordImage { url = option.GeneratedRustMapsImageUrl };

            return null;
        }

        private void AddVoteReactions(int index)
        {
            if (data.ActiveVote == null || data.ActiveVote.Options == null || index >= data.ActiveVote.Options.Count || index >= VoteEmojis.Length)
                return;

            string emoji = Uri.EscapeDataString(VoteEmojis[index]);
            string url = $"https://discord.com/api/v10/channels/{data.ActiveVote.ChannelId}/messages/{data.ActiveVote.MessageId}/reactions/{emoji}/@me";

            SendDiscordBotRequest(url, "", Oxide.Core.Libraries.RequestMethod.PUT, (code, response) =>
            {
                if (code < 200 || code >= 300)
                    PrintWarning($"Failed to add vote reaction {VoteEmojis[index]}. HTTP {code}. Response: {response}");

                timer.Once(0.35f, () => AddVoteReactions(index + 1));
            });
        }

        private void EndMapVote(bool manual)
        {
            if (data.ActiveVote == null || string.IsNullOrWhiteSpace(data.ActiveVote.MessageId))
                return;

            string channelId = data.ActiveVote.ChannelId;
            string messageId = data.ActiveVote.MessageId;
            string url = $"https://discord.com/api/v10/channels/{channelId}/messages/{messageId}";

            SendDiscordBotRequest(url, null, Oxide.Core.Libraries.RequestMethod.GET, (code, response) =>
            {
                if (code < 200 || code >= 300)
                {
                    PrintWarning($"Failed to fetch map vote results. HTTP {code}. Response: {response}");
                    return;
                }

                MapVoteResult winner = CalculateWinnerFromMessage(response);
                if (winner == null)
                {
                    PrintWarning("Map vote ended but no winner could be calculated.");
                    return;
                }

                data.LastVoteWinner = winner;
                data.ActiveVote = null;
                SaveData();

                Puts($"Map vote winner: {winner.Name} | Size {winner.Size} | Seed {winner.Seed} | Votes {winner.Votes}");

                if (config.MapVote.AutoAnnounceWinnerWhenVoteEnds || manual)
                    AnnounceMapVoteWinner(winner);
            });
        }

        private MapVoteResult CalculateWinnerFromMessage(string response)
        {
            if (data.ActiveVote == null || data.ActiveVote.Options == null || data.ActiveVote.Options.Count == 0)
                return null;

            int[] counts = new int[data.ActiveVote.Options.Count];

            try
            {
                JObject root = JObject.Parse(response);
                JArray reactions = root["reactions"] as JArray;
                if (reactions != null)
                {
                    foreach (JToken reaction in reactions)
                    {
                        string name = reaction["emoji"]?["name"]?.ToString();
                        int count = reaction["count"] != null ? reaction["count"].Value<int>() : 0;

                        for (int i = 0; i < data.ActiveVote.Options.Count && i < VoteEmojis.Length; i++)
                        {
                            if (name == VoteEmojis[i])
                            {
                                // Subtract the bot's own reaction so only player/community votes are counted.
                                counts[i] = Math.Max(0, count - 1);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintWarning($"Failed to parse Discord vote result JSON: {ex.Message}");
                return null;
            }

            int bestIndex = 0;
            int bestVotes = counts[0];
            for (int i = 1; i < counts.Length; i++)
            {
                if (counts[i] > bestVotes)
                {
                    bestVotes = counts[i];
                    bestIndex = i;
                }
            }

            MapVoteOption selected = data.ActiveVote.Options[bestIndex];
            return new MapVoteResult
            {
                Name = selected.Name,
                Size = selected.Size,
                Seed = selected.Seed,
                Votes = bestVotes,
                RustMapsUrl = !string.IsNullOrWhiteSpace(selected.GeneratedRustMapsUrl) ? selected.GeneratedRustMapsUrl : BuildRustMapsUrl(selected.Size.ToString(), selected.Seed.ToString()),
                RustMapsImageUrl = selected.GeneratedRustMapsImageUrl
            };
        }

        private void AnnounceMapVoteWinner(MapVoteResult winner)
        {
            if (winner == null)
                return;

            if (string.IsNullOrWhiteSpace(winner.RustMapsImageUrl) && config.RustMapsApi != null && config.RustMapsApi.UseGeneratedRustMapsImageInVoteEmbed && !string.IsNullOrWhiteSpace(config.RustMapsApi.ApiKey))
            {
                FetchRustMapsImageForWinner(winner, () => SendMapVoteWinnerEmbed(winner));
                return;
            }

            SendMapVoteWinnerEmbed(winner);
        }

        private void FetchRustMapsImageForWinner(MapVoteResult winner, Action callback)
        {
            string endpoint = BuildRustMapsApiEndpoint(winner.Size, winner.Seed, false);
            var headers = BuildRustMapsHeaders(false);

            webrequest.Enqueue(endpoint, string.Empty, (code, response) =>
            {
                if (code >= 200 && code < 300)
                {
                    string imageUrl = ExtractRustMapsImageUrlFromResponse(response);
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        winner.RustMapsImageUrl = imageUrl;
                        Puts($"RustMaps winner image resolved for {winner.Size}_{winner.Seed}: {imageUrl}");
                    }
                    else
                    {
                        PrintWarning($"RustMaps winner lookup succeeded for {winner.Size}_{winner.Seed}, but no image URL was found in the response.");
                    }
                }
                else
                {
                    PrintWarning($"RustMaps winner image lookup failed for {winner.Size}_{winner.Seed}. HTTP {code}. Response: {response}");
                }

                callback();
            }, this, Oxide.Core.Libraries.RequestMethod.GET, headers);
        }

        private void SendMapVoteWinnerEmbed(MapVoteResult winner)
        {
            string winnerImage = !string.IsNullOrWhiteSpace(winner.RustMapsImageUrl)
                ? winner.RustMapsImageUrl
                : config.Branding.BannerImageUrl;

            var payload = new DiscordBotMessagePayload
            {
                content = "",
                embeds = new List<DiscordEmbed>
                {
                    new DiscordEmbed
                    {
                        title = "🏆 RustStorm Map Vote Winner",
                        description = $"**{winner.Name}** won the next map vote.\n\n🗺 **Map Size:** {winner.Size}\n🌱 **Map Seed:** {winner.Seed}\n✅ **Votes:** {winner.Votes}\n🔗 **RustMaps:** {winner.RustMapsUrl}",
                        color = config.Branding.EmbedColorDecimal,
                        fields = new List<DiscordField>(),
                        footer = new DiscordFooter { text = $"{config.Message.Footer} • Map Vote Result" },
                        timestamp = DateTime.UtcNow.ToString("o"),
                        image = string.IsNullOrWhiteSpace(winnerImage) ? null : new DiscordImage { url = winnerImage },
                        thumbnail = string.IsNullOrWhiteSpace(config.Branding.ThumbnailImageUrl) ? null : new DiscordThumbnail { url = config.Branding.ThumbnailImageUrl }
                    }
                }
            };

            string url = $"https://discord.com/api/v10/channels/{config.MapVote.DiscordChannelId}/messages";
            SendDiscordBotRequest(url, JsonConvert.SerializeObject(payload), Oxide.Core.Libraries.RequestMethod.POST, (code, response) =>
            {
                if (code < 200 || code >= 300)
                    PrintWarning($"Failed to announce map vote winner. HTTP {code}. Response: {response}");
                else
                    SendMapVoteWinnerLinkMessage(winner);
            });
        }

        private void SendMapVoteWinnerLinkMessage(MapVoteResult winner)
        {
            if (winner == null || string.IsNullOrWhiteSpace(winner.RustMapsUrl))
                return;

            var payload = new DiscordBotMessagePayload
            {
                content = $"🗺 **Winning map:** {winner.RustMapsUrl}",
                embeds = null
            };

            string url = $"https://discord.com/api/v10/channels/{config.MapVote.DiscordChannelId}/messages";
            SendDiscordBotRequest(url, JsonConvert.SerializeObject(payload), Oxide.Core.Libraries.RequestMethod.POST, (code, response) =>
            {
                if (code < 200 || code >= 300)
                    PrintWarning($"Failed to post winning map link. HTTP {code}. Response: {response}");
            });
        }

        private void SendDiscordBotRequest(string url, string json, Oxide.Core.Libraries.RequestMethod method, Action<int, string> callback)
        {
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bot {config.MapVote.DiscordBotToken}",
                ["User-Agent"] = "RustStormWipeWebhook/1.6.2"
            };

            if (method != Oxide.Core.Libraries.RequestMethod.GET)
                headers["Content-Type"] = "application/json";

            webrequest.Enqueue(url, json ?? string.Empty, callback, this, method, headers);
        }

        private string ExtractJsonString(string json, string property)
        {
            try
            {
                JObject root = JObject.Parse(json);
                return root[property]?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string BuildRustMapsUrl(string size, string seed)
        {
            if (string.IsNullOrWhiteSpace(size) || string.IsNullOrWhiteSpace(seed))
                return string.Empty;

            string baseUrl = (config.MapInfo.RustMapsBaseUrl ?? "https://rustmaps.com/map").Trim().TrimEnd('/');
            return $"{baseUrl}/{size}_{seed}";
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

        private class DiscordBotMessagePayload
        {
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
