using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Domain.Interface
{
    public interface IRepertorizationPageService
    {
        List<MateriaMedicaHeadModel> GetMateriaMedicaHeadingbyAuthorId(int authorId);
        List<DifferentialMateriaMedicaListModel> GetDifferentialMateriaMedica(DifferentialMateriaMedica differentialMateriaMedica);
    }
}
