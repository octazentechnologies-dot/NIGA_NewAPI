using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Helpers;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;

namespace Homeocentrum.Niga.API.Domain.Interfaces
{
    public interface IRemedyGradeRepository
    {
        Task<RemedyGradeMaster> GetRemedyGradeById(long remedyGradeId);
        Task<PagedList<RemedyGradeModel>> GetAllRemedyGrades(ParameterParams parameterParams);
        void SaveRemedyGrade(RemedyGradeMaster remedyGrade);
        void UpdateRemedyGrade(RemedyGradeMaster remedyGrade);
        void DeleteRemedyGrade(RemedyGradeMaster remedyGrade);
        Task<RemedyGradeModel> GetRemedyGradeDetailsById(long remedyGradeId);
        Task<bool> SaveAllAsync();
    }
} 