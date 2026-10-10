using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Homeocentrum.Niga.API.Domain.DTOs
{
    public class AuthorMasterModel
    {
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string Description { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? IsForRepertory { get; set; }
        public string AuthorAlias { get; set; }

        private string? _authorNameAliasDescription;

        public string? AuthorNameAliasDescription
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_authorNameAliasDescription))
                    return _authorNameAliasDescription;
                var text = string.Join(" ", new[] { AuthorAlias, AuthorName }
                    .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
                if (!string.IsNullOrWhiteSpace(Description))
                    text = $"{text} [{Description.Trim()}]".Trim();
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            set => _authorNameAliasDescription = value;
        }
    }
}
