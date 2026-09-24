using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class AuthorMasterModel
    {
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string Description { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? IsForRepertory { get; set; }
        public string AuthorAlias { get; set; }

        public string AuthorNameAliasDescription { get; set;}
    }
}
