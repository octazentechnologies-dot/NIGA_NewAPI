using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Niga_Domain.Interface;
using Niga_Domain.Data;
using Niga_Domain.DTOs;

namespace Niga_Domain.Repositories
{
    public class RepertorizationPageService: IRepertorizationPageService
    {
        NIGACentrumContext context;
        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public RepertorizationPageService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        //Get MateriaMedica head by author Id
        public List<MateriaMedicaHeadModel> GetMateriaMedicaHeadingbyAuthorId(int authorId)
        {
            List<MateriaMedicaHeadModel> materiaMedicaHeadList = new List<MateriaMedicaHeadModel>();

            materiaMedicaHeadList = (from materiaMedicaHead in context.MateriaMedicaHeadMasters
                                     where materiaMedicaHead.AuthorId == authorId && materiaMedicaHead.IsDeleted == false
                                     select new MateriaMedicaHeadModel
                                     {
                                         MateriaMedicaHeadId = materiaMedicaHead.MateriaMedicaHeadId,
                                         MateriaMedicaHeadName = materiaMedicaHead.MateriaMedicaHeadName,
                                         DifferentialMM = materiaMedicaHead.DifferentialMm,
                                     }
                            ).ToList();

            return materiaMedicaHeadList;
        }


        public List<DifferentialMateriaMedicaListModel> GetDifferentialMateriaMedica(DifferentialMateriaMedica differentialMateriaMedica)
        { 
            var remedyIds=differentialMateriaMedica.RemedyIndexModelList.Select(x=>x.remedyId);


            List<DifferentialMateriaMedicaListModel> differentialMateriaMedicaList = new List<DifferentialMateriaMedicaListModel>();

            differentialMateriaMedicaList = (from materiaMedicaMaster in context.MateriaMedicaMasters
                                             join materiaMedicaHead in context.MateriaMedicaHeadMasters on materiaMedicaMaster.MateriaMedicaHeadId equals materiaMedicaHead.MateriaMedicaHeadId
                                             join materiaMedicaDetail in context.MateriaMedicaDetails on materiaMedicaMaster.MateriaMedicaId equals materiaMedicaDetail.MateriaMedicaId
                                             join remedyMaster in context.RemedyMasters on materiaMedicaMaster.RemedyId equals remedyMaster.RemedyId
                                             where materiaMedicaMaster.AuthorId == differentialMateriaMedica.authorId &&
                                                   differentialMateriaMedica.MateriaMedicaHeadIds.Contains(materiaMedicaMaster.MateriaMedicaHeadId) &&
                                                    remedyIds.Contains(remedyMaster.RemedyId) 
                                             select new DifferentialMateriaMedicaListModel
                                             {
                                                 RemedyId = materiaMedicaMaster.RemedyId,
                                                 MateriaMedicaHeadName = materiaMedicaHead.MateriaMedicaHeadName,
                                                 RemedyName = remedyMaster.RemedyName,
                                                 MateriaMedica=materiaMedicaDetail.MateriaMedicaDetail1
                                             }).ToList();

            differentialMateriaMedicaList = (from materiaMedicaList in differentialMateriaMedicaList 
                                             join remedyMaster in differentialMateriaMedica.RemedyIndexModelList on materiaMedicaList.RemedyId equals remedyMaster.remedyId
                                             orderby remedyMaster.index 
                                             select new DifferentialMateriaMedicaListModel
                                             {
                                                 RemedyId = materiaMedicaList.RemedyId,
                                                 MateriaMedicaHeadName = materiaMedicaList.MateriaMedicaHeadName,
                                                 RemedyName = materiaMedicaList.RemedyName,
                                                 MateriaMedica = materiaMedicaList.MateriaMedica,
                                                 score=differentialMateriaMedica.RemedyIndexModelList.Where(x=>x.remedyId== materiaMedicaList.RemedyId).Select(x=>x.score).FirstOrDefault(),
                                             }).ToList();
          
            return differentialMateriaMedicaList;
        }

    }
}
