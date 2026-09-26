using System.Text.Json;
using System.Text.Json.Nodes;

namespace Homeocentrum.Niga.NewAPI.Domain.Helpers
{
    /// <summary>
    /// CLN-07.02 — keep clipboard intensity on PatientBoardBackup payload
    /// (client-only intensity must survive restore).
    /// </summary>
    public static class BoardBackupIntensityMerger
    {
        public static string EnsureIntensityPersisted(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return payload;

            JsonNode? node;
            try
            {
                node = JsonNode.Parse(payload);
            }
            catch (JsonException)
            {
                return payload;
            }

            if (node == null)
                return payload;

            Walk(node);
            return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }

        private static void Walk(JsonNode? node)
        {
            if (node is JsonObject obj)
            {
                var hasSubSection = obj.ContainsKey("subSectionId")
                    || obj.ContainsKey("SubSectionId")
                    || obj.ContainsKey("subsectionId")
                    || obj.ContainsKey("rubricId");
                var hasIntensity = obj.ContainsKey("intensity")
                    || obj.ContainsKey("Intensity")
                    || obj.ContainsKey("intensityNo")
                    || obj.ContainsKey("IntensityNo");
                if (hasSubSection && !hasIntensity)
                    obj["intensityNo"] = 2;

                foreach (var kv in obj.ToList())
                    Walk(kv.Value);
            }
            else if (node is JsonArray arr)
            {
                foreach (var child in arr)
                    Walk(child);
            }
        }
    }
}
