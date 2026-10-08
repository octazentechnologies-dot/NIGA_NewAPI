using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace API.Mapper
{
    /// <summary>
    /// DTO-to-entity copies. CopyTo overwrites every listed destination property, nulls included.
    /// </summary>
    public static class EntityMappings
    {
        public static SubSectionMaster ToSubSectionMaster(this AddSubSectionModel src)
        {
            var dest = new SubSectionMaster();
            src.CopyTo(dest);
            return dest;
        }

        public static void CopyTo(this AddSubSectionModel src, SubSectionMaster dest)
        {
            dest.SubSectionId = src.SubSectionId;
            dest.SectionId = src.SectionId;
            dest.SubSectionName = src.SubSectionName;
            dest.SubSectionNameAlias = src.SubSectionNameAlias;
            dest.Description = src.Description;
            dest.DeleteStatus = src.DeleteStatus;
            dest.ParentSubSectionId = src.ParentSubSectionId;
            dest.SubSectionLanguageDetails.Clear();
            if (src.SubLanguageDetail != null)
            {
                foreach (var language in src.SubLanguageDetail)
                {
                    dest.SubSectionLanguageDetails.Add(language.ToSubSectionLanguageDetail());
                }
            }
        }

        public static SubSectionLanguageDetail ToSubSectionLanguageDetail(this AddSubSectionLanguage src)
        {
            var dest = new SubSectionLanguageDetail();
            src.CopyTo(dest);
            return dest;
        }

        public static void CopyTo(this AddSubSectionLanguage src, SubSectionLanguageDetail dest)
        {
            dest.SubSectionLanguageId = src.SubSectionLanguageId;
            dest.SubSectionId = src.SubSectionId;
            dest.LanguageId = src.LanguageId;
            dest.SubSectionDetails = src.SubSectionDetails;
            dest.DeleteStatus = src.DeleteStatus;
        }

        public static SectionMaster ToSectionMaster(this SectionMasterDto src)
        {
            var dest = new SectionMaster();
            src.CopyTo(dest);
            return dest;
        }

        public static void CopyTo(this SectionMasterDto src, SectionMaster dest)
        {
            dest.SectionId = checked((int)src.SectionId);
            dest.BodyPartSectionId = src.BodyPartSectionId;
            dest.SectionName = src.SectionName;
            dest.SectionAlias = src.SectionAlias;
            dest.Description = src.Description;
            dest.DeleteStatus = src.DeleteStatus;
        }

        public static QuestionSubgroup ToQuestionSubgroup(this QuestionSubGroupModel src)
        {
            var dest = new QuestionSubgroup();
            src.CopyTo(dest);
            return dest;
        }

        public static void CopyTo(this QuestionSubGroupModel src, QuestionSubgroup dest)
        {
            dest.QuestionSubgroupId = src.QuestionSubgroupId;
            dest.Description = src.Description;
            dest.QuestionGroupId = src.QuestionGroupId;
            dest.DeleteStatus = src.DeleteStatus;
        }

        public static QuestionSectionMaster ToQuestionSectionMaster(this QuestionSectionModel src)
        {
            var dest = new QuestionSectionMaster();
            src.CopyTo(dest);
            return dest;
        }

        public static void CopyTo(this QuestionSectionModel src, QuestionSectionMaster dest)
        {
            dest.QuestionSectionId = src.QuestionSectionId;
            dest.QuestionSectionName = src.QuestionSectionName;
            dest.EnteredBy = src.EnteredBy;
            dest.EnteredDate = src.EnteredDate;
            dest.ChangedBy = src.ChangedBy;
            dest.ChangedDate = src.ChangedDate;
            dest.DeleteStatus = src.DeleteStatus;
        }

        public static QualificationMaster ToQualificationMaster(this QualificationModel src)
        {
            return new QualificationMaster
            {
                QualificationId = src.QualificationId,
                QualificationName = src.QualificationName,
                QualificationAlias = src.QualificationAlias,
                Description = src.Description,
                DegreeLevel = src.DegreeLevel,
                EnteredBy = src.EnteredBy,
                EnteredDate = src.EnteredDate,
                ChangedBy = src.ChangedBy,
                ChangedDate = src.ChangedDate,
                DeleteStatus = src.DeleteStatus,
            };
        }

        public static BlogDetail ToBlogDetail(this BlogDetailModel1 src)
        {
            var dest = new BlogDetail();
            src.CopyTo(dest);
            return dest;
        }

        public static void CopyTo(this BlogDetailModel1 src, BlogDetail dest)
        {
            dest.BlogId = src.BlogId;
            dest.BlogHead = src.BlogHead;
            dest.BlogSubHead = src.BlogSubHead;
            dest.BlogDate = src.BlogDate;
            dest.BlogImage1 = src.BlogImage1;
            dest.BlogImage2 = src.BlogImage2;
            dest.IsActive = src.IsActive;
            dest.EnteredBy = src.EnteredBy?.ToString();
            dest.EnteredDate = src.EnteredDate;
            dest.ChangedBy = src.ChangedBy?.ToString();
            dest.ChangedDate = src.ChangedDate;
        }

        // No mapping has ever existed for these pairs, so the legacy actions that call them always fail.
        // The UI uses POST/PUT api/questiongroup and PUT api/subsection instead.
        public static QuestionGroupMaster ToQuestionGroupMaster(this QuestionGroupModel src) =>
            throw new NotSupportedException("QuestionGroupModel cannot be mapped to QuestionGroupMaster.");

        public static void CopyTo(this QuestionGroupModel src, QuestionGroupMaster dest) =>
            throw new NotSupportedException("QuestionGroupModel cannot be mapped to QuestionGroupMaster.");

        public static void CopyTo(this AddReferenceRubricDetails src, ReferenceRubricDetail dest) =>
            throw new NotSupportedException("AddReferenceRubricDetails cannot be mapped to ReferenceRubricDetail.");

        public static DoctorReceptionStaff ToDoctorReceptionStaff(this AddReceptionStaffRequest src)
        {
            return new DoctorReceptionStaff
            {
                UserId = src.UserID,
                Password = src.Password,
                FullName = src.FullName,
                Address = src.Address,
                ContactNumber = src.ContactNumber,
                EmailId = src.EmailId,
                Country = src.Country,
                State = src.State,
                City = src.City,
                EnteredBy = src.EnteredBy,
            };
        }
    }
}
