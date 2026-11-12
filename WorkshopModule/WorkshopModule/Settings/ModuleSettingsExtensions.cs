using System;
using Intent.Configuration;
using Intent.Engine;
using Intent.Modules.Common.Templates;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Settings.ModuleSettingsExtensions", Version = "1.0")]

namespace WorkshopModule.Settings
{
    public static class ModuleSettingsExtensions
    {
        public static TracingSettings GetTracingSettings(this IApplicationSettingsProvider settings)
        {
            return new TracingSettings(settings.GetGroup("e3fe5690-540e-4486-93b2-cd6e9b0857d6"));
        }
    }

    public class TracingSettings : IGroupSettings
    {
        private readonly IGroupSettings _groupSettings;

        public TracingSettings(IGroupSettings groupSettings)
        {
            _groupSettings = groupSettings;
        }

        public string Id => _groupSettings.Id;

        public string Title
        {
            get => _groupSettings.Title;
            set => _groupSettings.Title = value;
        }

        public ISetting GetSetting(string settingId)
        {
            return _groupSettings.GetSetting(settingId);
        }

        public string CorrelationHeaderName() => _groupSettings.GetSetting("72a59033-749b-40cf-8ddf-6d628d65f44a")?.Value;

        public bool EchoToResponse() => bool.TryParse(_groupSettings.GetSetting("0165ecd1-e2f2-4a21-9aa7-1377a9fea5a7")?.Value.ToPascalCase(), out var result) && result;
    }
}