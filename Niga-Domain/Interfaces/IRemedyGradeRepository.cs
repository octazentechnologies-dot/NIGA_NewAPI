using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.Helpers;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Master;

namespace Niga_Domain.Interfaces
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