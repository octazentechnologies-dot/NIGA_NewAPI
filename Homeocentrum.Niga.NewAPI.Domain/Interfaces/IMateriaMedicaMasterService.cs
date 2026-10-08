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
    public interface IMateriaMedicaMasterService
    {
        /// <summary>
        /// Method is used for to get Materiamedica by materiamedicaId
        /// </summary>
        /// <param name="materiamedicaId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        MateriaMedicaMasterModel GetMateriaMedicaById(long materiamedicaId, ref ErrorResponseModel errorResponseModel);
        /// <summary>
            /// interface for getting all the MateriaMedica
            /// </summary>
            /// <param name="errorResponseModel"></param>
            /// <returns></returns>
        List<MateriaMedicaMasterModel1> GetMateriaMedica(NigaParameters nigaParameters,ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to save MateriaMedica
        /// </summary>
        /// <param name="materiamedicamodel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveMateriaMedica(MateriaMedicaMasterModel materiamedicamodel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate MateriaMedica.
        /// </summary>
        /// <param name="materiamedicamodel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        
       

        string DeleteMateriaMedica(MateriaMedicaMasterModel materiamedicamodel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// interface for getting all the MateriaMedica by Author
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<MateriaMedicaMasterModel2> GetMateriaMedicaHeadByAuthorId(long authorId, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// interface for getting all the Author
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<AuthorDDLModel> GetAuthorDDL(ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// interface for getting all the Remedy
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<RemedyDDLModel> GetRemedyDDL(ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// interface for getting all the MateriaMedica by author & Remedy
        /// </summary>
        /// <param name="authorId"></param>
        /// <param name="remedyId"></param>
        /// <param name="nigaParameters"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<MateriaMedicaModel> GetMateriaMedicaByAuthorRemedy(MateriaMedicaFilterModel materiaMedicaFilter, ref ErrorResponseModel errorResponseModel);
    }
}
