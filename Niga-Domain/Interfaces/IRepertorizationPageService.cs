using System;
using System.Collections.Generic;
using System.Text;
using Niga_Domain.DTOs;

namespace Niga_Domain.Interface
{
    public interface IRepertorizationPageService
    {
        List<MateriaMedicaHeadModel> GetMateriaMedicaHeadingbyAuthorId(int authorId);
        List<DifferentialMateriaMedicaListModel> GetDifferentialMateriaMedica(DifferentialMateriaMedica differentialMateriaMedica);
    }
}
