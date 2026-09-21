using System;
using System.Collections.Generic;
using System.Text;
using Niga_Domain.DTOs;

namespace Niga_Domain.Business.Interface
{
    public interface IMateriaMedicaRemediesDetails
    {
        MateriaMedicaRemediesDetailsModel GetMateriaMedicaRemediesDetails(long remedyId, long authorId,  ref ErrorResponseModel errorResponseModel);
        List<MateriaMedicaRemediesDetailsModel> GetMateriaMedicaByRemedy(long remedyId, ref ErrorResponseModel errorResponseModel);

    }
}
