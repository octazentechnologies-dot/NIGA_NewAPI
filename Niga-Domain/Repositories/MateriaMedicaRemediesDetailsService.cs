using Niga_Domain.Business.Interface;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Niga_Domain.Business.Implementation
{
    public class MateriaMedicaRemediesDetailsService : IMateriaMedicaRemediesDetails
    {
        NIGACentrumContext context;
        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public MateriaMedicaRemediesDetailsService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }


        public MateriaMedicaRemediesDetailsModel GetMateriaMedicaRemediesDetails(long remedyId, long authorId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var MateriaMedicaList = new List<MateriaMedicaRemediesDetailsModel>();
            var M1 = context.MateriaMedicaMasters.Where(x => x.AuthorId == authorId && x.RemedyId == remedyId).FirstOrDefault();
            var materiamedicaremediesEntity = (from remedydetail in context.MateriaMedicaDetails
                                               join materiaMedica in context.MateriaMedicaMasters
                                               on remedydetail.MateriaMedicaId equals materiaMedica.MateriaMedicaId
                                               join head in context.MateriaMedicaHeadMasters
                                               on materiaMedica.MateriaMedicaHeadId equals head.MateriaMedicaHeadId
                                               where materiaMedica.RemedyId==remedyId && materiaMedica.AuthorId==authorId
                                               && materiaMedica.IsDeleted == false
                                               select new
                                               {
                                                   head.MateriaMedicaHeadName,
                                                   head.MateriaMedicaHeadId,
                                                   remedydetail.MateriaMedicaDetail1,

                                               }).Distinct().ToList();

            if (materiamedicaremediesEntity.Count == 0)
            {
                errorResponseModel.StatusCode = System.Net.HttpStatusCode.NotFound;
                errorResponseModel.Message = "MateriaMedica Not Found";
            }
            
           List<MateriaMedicaRemediesDetailsModel1> lstMatMedicaDetails = new List<MateriaMedicaRemediesDetailsModel1>();

            materiamedicaremediesEntity.ForEach(item =>
            {
                MateriaMedicaRemediesDetailsModel1 modelValues = new MateriaMedicaRemediesDetailsModel1();
                
                modelValues.MateriaMedicaHeadId = item.MateriaMedicaHeadId;
                modelValues.MateriaMedicaHeadName = item.MateriaMedicaHeadName;
                modelValues.MateriaMedicaDetail1 = item.MateriaMedicaDetail1;
                lstMatMedicaDetails.Add(modelValues);
                               

               });
            
            MateriaMedicaRemediesDetailsModel modelInfo=new MateriaMedicaRemediesDetailsModel();
            modelInfo.RemedyId = Convert.ToInt32(remedyId);
            modelInfo.AuthorId = Convert.ToInt32(authorId);
            modelInfo.lstRemedy = lstMatMedicaDetails;

            return modelInfo;


        }
    }
}
