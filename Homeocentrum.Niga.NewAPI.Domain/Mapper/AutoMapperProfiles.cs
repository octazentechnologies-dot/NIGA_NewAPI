using API.Entities;
using AutoMapper;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace API.Mapper
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            // CreateMap<LocationMasterDto, LocationMaster>();
            // CreateMap<LocationMasterDto, LocationMasterDto>();
            // CreateMap<LocationMaster, LocationMasterDto>();

            CreateMap<AddSubSectionModel, SubSectionMaster>();
            CreateMap<SubSectionMaster, AddSubSectionModel>();

            CreateMap<SectionMaster, SectionMasterDto>();
            CreateMap<SectionMasterDto, SectionMaster>();
            CreateMap<SectionMaster, SectionModel>();
            CreateMap<SubSectionList, SubSectionList>();

            CreateMap<AddSubSectionModel, SubSectionMaster>()
                .ForMember(
                    dest => dest.SubSectionLanguageDetails,
                    opt => opt.MapFrom(src => src.SubLanguageDetail)
                );

            // Mapping for SubSectionLanguageDetails (if it's a complex type)
            CreateMap<AddSubSectionLanguage, SubSectionLanguageDetail>().ReverseMap();

            CreateMap<RemedyGradeModel, RemedyGradeMaster>();
            CreateMap<RemedyGradeModel, RemedyGradeModel>();
            CreateMap<RemedyGradeMaster, RemedyGradeModel>();

            CreateMap<QualificationModel, QualificationMaster>();
            CreateMap<QualificationModel, QualificationModel>();
            CreateMap<QualificationMaster, QualificationModel>();

            CreateMap<QuestionSectionModel, QuestionSectionMaster>();
            CreateMap<QuestionSectionModel, QuestionSectionModel>();
            CreateMap<QuestionSectionMaster, QuestionSectionModel>();

            CreateMap<QuestionSubGroupModel, QuestionSubgroup>();
            CreateMap<QuestionSubGroupModel, QuestionSubGroupModel>();
            CreateMap<QuestionSubgroup, QuestionSubGroupModel>();

            CreateMap<BlogDetailModel1, BlogDetail>();
            CreateMap<BlogDetailModel1, BlogDetailModel1>();
            CreateMap<BlogDetail, BlogDetailModel1>();

            CreateMap<ThreeDBodyPartSectionHotspot, ThreeDBodyPartSectionHotspotModel>();
            CreateMap<ThreeDBodyPartSectionHotspotModel, ThreeDBodyPartSectionHotspot>();

            CreateMap<ThreeDBodyPartMeshKeyMaster, ThreeDBodyPartMeshKeyMasterModel>();
            CreateMap<ThreeDBodyPartMeshKeyMasterModel, ThreeDBodyPartMeshKeyMaster>();

            CreateMap<ThreeDBodyPartSectionMaster, ThreeDBodyPartSectionMasterModel>();
            CreateMap<ThreeDBodyPartSectionMasterModel, ThreeDBodyPartSectionMaster>();

            CreateMap<AddReceptionStaffRequest, DoctorReceptionStaff>();
            CreateMap<UpdateReceptionStaffRequest, DoctorReceptionStaff>();
            CreateMap<DoctorReceptionStaff, ReceptionStaffResponseModel>()
                .ForMember(dest => dest.ReceptionStaffID, opt => opt.MapFrom(src => src.ReceptionStaffId))
                .ForMember(dest => dest.DoctorID, opt => opt.MapFrom(src => src.DoctorId))
                .ForMember(dest => dest.UserID, opt => opt.MapFrom(src => src.UserId));
        }
    }
}
