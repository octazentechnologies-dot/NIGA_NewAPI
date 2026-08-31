using API.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for Qualification entity 
    /// </summary>
    [Route("api/qualification")]
    [ApiController]
    //[Authorize]
    public class QualificationController : ControllerBase
    {
        private readonly IQualificationService _qualificationService;
        private readonly IMapper _mapper;

        public QualificationController(IQualificationService qualificationService, IMapper mapper)
        {
            _qualificationService = qualificationService;
            _mapper = mapper;
        }

        /// <summary>
        /// To get qualification by Qualification ID 
        /// </summary>
        /// <param name="qualificationId"></param>
        /// <returns></returns>
        [HttpGet("GetQualificationDetailsById/{qualificationId}")]
        public async Task<object> GetQualificationById(long qualificationId)
        {
            try
            {
                var qualificationModel = await _qualificationService.GetQualificationDetailsById(qualificationId);

                if (qualificationModel != null)
                {
                    return new
                    {
                        Status = 200,
                        Data = qualificationModel
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Data = "No Data Found"
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    Status = 500,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// To get all Qualifications
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetQualificationList")]
        public async Task<List<QualificationModel>> ShowQualificationList([FromQuery] ParameterParams parameterParams)
        {
            var qualificationList = await _qualificationService.GetAllQualifications(parameterParams);
            Response.AddPaginationHeader(qualificationList.CurrentPage, qualificationList.PageSize,
                    qualificationList.TotalCount, qualificationList.TotalPages);
            return qualificationList;
        }

        [HttpPost("AddQualification")]
        public async Task<object> AddNewQualification(QualificationModel qualificationMasterDto)
        {
            try
            {
                if (qualificationMasterDto == null || string.IsNullOrWhiteSpace(qualificationMasterDto.QualificationName))
                {
                    return new
                    {
                        Status = 400,
                        Message = "Qualification Name is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(qualificationMasterDto.QualificationAlias))
                {
                    qualificationMasterDto.QualificationAlias = qualificationMasterDto.QualificationName;
                }

                var qualification = _mapper.Map<QualificationMaster>(qualificationMasterDto);
                qualification.QualificationId = 0;
                qualification.EnteredDate = DateTime.Now;
                qualification.DeleteStatus = false;
                _qualificationService.SaveQualification(qualification);
                if (await _qualificationService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Message = "Data Added Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Message = "Failed To Add Data"
                    };
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    Status = 500,
                    Message = ex.Message
                };
            }
        }

        [HttpPost("UpdateQualificationDetails")]
        public async Task<object> UpdateQualificationDetails(QualificationModel updateQualificationDto)
        {
            try
            {
                var data = await _qualificationService.GetQualificationById(updateQualificationDto.QualificationId);
                if (data == null)
                {
                    return new
                    {
                        Status = 404,
                        Message = "Qualification not found"
                    };
                }

                if (string.IsNullOrWhiteSpace(updateQualificationDto.QualificationAlias))
                {
                    updateQualificationDto.QualificationAlias = updateQualificationDto.QualificationName;
                }

                data.QualificationName = updateQualificationDto.QualificationName;
                data.QualificationAlias = updateQualificationDto.QualificationAlias;
                data.Description = updateQualificationDto.Description;
                data.DegreeLevel = updateQualificationDto.DegreeLevel;
                data.ChangedBy = updateQualificationDto.ChangedBy;
                data.ChangedDate = DateTime.Now;
                _qualificationService.UpdateQualification(data);
                if (await _qualificationService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Message = "Data Updated Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Message = "Failed To Update Data"
                    };
                }
            }
            catch (Exception ex)
            {
                var result = new
                {
                    Status = 500,
                    Message = ex.Message
                };
                return Ok(result);
            }
        }

        [HttpPost("DeleteQualificationDetails/{Id}")]
        public async Task<object> DeleteQualificationDetails(int Id)
        {
            var data = await _qualificationService.GetQualificationById(Id);
            try
            {
                _qualificationService.DeleteQualification(data);
                if (await _qualificationService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Message = "Data Deleted Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Message = "Failed To Delete Data"
                    };
                }
            }
            catch (Exception ex)
            {
                var result = new
                {
                    Status = 500,
                    Message = ex.Message
                };
                return BadRequest(result);
            }
        }

        [HttpGet]
        [Route("GetAllQualificationsByFilter")]
        [ProducesResponseType(typeof(QualificationModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAllQualificationsByFilter(string search)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var qualificationModel = _qualificationService.GetAllQualificationsByFilter(search, ref errorResponseModel);

                if (qualificationModel != null)
                {
                    return Ok(qualificationModel);
                }
                return BadRequest(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

       
    }
}