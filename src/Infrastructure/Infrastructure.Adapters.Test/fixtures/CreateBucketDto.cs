using System.Text.Json.Serialization;

namespace Arbeidstilsynet.MeldingerReceiver.Infrastructure.Adapters.Test.fixtures
{
    public class IamConfiguration
    {
        [JsonPropertyName("uniformBucketLevelAccess")]
        public required UniformBucketLevelAccess UniformBucketLevelAccess { get; set; }
    }

    public class CreateBucketDto
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("location")]
        public required string Location { get; set; }

        [JsonPropertyName("storageClass")]
        public required string StorageClass { get; set; }

        [JsonPropertyName("iamConfiguration")]
        public required IamConfiguration IamConfiguration { get; set; }
    }

    public class UniformBucketLevelAccess
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
    }
}
