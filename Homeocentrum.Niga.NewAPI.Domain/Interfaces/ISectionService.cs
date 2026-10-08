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
    public interface ISectionService
    {
        /// <summary>
        /// Interface is used to save Section
        /// </summary>
        /// <param name="sectionModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveSection(SectionModel sectionModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate Section.
        /// </summary>
        /// <param name="sectionModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteSection(SectionModel sectionModel, ref ErrorResponseModel errorResponseModel);
    }
}
