using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
public class GradeRemediesModel
{
    public int GradeId { get; set; }
    public int GradeNo { get; set; }
    public int subSectionId { get; set; }
    public string FontName { get; set; }
    public string FontStyle { get; set; }
    public string FontColor { get; set; }
    public string Description { get; set; }
    public List<int> AuthorId { get; set; }


    public List<RemediesModel> remediesModels { get; set; }
}


public class RemediesModel
{
    public int RemedyId { get; set; }
    public string RemedyName { get; set; }
    public string RemedyAlias { get; set; }
    public int AuthorId { get; set; }
    public string AuthorAlias { get; set; }
    public int? ThermalId { get; set; }
    public bool? CommonOrUncommon { get; set; }

    public string FontName { get; set; }
   public string FontStyle { get; set; }
    public string FontColor { get; set; }
     public int GradeNo { get; set; }
    public string ThemesORCharacteristics { get; set; }
    public string Generals { get; set; }
    public string Modalities { get; set; }
    public string Particulars { get; set; }


}
