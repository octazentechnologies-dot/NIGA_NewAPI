using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interface
{
    public interface IRepertorizationPageService
    {
        List<MateriaMedicaHeadModel> GetMateriaMedicaHeadingbyAuthorId(int authorId);
        List<DifferentialMateriaMedicaListModel> GetDifferentialMateriaMedica(DifferentialMateriaMedica differentialMateriaMedica);
    }
}
