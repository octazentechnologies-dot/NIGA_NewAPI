using Niga_Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.Business.Interface
{
    public interface ILabTestMasterServices
    {
        /// <summary>
        /// Get All lab test 
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<LabTestMasterModel> GetLabTests(ref ErrorResponseModel errorResponseModel);
    }
}

