using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Niga_Domain.DTOs
{
    public class ThreeDBodyPartSectionMasterModel
    {
        [JsonPropertyName("ThreeDBodyPartSectionMasterID")]
        public int ThreeDBodyPartSectionMasterId { get; set; }

        [JsonPropertyName("ThreeD_BodyPart_MeshKeyID")]
        public int ThreeDBodyPartMeshKeyId { get; set; }

        [JsonPropertyName("ThreeD_BodyPart_MeshKey_Name")]
        public string? ThreeDBodyPartMeshKeyName { get; set; }

        [JsonPropertyName("ThreeDBodyPartSectionID")]
        public int ThreeDBodyPartSectionId { get; set; }

        [JsonPropertyName("SectionName")]
        public string? SectionName { get; set; }

        public int? EnteredBy { get; set; }

        public DateTime? EnteredDate { get; set; }

        public int? ChangedBy { get; set; }

        public DateTime? ChangedDate { get; set; }

        public bool DeleteStatus { get; set; }
    }

    public class ThreeDBodyPartSectionMasterListItem
    {
        [JsonPropertyName("ThreeDBodyPartSectionMasterID")]
        public int ThreeDBodyPartSectionMasterId { get; set; }

        [JsonPropertyName("ThreeD_BodyPart_MeshKeyID")]
        public int ThreeDBodyPartMeshKeyId { get; set; }

        [JsonPropertyName("ThreeD_BodyPart_MeshKey_Name")]
        public string? ThreeDBodyPartMeshKeyName { get; set; }

        [JsonPropertyName("ThreeDBodyPartSectionID")]
        public int ThreeDBodyPartSectionId { get; set; }

        [JsonPropertyName("SectionName")]
        public string? SectionName { get; set; }
    }

    public class ThreeDBodyPartMeshKeyDropdownModel
    {
        [JsonPropertyName("ThreeD_BodyPart_MeshKeyID")]
        public int ThreeDBodyPartMeshKeyId { get; set; }

        [JsonPropertyName("ThreeD_BodyPart_MeshKey_Name")]
        public string ThreeDBodyPartMeshKeyName { get; set; } = null!;
    }

    public class SectionMasterDropdownModel
    {
        [JsonPropertyName("SectionID")]
        public int SectionId { get; set; }

        [JsonPropertyName("SectionName")]
        public string SectionName { get; set; } = null!;
    }

    public class AddThreeDBodyPartSectionMasterRequest
    {
        [Required(ErrorMessage = "ThreeD_BodyPart_MeshKeyID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ThreeD_BodyPart_MeshKeyID is required")]
        [JsonPropertyName("ThreeD_BodyPart_MeshKeyID")]
        public int ThreeDBodyPartMeshKeyId { get; set; }

        [Required(ErrorMessage = "ThreeDBodyPartSectionID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ThreeDBodyPartSectionID is required")]
        [JsonPropertyName("ThreeDBodyPartSectionID")]
        public int ThreeDBodyPartSectionId { get; set; }

        [Required(ErrorMessage = "EnteredBy is required")]
        [Range(1, int.MaxValue, ErrorMessage = "EnteredBy is required")]
        public int EnteredBy { get; set; }
    }

    public class UpdateThreeDBodyPartSectionMasterRequest
    {
        [Required(ErrorMessage = "ThreeDBodyPartSectionMasterID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ThreeDBodyPartSectionMasterID is required")]
        [JsonPropertyName("ThreeDBodyPartSectionMasterID")]
        public int ThreeDBodyPartSectionMasterId { get; set; }

        [Required(ErrorMessage = "ThreeD_BodyPart_MeshKeyID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ThreeD_BodyPart_MeshKeyID is required")]
        [JsonPropertyName("ThreeD_BodyPart_MeshKeyID")]
        public int ThreeDBodyPartMeshKeyId { get; set; }

        [Required(ErrorMessage = "ThreeDBodyPartSectionID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ThreeDBodyPartSectionID is required")]
        [JsonPropertyName("ThreeDBodyPartSectionID")]
        public int ThreeDBodyPartSectionId { get; set; }

        [Required(ErrorMessage = "ChangedBy is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ChangedBy is required")]
        public int ChangedBy { get; set; }
    }

    public class DeleteThreeDBodyPartSectionMasterRequest
    {
        [Required(ErrorMessage = "ThreeDBodyPartSectionMasterID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ThreeDBodyPartSectionMasterID is required")]
        [JsonPropertyName("ThreeDBodyPartSectionMasterID")]
        public int ThreeDBodyPartSectionMasterId { get; set; }

        [Required(ErrorMessage = "ChangedBy is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ChangedBy is required")]
        public int ChangedBy { get; set; }
    }

    public class GetThreeDBodyPartSectionMasterByIdRequest
    {
        [Required(ErrorMessage = "ThreeDBodyPartSectionMasterID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ThreeDBodyPartSectionMasterID is required")]
        [JsonPropertyName("ThreeDBodyPartSectionMasterID")]
        public int ThreeDBodyPartSectionMasterId { get; set; }
    }

}
