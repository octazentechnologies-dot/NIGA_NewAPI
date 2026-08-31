using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Niga_Domain.DTOs
{
    public class ThreeDBodyPartMeshKeyMasterModel
    {
        [JsonPropertyName("ThreeD_BodyPart_MeshKeyID")]
        public int ThreeDBodyPartMeshKeyId { get; set; }

        [JsonPropertyName("ThreeD_BodyPart_MeshKey_Name")]
        public string ThreeDBodyPartMeshKeyName { get; set; } = null!;

        public int? EnteredBy { get; set; }

        public DateTime? EnteredDate { get; set; }

        public int? ChangedBy { get; set; }

        public DateTime? ChangedDate { get; set; }

        public bool DeleteStatus { get; set; }
    }

    public class AddThreeDBodyPartMeshKeyMasterRequest
    {
        [Required(ErrorMessage = "ThreeD_BodyPart_MeshKey_Name is required")]
        [MaxLength(200, ErrorMessage = "ThreeD_BodyPart_MeshKey_Name cannot exceed 200 characters")]
        [JsonPropertyName("ThreeD_BodyPart_MeshKey_Name")]
        public string ThreeDBodyPartMeshKeyName { get; set; } = null!;

        [Required(ErrorMessage = "EnteredBy is required")]
        [Range(1, int.MaxValue, ErrorMessage = "EnteredBy is required")]
        public int EnteredBy { get; set; }
    }

    public class UpdateThreeDBodyPartMeshKeyMasterRequest
    {
        [Required(ErrorMessage = "ThreeD_BodyPart_MeshKeyID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ThreeD_BodyPart_MeshKeyID is required")]
        [JsonPropertyName("ThreeD_BodyPart_MeshKeyID")]
        public int ThreeDBodyPartMeshKeyId { get; set; }

        [Required(ErrorMessage = "ThreeD_BodyPart_MeshKey_Name is required")]
        [MaxLength(200, ErrorMessage = "ThreeD_BodyPart_MeshKey_Name cannot exceed 200 characters")]
        [JsonPropertyName("ThreeD_BodyPart_MeshKey_Name")]
        public string ThreeDBodyPartMeshKeyName { get; set; } = null!;

        [Required(ErrorMessage = "ChangedBy is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ChangedBy is required")]
        public int ChangedBy { get; set; }
    }

    public class DeleteThreeDBodyPartMeshKeyMasterRequest
    {
        [Required(ErrorMessage = "ThreeD_BodyPart_MeshKeyID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ThreeD_BodyPart_MeshKeyID is required")]
        [JsonPropertyName("ThreeD_BodyPart_MeshKeyID")]
        public int ThreeDBodyPartMeshKeyId { get; set; }

        [Required(ErrorMessage = "ChangedBy is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ChangedBy is required")]
        public int ChangedBy { get; set; }
    }

    public class GetThreeDBodyPartMeshKeyMasterByIdRequest
    {
        [Required(ErrorMessage = "ThreeD_BodyPart_MeshKeyID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ThreeD_BodyPart_MeshKeyID is required")]
        [JsonPropertyName("ThreeD_BodyPart_MeshKeyID")]
        public int ThreeDBodyPartMeshKeyId { get; set; }
    }
}
