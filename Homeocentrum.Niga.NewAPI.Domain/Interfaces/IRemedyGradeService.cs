#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
{
    public interface IRemedyGradeService
    {
        /// <summary>
        /// Interface is used to save Remedy Grade
        /// </summary>
        /// <param name="remedyGradeModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveRemedyGrade(RemedyGradeModel remedyGradeModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate Remedy Grade.
        /// </summary>
        /// <param name="remedyGradeModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteRemedyGrade(RemedyGradeModel remedyGradeModel, ref ErrorResponseModel errorResponseModel);
    }
}
